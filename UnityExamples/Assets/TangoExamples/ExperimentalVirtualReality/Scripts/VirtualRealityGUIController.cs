// <copyright file="VirtualRealityGUIController.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// Extra GUI controls.
/// </summary>
public class VirtualRealityGUIController : MonoBehaviour
{
    public TangoDeltaPoseController poseController;

    /// <summary>
    /// Unity 2D GUI.
    /// </summary>
    private void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 200, 50, 150, 80), "Reset Position"))
        {
            poseController.transform.position = Vector3.zero;
            poseController.SetPose(Vector3.zero, poseController.transform.rotation);
        }
    }
}