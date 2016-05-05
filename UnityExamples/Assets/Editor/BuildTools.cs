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
    /// Function for UMB.
    /// </summary>
    public static void BuildTangoDat()
    {
        // Write out a version file.
        string filePath = Application.dataPath + TangoSDKAbout.TANGO_VERSION_DATA_PATH;
        using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        {
            binaryWriter.Write(GitHelpers.GetTagInfo());
            binaryWriter.Write(GitHelpers.GetPrettyGitHash());
            binaryWriter.Write(GitHelpers.GetRemoteBranchName());
        }
    }
}
