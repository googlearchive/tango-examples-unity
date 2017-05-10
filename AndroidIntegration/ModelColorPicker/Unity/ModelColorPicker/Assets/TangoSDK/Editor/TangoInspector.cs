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
        if (m_tangoApplication.m_autoConnectToService && m_tangoApplication.m_enableAreaDescriptions &&
            !m_tangoApplication.m_enableDriftCorrection)
        {
            EditorGUILayout.HelpBox("Note that auto-connect does not supply a chance "
                                    + "to specify an Area Description.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        _DrawMotionTrackingOptions(m_tangoApplication);
        _DrawAreaDescriptionOptions(m_tangoApplication);
        _DrawDepthOptions(m_tangoApplication);
        _DrawVideoOverlayOptions(m_tangoApplication);
        _Draw3DReconstructionOptions(m_tangoApplication);
        _DrawPerformanceOptions(m_tangoApplication);
        _DrawEmulationOptions(m_tangoApplication);
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

        // Fixup the old state of TangoApplication before there were two checkboxes.  If only m_enableVideoOverlay was
        // set, then that meant to use the Byte Buffer method.
        if (m_tangoApplication.m_enableVideoOverlay && !m_tangoApplication.m_videoOverlayUseTextureMethod
            && !m_tangoApplication.m_videoOverlayUseYUVTextureIdMethod
            && !m_tangoApplication.m_videoOverlayUseByteBufferMethod)
        {
            m_tangoApplication.m_videoOverlayUseByteBufferMethod = true;
        }
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
        string[] options = new string[]
        {
            "Motion Tracking",
            "Motion Tracking (with Drift Correction)",
            "Local Area Description (Load Existing)",
            "Local Area Description (Learning)",
            "Cloud Area Description"
        };
        int selectedOption = 0;
        if (tangoApplication.m_enableDriftCorrection)
        {
            selectedOption = 1;
        }
        else if (tangoApplication.m_enableAreaDescriptions)
        {
            if (tangoApplication.m_areaDescriptionLearningMode)
            {
                selectedOption = 3;
            }
            else if (tangoApplication.m_enableCloudADF)
            {
                selectedOption = 4;
            }
            else
            {
                selectedOption = 2;
            }
        }

        switch (EditorGUILayout.Popup("Pose Mode", selectedOption, options))
        {
        case 1: // motion tracking with drift correction
            tangoApplication.m_enableDriftCorrection = true;
            tangoApplication.m_enableAreaDescriptions = false;
            tangoApplication.m_areaDescriptionLearningMode = false;
            tangoApplication.m_enableCloudADF = false;
            break;
        case 2: // area learning, load existing local
            tangoApplication.m_enableDriftCorrection = false;
            tangoApplication.m_enableAreaDescriptions = true;
            tangoApplication.m_areaDescriptionLearningMode = false;
            tangoApplication.m_enableCloudADF = false;
            break;
        case 3: // area learning, local learning mode
            tangoApplication.m_enableDriftCorrection = false;
            tangoApplication.m_enableAreaDescriptions = true;
            tangoApplication.m_areaDescriptionLearningMode = true;
            tangoApplication.m_enableCloudADF = false;
            break;
        case 4: // area learning, cloud mode
            tangoApplication.m_enableDriftCorrection = false;
            tangoApplication.m_enableAreaDescriptions = true;
            tangoApplication.m_areaDescriptionLearningMode = false;
            tangoApplication.m_enableCloudADF = true;
            break;
        default: // case 0, motion tracking
            tangoApplication.m_enableDriftCorrection = false;
            tangoApplication.m_enableAreaDescriptions = false;
            tangoApplication.m_areaDescriptionLearningMode = false;
            tangoApplication.m_enableCloudADF = false;
            break;
        }

        if (m_tangoApplication.m_enableDriftCorrection)
        {
            EditorGUILayout.HelpBox("Drift correction mode is experimental.", MessageType.Warning);
        }

        if (m_tangoApplication.m_enableCloudADF)
        {
            EditorGUILayout.HelpBox("Cloud Area Descriptions is experimental.", MessageType.Warning);
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
            
            string[] options = new string[]
            {
                "Texture (ITangoCameraTexture)",
                "YUV Texture (IExperimentalTangoVideoOverlay)",
                "Raw Bytes (ITangoVideoOverlay)",
                "Texture and Raw Bytes",
                "YUV Texture and Raw Bytes",
                "Texture and YUV Texture",
                "All",
            };

            int selectedOption;
            if (tangoApplication.m_videoOverlayUseTextureMethod
                && tangoApplication.m_videoOverlayUseYUVTextureIdMethod
                && tangoApplication.m_videoOverlayUseByteBufferMethod)
            {
                selectedOption = 6;
            }
            else if (tangoApplication.m_videoOverlayUseTextureMethod
                     && tangoApplication.m_videoOverlayUseYUVTextureIdMethod)
            {
                selectedOption = 5;
            }
            else if (tangoApplication.m_videoOverlayUseYUVTextureIdMethod
                     && tangoApplication.m_videoOverlayUseByteBufferMethod)
            {
                selectedOption = 4;
            }
            else if (tangoApplication.m_videoOverlayUseTextureMethod
                     && tangoApplication.m_videoOverlayUseByteBufferMethod)
            {
                selectedOption = 3;
            }
            else if (tangoApplication.m_videoOverlayUseByteBufferMethod)
            {
                selectedOption = 2;
            }
            else if (tangoApplication.m_videoOverlayUseYUVTextureIdMethod)
            {
                selectedOption = 1;
            }
            else
            {
                selectedOption = 0;
            }

            switch (EditorGUILayout.Popup("Method", selectedOption, options))
            {
                case 0:
                    tangoApplication.m_videoOverlayUseTextureMethod = true;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = false;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = false;
                    break;
                case 1:
                    tangoApplication.m_videoOverlayUseTextureMethod = false;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = true;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = false;
                    break;
                case 2:
                    tangoApplication.m_videoOverlayUseTextureMethod = false;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = false;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = true;
                    break;
                case 3:
                    tangoApplication.m_videoOverlayUseTextureMethod = true;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = false;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = true;
                    break;
                case 4:
                    tangoApplication.m_videoOverlayUseTextureMethod = false;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = true;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = true;
                    break;
                case 5:
                    tangoApplication.m_videoOverlayUseTextureMethod = true;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = true;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = false;
                    break;
                case 6:
                    tangoApplication.m_videoOverlayUseTextureMethod = true;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = true;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = true;
                    break;
                default:
                    tangoApplication.m_videoOverlayUseTextureMethod = true;
                    tangoApplication.m_videoOverlayUseYUVTextureIdMethod = false;
                    tangoApplication.m_videoOverlayUseByteBufferMethod = false;
                    break;
            }

            EditorGUI.indentLevel--;
        }
        else
        {
            tangoApplication.m_videoOverlayUseTextureMethod = true;
            tangoApplication.m_videoOverlayUseYUVTextureIdMethod = false;
            tangoApplication.m_videoOverlayUseByteBufferMethod = false;
        }

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draw motion tracking options.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _Draw3DReconstructionOptions(TangoApplication tangoApplication)
    {
        GUILayout.Label("Enable 3D Reconstruction", GUILayout.ExpandWidth(true));
        tangoApplication.m_enable3DReconstruction = EditorGUILayout.Toggle("(Experimental)",
                                                                           tangoApplication.m_enable3DReconstruction);
        if (tangoApplication.m_enable3DReconstruction)
        {
            if (!tangoApplication.m_enableMotionTracking)
            {
                EditorGUILayout.HelpBox("Motion tracking is required for 3D Reconstruction.", MessageType.Warning);
            }

            if (!tangoApplication.m_enableDepth)
            {
                EditorGUILayout.HelpBox("Depth is required for 3D Reconstruction.", MessageType.Warning);
            }

            EditorGUI.indentLevel++;
            tangoApplication.ReconstructionMeshResolution = EditorGUILayout.FloatField(
                "Resolution (meters)", tangoApplication.ReconstructionMeshResolution);
            tangoApplication.ReconstructionMeshResolution = Mathf.Max(tangoApplication.ReconstructionMeshResolution, 0.001f);
            tangoApplication.m_3drGenerateColor = EditorGUILayout.Toggle(
                "Generate Color", tangoApplication.m_3drGenerateColor);

            if (tangoApplication.m_3drGenerateColor
                && (!tangoApplication.m_enableVideoOverlay || !tangoApplication.m_videoOverlayUseByteBufferMethod))
            {
                EditorGUILayout.HelpBox("To use 3D reconstruction with color, you must enable Video Overlay and"
                                        + " set it to \"Raw Bytes\", \"Texture and Raw Bytes\", or "
                                        + " \"YUV Texture and Raw Bytes\".", MessageType.Warning);
            }

            tangoApplication.m_3drGenerateNormal = EditorGUILayout.Toggle(
                "Generate Normals", tangoApplication.m_3drGenerateNormal);
            tangoApplication.m_3drGenerateTexCoord = EditorGUILayout.Toggle(
                "Generate UVs", tangoApplication.m_3drGenerateTexCoord);
            tangoApplication.m_3drSpaceClearing = EditorGUILayout.Toggle(
                "Space Clearing", tangoApplication.m_3drSpaceClearing);
            tangoApplication.m_3drUpdateMethod = (Tango3DReconstruction.UpdateMethod)EditorGUILayout.EnumPopup(
                "Update Method", tangoApplication.m_3drUpdateMethod);

            string tooltip = "If non-zero, any mesh that has less than this number of vertices is assumed to be "
                + "noise and will not be generated.";
            int newMinNumVertices = EditorGUILayout.IntField(new GUIContent("Mesh Min Vertices", tooltip),
                                                             tangoApplication.m_3drMinNumVertices);
            tangoApplication.m_3drMinNumVertices = Mathf.Max(newMinNumVertices, 0);

            tangoApplication.m_3drUseAreaDescriptionPose = EditorGUILayout.Toggle(
                "Use Area Description Pose", tangoApplication.m_3drUseAreaDescriptionPose);

            if (tangoApplication.m_3drUseAreaDescriptionPose && !tangoApplication.m_enableAreaDescriptions)
            {
                EditorGUILayout.HelpBox("Area Descriptions must be enabled in order for "
                                        + "3D Reconstruction to use them.", MessageType.Warning);
            }
            else if (!tangoApplication.m_3drUseAreaDescriptionPose && tangoApplication.m_enableAreaDescriptions)
            {
                EditorGUILayout.HelpBox("Area Descriptions are enabled, but \"Use Area Description Pose\" is disabled "
                                        + "for 3D Reconstruction.\n\nIf left as-is, 3D Reconstruction will use the Start of "
                                        + "Service pose, even if an area description is loaded and/or area learning is enabled.",
                                        MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draws options for performance management.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawPerformanceOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_showPerformanceOptionsInInspector =
            EditorGUILayout.Foldout(tangoApplication.m_showPerformanceOptionsInInspector, "Performance Options");
        
        if (tangoApplication.m_showPerformanceOptionsInInspector)
        {
            EditorGUI.indentLevel++;

            m_tangoApplication.m_initialPointCloudMaxPoints = EditorGUILayout.IntField(
                new GUIContent("Point Cloud Max Points", 
                           "Set an upper limit on the number of points in the point cloud. If value is 0, no limit is imposed."),
                m_tangoApplication.m_initialPointCloudMaxPoints);

            tangoApplication.m_keepScreenAwake = EditorGUILayout.Toggle("Keep Screen Awake", tangoApplication.m_keepScreenAwake);

            tangoApplication.m_adjustScreenResolution = EditorGUILayout.Toggle(
                new GUIContent("Reduce Resolution", 
                           "Whether to adjust the size of the application's main render buffer for performance reasons"),
                tangoApplication.m_adjustScreenResolution);

            EditorGUI.indentLevel++;

            GUI.enabled = tangoApplication.m_adjustScreenResolution;

            tangoApplication.m_targetResolution = EditorGUILayout.IntField(
                new GUIContent("Target Resolution", 
                           "Target resolution to reduce resolution to when m_adjustScreenResolution is enabled."),
                tangoApplication.m_targetResolution);

            string oversizedResolutionTooltip = "If true, resolution adjustment will allow adjusting to a resolution " +
                "larger than the display of the current device. This is generally discouraged.";
            tangoApplication.m_allowOversizedScreenResolutions = EditorGUILayout.Toggle(
                new GUIContent("Allow Oversized", oversizedResolutionTooltip), tangoApplication.m_allowOversizedScreenResolutions);

            GUI.enabled = true;

            if (!tangoApplication.m_adjustScreenResolution)
            {
                EditorGUILayout.HelpBox("Some Tango devices have very high-resolution displays.\n\n" +
                                        "Consider limiting application resolution here or elsewhere in your application.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }
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
        tangoApplication.m_allowOutOfDateTangoAPI = EditorGUILayout.Toggle(
            "Allow out of date API", m_tangoApplication.m_allowOutOfDateTangoAPI);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draws editor emulation options.
    /// 
    /// These will only have any effect while in the Unity Editor.
    /// </summary>
    /// <param name="tangoApplication">Tango application.</param>
    private void _DrawEmulationOptions(TangoApplication tangoApplication)
    {
        tangoApplication.m_showEmulationOptionsInInspector =
            EditorGUILayout.Foldout(tangoApplication.m_showEmulationOptionsInInspector, "Editor Emulation");

        if (tangoApplication.m_showEmulationOptionsInInspector)
        {
            EditorGUI.indentLevel++;
            tangoApplication.m_doSlowEmulation = EditorGUILayout.Toggle(
                new GUIContent("Depth and Video",
                               "Simulate depth and color camera data based on a specified mesh. Disable for editor performance."),
                tangoApplication.m_doSlowEmulation);
            
            if (tangoApplication.m_doSlowEmulation)
            {
                EditorGUI.indentLevel++;
                tangoApplication.m_emulationEnvironment = (Mesh)EditorGUILayout.ObjectField(
                    new GUIContent("Mesh For Emulation", "Mesh to use as the world when simulating color camera and depth data."),
                    m_tangoApplication.m_emulationEnvironment, typeof(Mesh), false);
                tangoApplication.m_emulationEnvironmentTexture = (Texture)EditorGUILayout.ObjectField(
                    new GUIContent("Texture for Emulation", "(Optional) Texture to use on emulated environment mesh."),
                    m_tangoApplication.m_emulationEnvironmentTexture, typeof(Texture), false);
                m_tangoApplication.m_emulationVideoOverlaySimpleLighting = EditorGUILayout.Toggle(
                    new GUIContent("Simulate Lighting", "Use simple lighting in simulating camera feed"),
                    m_tangoApplication.m_emulationVideoOverlaySimpleLighting);
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
            }

            tangoApplication.m_emulatedAreaDescriptionStartOffset = EditorGUILayout.Vector3Field(
                new GUIContent("Area Description Offset",
                           "Simulate difference between Start of Service and Area Description origins with a simple positional offset"),
                tangoApplication.m_emulatedAreaDescriptionStartOffset);

            EditorGUI.indentLevel--;
        }
    }
}
