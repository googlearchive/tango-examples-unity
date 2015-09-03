// <copyright file="BuildTools.cs" company="Google">
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
using System.Collections;
using UnityEngine;
using UnityEditor;

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
        Icon = "TangoExamples/Common/Textures/ProjectTango_Logo.png",
        Scenes = new string[]
        {
            "Scenes/AreaLearning.unity",
            "Scenes/MotionTracking.unity",
            "Scenes/PointCloud.unity",
        },
        BundleIdentifier = "com.google.projecttango.examples"
    };

    private static BuildUtil.APKSettings augmentedRealityAPK = new BuildUtil.APKSettings 
    {
        ProjectName = "Unity Augmented Reality",
        Icon = "TangoExamples/ExperimentalAugmentedReality/Textures/ar_icon.png",
        Scenes = new string[] { "Scenes/ExperimentalAugmentedReality.unity" },
        BundleIdentifier = "com.projecttango.experiments.augmentedreality"
    };

    private static BuildUtil.APKSettings meshBuilderAPK = new BuildUtil.APKSettings 
    {
        ProjectName = "Unity Mesh Builder",
        Icon = null,
        Scenes = new string[] { "Scenes/ExperimentalMeshBuilder.unity" },
        BundleIdentifier = "com.google.projecttango.meshbuilder"
    };

    private static BuildUtil.APKSettings persistentStateAPK = new BuildUtil.APKSettings
    {
        ProjectName = "Unity Persistent State",
        Icon = "TangoExamples/Common/Textures/ProjectTango_Logo.png",
        Scenes = new string[] 
        { 
            "Scenes/ExperimentalPersistentState/ExperimentalPersistentState_StartScene.unity",
            "Scenes/ExperimentalPersistentState/ExperimentalPersistentState_GameScene.unity" 
        },
        BundleIdentifier = "com.projecttango.persistentstate",
    };

    private static BuildUtil.APKSettings virtualRealityAPK = new BuildUtil.APKSettings
    {
        ProjectName = "Unity VirtualReality",
        Icon = "TangoExamples/ExperimentalVirtualReality/Textures/icon.png",
        Scenes = new string[] { "Scenes/ExperimentalVirtualReality.unity" },
        BundleIdentifier = "com.projecttango.experimental.virtualreality"
    };

    private static BuildUtil.PackageSettings sdkPackage = new BuildUtil.PackageSettings
    {
        PackageName = "TangoSDK",
        Directories = new string[] { "Google-Unity", "Plugins", "TangoPrefabs", "TangoSDK" }
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
        BuildUtil.BuildAPK(persistentStateAPK);
        BuildUtil.BuildAPK(virtualRealityAPK);
        BuildUtil.BuildPackage(sdkPackage);
    }
    
    /// <summary>
    /// Function for UI.
    /// </summary>
    [MenuItem("Tango/Build/SDK Package", false, 21)]
    public static void BuildSdkPackage()
    {
        BuildUtil.BuildPackage(sdkPackage);
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
    [MenuItem("Tango/Build/Persistent State")]
    public static void BuildPersistentState()
    {
        BuildUtil.BuildAPK(persistentStateAPK);
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
