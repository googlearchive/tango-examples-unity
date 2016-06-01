//-----------------------------------------------------------------------
// <copyright file="TangoDeltaPoseControllerInspector.cs" company="Google">
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
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for a TangoDeltaPoseController component.
/// </summary>
[CustomEditor(typeof(TangoDeltaPoseController))]
public class TangoDeltaPoseControllerInspector : Editor
{
    /// <summary>
    /// The scene's TangoApplication, if any.
    /// </summary>
    private TangoApplication m_tangoApplication;
    
    /// <summary>
    /// Inspector GUI event for immediate-mode Editor GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TangoDeltaPoseController inspectedObject = (TangoDeltaPoseController)target;

        if (TangoPrefabInspectorHelper.CheckForTangoApplication(inspectedObject, ref m_tangoApplication))
        {
            TangoPrefabInspectorHelper.CheckMotionTrackingPermissions(m_tangoApplication);

            TangoPrefabInspectorHelper.CheckAreaDescriptionPermissions(m_tangoApplication,
                                                                        inspectedObject.m_useAreaDescriptionPose);
        }
    }
}
