//-----------------------------------------------------------------------
// <copyright file="BuildTools.cs" company="Google">
//   
// Copyright 2016 Google Inc. All Rights Reserved.
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
/// <FULLPATH_UNITY.APP>/Contents/MacOS/Unity -batchmode -projectPath <FULLPATH> -executeMethod <METHOD_TO_RUN> -quit
/// 
/// For example:
/// /Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -projectPath ~/Unity/tango-examples-unity/UnityExamples/ -executeMethod BuildTools.BuildAll -quit
/// 
/// For more info, goto <http://docs.unity3d.com/Manual/CommandLineArguments.html>
/// </summary>
public class BuildTools
{
    private static BuildUtil.APKSettings tangoCardboard = new BuildUtil.APKSettings
    {
        ProjectName = "Tango Cardboard VR",
        Icon = "Textures/tango_logo.png",
        Scenes = new string[] { "Scenes/TangoCardboard.unity" },
        BundleIdentifier = "com.projecttango.tangocardboard"
    };

    /// <summary>
    /// Builds all the appropriate APKs for this project.
    /// </summary>
    [MenuItem("Tango/Build/All", false, 1)]
    public static void BuildAll()
    {
        BuildUtil.BuildAPK(tangoCardboard);
    }
}
