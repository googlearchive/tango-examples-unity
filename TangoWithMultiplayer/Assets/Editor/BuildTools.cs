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
/// Build scripts for this specific project.  Uses BuildUtil.cs, which should be sharable across
/// all projects.
/// 
/// To use this from the command line, run the following command:
/// [FULLPATH_UNITY.APP]/Contents/MacOS/Unity -batchmode -projectPath [FULLPATH] -executeMethod [METHOD_TO_RUN] -quit
/// 
/// For example:
/// /Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -projectPath ~/Unity/tango-examples-unity/UnityExamples/ -executeMethod BuildTools.BuildAll -quit
/// 
/// For more info, goto [http://docs.unity3d.com/Manual/CommandLineArguments.html].
/// </summary>
public class BuildTools
{
    private static BuildUtil.APKSettings multiplayerAPK = new BuildUtil.APKSettings
    {
        ProjectName = "Tango Multiplayer",
        Icon = "Texture/multiplayer_logo.png",
        Scenes = new string[]
        {
            "Scenes/AreaDescriptionPicker.unity",
            "Scenes/MultiplayerCubeStacker.unity"
        },
        BundleIdentifier = "com.projecttango.multiplayerdemo"
    };

    /// <summary>
    /// Builds all the appropriate APKs for this project.
    /// </summary>
    [MenuItem("Tango/Build/All", false, 1)]
    public static void BuildAll()
    {
        string appId = File.ReadAllText(BuildUtil.FindKeystoreDirectory() + "/pun-app-id.txt");

        if (PhotonNetwork.PhotonServerSettings == null)
        {
            PhotonNetwork.CreateSettings();
        }

        if (PhotonNetwork.PhotonServerSettings == null)
        {
            return;
        }

        PhotonNetwork.PhotonServerSettings.AppID = appId;
        BuildUtil.BuildAPK(multiplayerAPK);
        PhotonNetwork.PhotonServerSettings.AppID = "";
    }
}
