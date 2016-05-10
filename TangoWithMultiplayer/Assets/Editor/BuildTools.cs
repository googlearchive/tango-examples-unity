// <copyright file="BuildTools.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
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
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Build scripts for this specific project.
/// </summary>
public class BuildTools
{
    /// <summary>
    /// Fill out App-specific IDs.
    /// </summary>
    public static void FillAppID()
    {
        string appId = File.ReadAllText(_FindKeystoreDirectory() + "/pun-app-id.txt");

        if (PhotonNetwork.PhotonServerSettings == null)
        {
            PhotonNetwork.CreateSettings();
        }

        if (PhotonNetwork.PhotonServerSettings == null)
        {
            return;
        }

        PhotonNetwork.PhotonServerSettings.AppID = appId;
        EditorUtility.SetDirty(PhotonNetwork.PhotonServerSettings);
    }

    /// <summary>
    /// Searches up the directory hierarchy for a Keystore directory.
    /// </summary>
    /// <returns>Absolute path to the keystore directory or null if none is found.</returns>
    private static string _FindKeystoreDirectory()
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
}
