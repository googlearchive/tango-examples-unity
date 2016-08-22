//-----------------------------------------------------------------------
// <copyright file="SceneSwitcher.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// Script that displays a scene switching UI.
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    private const int SCENE_BUTTON_SIZE_X = 300;
    private const int SCENE_BUTTON_SIZE_Y = 65;
    private const int SCENE_BUTTON_GAP_X = 5;
    private const int SCENE_BUTTON_GAP_Y = 3;

    /// <summary>
    /// The names of all the scenes this can switch between.
    /// </summary>
    private readonly string[] m_sceneNames =
    {
        "DetectTangoCore",
        "MotionTracking",
        "PointCloud",
        "AreaLearning",
        "AreaDescriptionManagement",
        "SimpleAugmentedReality",
        "PointToPoint"
    };

    /// <summary>
    /// Scene switching GUI.
    /// </summary>
    private void OnGUI()
    {
        for (int it = 0; it < m_sceneNames.Length; ++it)
        {
            Rect buttonRect = new Rect(Screen.width - SCENE_BUTTON_GAP_X - SCENE_BUTTON_SIZE_X,
                                       SCENE_BUTTON_GAP_Y + ((SCENE_BUTTON_GAP_Y + SCENE_BUTTON_SIZE_Y) * it),
                                       SCENE_BUTTON_SIZE_X,
                                       SCENE_BUTTON_SIZE_Y);
            #pragma warning disable 618
            if (GUI.Button(buttonRect, "<size=20>" + m_sceneNames[it] + "</size>")
                && Application.loadedLevelName != m_sceneNames[it])
            {
                Application.LoadLevel(m_sceneNames[it]);
            }
            #pragma warning restore 618
        }
    }
}
