/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections;
using UnityEditor;
using UnityEngine;
using Tango;

[CustomEditor(typeof(TangoApplication))]
public class TangoInspector : Editor
{
    TangoApplication m_tangoApplication;

    /// <summary>
    /// Raises the enable event.
    /// </summary>
    private void OnEnable()
    {
        m_tangoApplication = (TangoApplication)target;
    }

    /// <summary>
    /// Raises the inspector GUI event.
    /// </summary>
    public override void OnInspectorGUI()
    {
        _DrawMotionTrackingOptions(m_tangoApplication);
        _DrawDepthOptions(m_tangoApplication);
        _DrawVideoOverlayOptions(m_tangoApplication);
        _DrawUXLibraryOptions(m_tangoApplication);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(m_tangoApplication);
        }
    }

    /// <summary>
    /// Draw motion tracking options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawMotionTrackingOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_enableMotionTracking = EditorGUILayout.Toggle("Enable Motion Tracking", 
                                                                         tangoApplication.m_enableMotionTracking);
        if (tangoApplication.m_enableMotionTracking)
        {
            EditorGUI.indentLevel++;
            tangoApplication.m_motionTrackingAutoReset = EditorGUILayout.Toggle("Auto Reset", 
                                                                                tangoApplication.m_motionTrackingAutoReset);

            tangoApplication.m_useLowLatencyIMUIntegration = EditorGUILayout.Toggle("Low Latency Pose", 
                                                                                	tangoApplication.m_useLowLatencyIMUIntegration);
			
            tangoApplication.m_enableAreaLearning = EditorGUILayout.Toggle("Area Learning", 
                                                                           tangoApplication.m_enableAreaLearning);
            if (tangoApplication.m_enableAreaLearning)
            {
                EditorGUI.indentLevel++;
                tangoApplication.m_useExperimentalADF = EditorGUILayout.Toggle("High Accuracy (Experimental)", 
                                                                               tangoApplication.m_useExperimentalADF);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.indentLevel--;
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
        tangoApplication.m_enableVideoOverlay = EditorGUILayout.Toggle("Enable Video Overlay", 
                                                                       tangoApplication.m_enableVideoOverlay);
        if (tangoApplication.m_enableVideoOverlay)
        {
            EditorGUI.indentLevel++;
            tangoApplication.m_useExperimentalVideoOverlay = EditorGUILayout.Toggle("GPU Accelerated (Experimental)", 
                                                                                    tangoApplication.m_useExperimentalVideoOverlay);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draw UX library options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawUXLibraryOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_enableUXLibrary = EditorGUILayout.Toggle("Enable UX Library", tangoApplication.m_enableUXLibrary);
        if (tangoApplication.m_enableUXLibrary)
        {
            tangoApplication.m_drawDefaultUXExceptions = EditorGUILayout.Toggle("Show default UX popups",
                                                                                tangoApplication.m_drawDefaultUXExceptions);
        }
    }
}