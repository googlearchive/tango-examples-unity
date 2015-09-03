// <copyright file="BuildUtil.cs" company="Google">
//
// Copyright 2015 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Build utilities for building packages and apks.  Shareable across all projects.
/// </summary>
public class BuildUtil
{
    /// <summary>
    /// Defines all the APK-specific configs we have.
    /// </summary>
    public class APKSettings
    {
        public string ProjectName;
        public string Icon;
        public string[] Scenes;
        public string BundleIdentifier;
    }

    /// <summary>
    /// Defines all the UnityPackage-specific configs we have.
    /// </summary>
    public class PackageSettings
    {
        public string PackageName;
        public string[] Directories;
    }

    /// <summary>
    /// Where to put built APKs.
    /// </summary>
    public const string BUILD_APK_DIRECTORY_W_SLASH = "Build/Android/";
    
    /// <summary>
    /// Where to put built UnityPackages.
    /// </summary>
    public const string BUILD_PACKAGE_DIRECTORY_W_SLASH = "Build/";

    /// <summary>
    /// The assets folder.  All assets are inside of this folder anyway, so this avoids having to specify it.
    /// </summary>
    public const string ASSET_DIRECTORY_W_SLASH = "Assets/";

    /// <summary>
    /// Builds a single APK and places it in the build folder.
    /// </summary>
    /// <param name="settings">Description of the APK to build.</param>
    public static void BuildAPK(APKSettings settings)
    {
        // Don't make any permanent changes to the player settings!
        Texture2D[] oldIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
        string oldKeystoreName = PlayerSettings.Android.keystoreName;
        string oldkeyaliasName = PlayerSettings.Android.keyaliasName;
        string oldKeystorePass = PlayerSettings.Android.keystorePass;
        string oldKeyaliasPass = PlayerSettings.Android.keyaliasPass;
        string oldProductName = PlayerSettings.productName;
        string oldBundleIdentifier = PlayerSettings.bundleIdentifier;
        string oldAndroidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot");

        // make sure the product folder exists
        System.IO.Directory.CreateDirectory(BUILD_APK_DIRECTORY_W_SLASH);

        // set icons (Android currently supports 6 sizes)
        Texture2D icon = (Texture2D)AssetDatabase.LoadMainAssetAtPath(ASSET_DIRECTORY_W_SLASH + settings.Icon);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, 
                                              new Texture2D[] { icon, icon, icon, icon, icon, icon });

        string keystoreDir = FindKeystoreDirectory();
        if (keystoreDir != null)
        {
            string keystorePass = ReadKeystorePassword(keystoreDir + "/trailmix-release-key-password.txt");

            PlayerSettings.Android.keystoreName = keystoreDir + "/trailmix-release-key.keystore";
            PlayerSettings.Android.keyaliasName = "alias_name";
            PlayerSettings.Android.keystorePass = PlayerSettings.Android.keyaliasPass = keystorePass;
        }

        // set name and ids
        PlayerSettings.productName = settings.ProjectName;
        PlayerSettings.bundleIdentifier = settings.BundleIdentifier;

        // Unity won't remember where the Android SDK was when built on Jenkins
        string sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (sdkRoot != String.Empty)
        {
            EditorPrefs.SetString("AndroidSdkRoot", sdkRoot);
        }

        // and finally... build it!
        string[] scenesUnityPath = new string[settings.Scenes.Length];
        for (int it = 0; it != settings.Scenes.Length; ++it)
        {
            scenesUnityPath[it] = ASSET_DIRECTORY_W_SLASH + settings.Scenes[it];
        }
        BuildPipeline.BuildPlayer(scenesUnityPath, BUILD_APK_DIRECTORY_W_SLASH + settings.ProjectName + ".apk", 
                                  BuildTarget.Android, BuildOptions.None);

        // Restore player settings
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, oldIcons);
        PlayerSettings.Android.keystoreName = oldKeystoreName;
        PlayerSettings.Android.keyaliasName = oldkeyaliasName;
        PlayerSettings.Android.keystorePass = oldKeystorePass;
        PlayerSettings.Android.keyaliasPass = oldKeyaliasPass;
        PlayerSettings.productName = oldProductName;
        PlayerSettings.bundleIdentifier = oldBundleIdentifier;
        EditorPrefs.SetString("AndroidSdkRoot", oldAndroidSdkRoot);
    }

    /// <summary>
    /// Builds a Unity package from a list of directories.
    /// </summary>
    /// <param name="settings">Description of the UnityPackage to build.</param>
    public static void BuildPackage(PackageSettings settings)
    {
        // make sure the product folder exists
        System.IO.Directory.CreateDirectory(BUILD_PACKAGE_DIRECTORY_W_SLASH);

        // Ensure any recently changed / newly created assets are known by Unity.
        AssetDatabase.Refresh();

        // and finally... build it!
        string[] dirUnityPath = new string[settings.Directories.Length];
        for (int it = 0; it != settings.Directories.Length; ++it)
        {
            dirUnityPath[it] = ASSET_DIRECTORY_W_SLASH + settings.Directories[it];
        }
        AssetDatabase.ExportPackage(dirUnityPath, 
                                    BUILD_PACKAGE_DIRECTORY_W_SLASH + settings.PackageName + ".unitypackage", 
                                    ExportPackageOptions.Recurse);
    }

    /// <summary>
    /// Searches up the directory hierarchy for a Keystore directory.
    /// </summary>
    /// <returns>Absolute path to the keystore directory or null if none is found.</returns>
    private static string FindKeystoreDirectory()
    {
        DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string keystoreDirName = dir.FullName + "/Keystore";
            if (Directory.Exists(keystoreDirName))
            {
                return keystoreDirName;
            }

            dir = dir.Parent;
        }

        Debug.Log("Unable to find keystore");
        return null;
    }

    /// <summary>
    /// Read keystore password from file.
    /// </summary>
    /// <returns>The password, or null if it can not read the file.</returns>
    /// <param name="passwordFile">
    /// The file to read the password from.  The password should be on the first line.
    /// </param>
    private static string ReadKeystorePassword(string passwordFile)
    {
        try
        {
            System.IO.StreamReader file = new System.IO.StreamReader(passwordFile);
            string psw = file.ReadLine();
            file.Close();
            return psw;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return null;
        }
    }
}
