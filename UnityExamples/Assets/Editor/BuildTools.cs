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
    private static BuildUtil.APKSettings examplesAPK = new BuildUtil.APKSettings
    {
        ProjectName = "Unity Examples",
        Icon = "TangoSDK/Examples/Common/Textures/ProjectTango_Logo.png",
        Scenes = new string[]
        {
            "TangoSDK/Examples/Scenes/MotionTracking.unity",
            "TangoSDK/Examples/Scenes/PointCloud.unity",
            "TangoSDK/Examples/Scenes/AreaLearning.unity",
            "TangoSDK/Examples/Scenes/AreaDescriptionManagement.unity",
        },
        BundleIdentifier = "com.google.projecttango.examples"
    };

    private static BuildUtil.APKSettings augmentedRealityAPK = new BuildUtil.APKSettings 
    {
        ProjectName = "Unity Augmented Reality",
        Icon = "TangoSDK/Examples/AugmentedReality/Textures/ar_icon.png",
        Scenes = new string[] { "TangoSDK/Examples/Scenes/AugmentedReality.unity" },
        BundleIdentifier = "com.projecttango.experiments.augmentedreality"
    };

    private static BuildUtil.APKSettings meshBuilderAPK = new BuildUtil.APKSettings 
    {
        ProjectName = "Unity Mesh Builder",
        Icon = null,
        Scenes = new string[] { "TangoSDK/Examples/Scenes/ExperimentalMeshBuilder.unity" },
        BundleIdentifier = "com.google.projecttango.meshbuilder"
    };

    private static BuildUtil.APKSettings virtualRealityAPK = new BuildUtil.APKSettings
    {
        ProjectName = "Unity VirtualReality",
        Icon = "TangoSDK/Examples/ExperimentalVirtualReality/Textures/icon.png",
        Scenes = new string[] { "TangoSDK/Examples/Scenes/ExperimentalVirtualReality.unity" },
        BundleIdentifier = "com.projecttango.experimental.virtualreality"
    };

    private static BuildUtil.PackageSettings sdkPackageUnity5 = new BuildUtil.PackageSettings
    {
        PackageName = "TangoSDK_Unity5",
        Directories = new string[]
        {
            "Google-Unity", "Plugins", "TangoPrefabs", "TangoSDK/Core", "TangoSDK/Editor", "TangoSDK/Examples",
            "TangoSDK/TangoSupport", "TangoSDK/TangoUX"
        }
    };

    private static BuildUtil.PackageSettings sdkPackageUnity4 = new BuildUtil.PackageSettings
    {
        PackageName = "TangoSDK_Unity4",
        Directories = new string[]
        {
            "Google-Unity", "Plugins", "TangoPrefabs", "TangoSDK/Core", "TangoSDK/Editor",
            "TangoSDK/TangoSupport", "TangoSDK/TangoUX"
        }
    };
    
    /// <summary>
    /// Builds all the appropriate APKs for this project.
    /// </summary>
    [MenuItem("Tango/Build/All", false, 1)]
    public static void BuildAll()
    {
        BuildUtil.BuildAPK(examplesAPK);
        BuildUtil.BuildAPK(augmentedRealityAPK);
        BuildUtil.BuildAPK(meshBuilderAPK);
        BuildUtil.BuildAPK(virtualRealityAPK);
        BuildSdkPackage();
    }
    
    /// <summary>
    /// Function for UI.
    /// </summary>
    [MenuItem("Tango/Build/SDK Packages", false, 21)]
    public static void BuildSdkPackage()
    {
        // Write out a version file.
        string filePath = Application.dataPath + TangoSDKAbout.TANGO_VERSION_DATA_PATH;
        using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        {
            binaryWriter.Write(GitHelpers.GetTagInfo());
            binaryWriter.Write(GitHelpers.GetPrettyGitHash());
            binaryWriter.Write(GitHelpers.GetRemoteBranchName());
        }

        BuildUtil.BuildPackage(sdkPackageUnity5);
        BuildUtil.BuildPackage(sdkPackageUnity4);
    }
    
    /// <summary>
    /// Function for UI.
    /// </summary>
    [MenuItem("Tango/Build/Examples")]
    public static void BuildExamples()
    {
        BuildUtil.BuildAPK(examplesAPK);
    }
    
    /// <summary>
    /// Function for UI.
    /// </summary>
    [MenuItem("Tango/Build/Augmented Reality")]
    public static void BuildAugmentedReality()
    {
        BuildUtil.BuildAPK(augmentedRealityAPK);
    }
    
    /// <summary>
    /// Function for UI.
    /// </summary>
    [MenuItem("Tango/Build/Mesh Builder")]
    public static void BuildMeshBuilder()
    {
        BuildUtil.BuildAPK(meshBuilderAPK);
    }
    
    /// <summary>
    /// Function for UI.
    /// </summary>
    [MenuItem("Tango/Build/Virtual Reality")]
    public static void BuildVirtualReality()
    {
        BuildUtil.BuildAPK(virtualRealityAPK);
    }
}
