//-----------------------------------------------------------------------
// <copyright file="TangoInspector.cs" company="Google">
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
/// Custom editor for the TangoApplication.
/// </summary>
[CustomEditor(typeof(TangoApplication))]
public class TangoInspector : Editor
{
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Raises the inspector GUI event.
    /// </summary>
    public override void OnInspectorGUI()
    {
        m_tangoApplication.m_autoConnectToService = EditorGUILayout.Toggle("Auto-connect to Service",
                                                                           m_tangoApplication.m_autoConnectToService);
        EditorGUILayout.Space();

        _DrawMotionTrackingOptions(m_tangoApplication);
        _DrawAreaDescriptionOptions(m_tangoApplication);
        _DrawDepthOptions(m_tangoApplication);
        _DrawVideoOverlayOptions(m_tangoApplication);
        _DrawDevelopmentOptions(m_tangoApplication);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(m_tangoApplication);
        }
    }

    /// <summary>
    /// Raises the enable event.
    /// </summary>
    private void OnEnable()
    {
        m_tangoApplication = (TangoApplication)target;
    }

    /// <summary>
    /// Draw motion tracking options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawMotionTrackingOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_enableMotionTracking = EditorGUILayout.Toggle(
            "Enable Motion Tracking", tangoApplication.m_enableMotionTracking);
        if (tangoApplication.m_enableMotionTracking)
        {
            ++EditorGUI.indentLevel;
            tangoApplication.m_motionTrackingAutoReset = EditorGUILayout.Toggle(
                "Auto Reset", tangoApplication.m_motionTrackingAutoReset);
            --EditorGUI.indentLevel;
        }

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draw area description options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawAreaDescriptionOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_enableAreaDescriptions = EditorGUILayout.Toggle(
            "Enable Area Descriptions", tangoApplication.m_enableAreaDescriptions);

        if (tangoApplication.m_enableAreaDescriptions)
        {
            ++EditorGUI.indentLevel;
            tangoApplication.m_areaDescriptionLearningMode = EditorGUILayout.Toggle(
                "Learning Mode", tangoApplication.m_areaDescriptionLearningMode);
            --EditorGUI.indentLevel;
        }

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draw depth options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawDepthOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_enableDepth = EditorGUILayout.Toggle("Enable Depth", tangoApplication.m_enableDepth);
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draw video overlay options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawVideoOverlayOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_enableVideoOverlay = EditorGUILayout.Toggle(
            "Enable Video Overlay", tangoApplication.m_enableVideoOverlay);
        if (tangoApplication.m_enableVideoOverlay)
        {
            EditorGUI.indentLevel++;
            tangoApplication.m_videoOverlayUseTextureIdMethod = EditorGUILayout.Toggle("TextureID Method", 
                                                                            tangoApplication.m_videoOverlayUseTextureIdMethod);
            tangoApplication.m_videoOverlayUseByteBufferMethod = EditorGUILayout.Toggle("Byte Buffer Method", 
                                                                            tangoApplication.m_videoOverlayUseByteBufferMethod);
            EditorGUI.indentLevel--;
        }
        else
        {
            tangoApplication.m_videoOverlayUseTextureIdMethod = false;
            tangoApplication.m_videoOverlayUseByteBufferMethod = false;
        }

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draws development options.
    /// 
    /// These should only be set while in development.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawDevelopmentOptions(TangoApplication tangoApplication)
    {
        GUILayout.Label("Development Options (Disable these before publishing)", GUILayout.ExpandWidth(true));
        EditorGUI.indentLevel++;
        tangoApplication.m_allowOutOfDateTangoAPI = EditorGUILayout.Toggle("Allow out of date API",
                                                                           m_tangoApplication.m_allowOutOfDateTangoAPI);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
    }
}
