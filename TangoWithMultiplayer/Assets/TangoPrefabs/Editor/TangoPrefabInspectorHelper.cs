//-----------------------------------------------------------------------
// <copyright file="TangoPrefabInspectorHelper.cs" company="Google">
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

namespace Tango
{
    using System.Collections;
    using Tango;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Helper methods to display GUI warnings for common setup incompatibilities 
    /// in Tango prefab components.
    /// </summary>
    internal static class TangoPrefabInspectorHelper
    {
        /// <summary>
        /// Check for a usable tango application component in the scene. Draw warning if 
        /// one could not be found.
        /// 
        /// Should be called first before using any other TangoPrefabInspectorHelper function
        /// during a given frame, to determine if a valid TangoApplication reference exists
        /// with which to call other TangoPrefabInspectorHelper methods.
        /// </summary>
        /// <returns><c>true</c>, if a tango application on an active GameObject can be identified, 
        /// <c>false</c> otherwise.</returns>
        /// <param name="inspectedBehaviour">Prefab behavior that's being inspected.</param>
        /// <param name="tangoApplication">Prefab inspector's reference to Tango Application, or
        /// null if no Tango Application on an active GameObject can be identified.</param>
        public static bool CheckForTangoApplication(MonoBehaviour inspectedBehaviour,
                                                    ref TangoApplication tangoApplication)
        {
            if (tangoApplication == null || !tangoApplication.gameObject.activeInHierarchy)
            {
                tangoApplication = GameObject.FindObjectOfType<TangoApplication>();
            }
            
            // Note: .isActiveAndEnabled is the appropriate thing to check here because all Register() 
            // calls on existing Tango prefabs are called in Start(), which won't occur until both the
            // behaviour is enabled and the game object it is attached to is active.
            // 
            // Conversely, if any of the tango prefabs called Register() in Awake(), the correct thing
            // to check against would be .gameObject.activeInHeirarchy, since Awake is called when the
            // game object it is attached to is active, regardless of whether the behaviour itself is
            // enabled.
            if (tangoApplication == null && inspectedBehaviour.isActiveAndEnabled)
            {
                EditorGUILayout.HelpBox("Could not find an active TangoApplication component in the scene.\n\n"
                                        + "Component will not function correctly if it cannot find "
                                        + "an active TangoApplication component at runtime.",
                                        MessageType.Warning);
                return false;
            }
            
            return tangoApplication != null;
        }

        /// <summary>
        /// Checks whether motion tracking permissions are selected and draws a warning if they are not.
        /// </summary>
        /// <returns><c>true</c>, if motion tracking permissions are enabled, <c>false</c> otherwise.</returns>
        /// <param name="tangoApplication">Prefab inspector's reference to Tango Application.</param>
        public static bool CheckMotionTrackingPermissions(TangoApplication tangoApplication)
        {
            bool hasPermissions = tangoApplication.m_enableMotionTracking;
            
            if (!hasPermissions)
            {
                EditorGUILayout.HelpBox("This component needs motion tracking to be enabled in "
                                        + "TangoApplication to function.",
                                        MessageType.Warning);
            }
            
            return hasPermissions;
        }
        
        /// <summary>
        /// Checks whether area description permissions are selected (on the assumption we're
        /// dealing with a prefab that has an m_useAreaDescriptionPose option) and draws
        /// a warning if they seem to be set inappropriately.
        /// </summary>
        /// <returns><c>true</c>, if area description permissions are enabled, <c>false</c> otherwise.</returns>
        /// <param name="tangoApplication">Prefab inspector's reference to Tango Application.</param>
        /// <param name="shouldUsePermissions">If set to <c>true</c> should use permissions.</param>
        public static bool CheckAreaDescriptionPermissions(TangoApplication tangoApplication,
                                                           bool shouldUsePermissions)
        {
            bool hasPermissions = tangoApplication.m_enableAreaDescriptions;

            if (!hasPermissions && shouldUsePermissions)
            {
                EditorGUILayout.HelpBox("\"Use Area Description Pose\" option selected but active "
                                        + "TangoApplication component does not have Area "
                                        + "Descriptions enabled.",
                                        MessageType.Warning);
            }
            else if (hasPermissions && !shouldUsePermissions)
            {
                EditorGUILayout.HelpBox("TangoApplication has Area Descriptions enabled but \"Use "
                                        + "Area Description Pose\" option is not selected.\n\n"
                                        + "If left as-is, this script will use the Start of Service "
                                        + "pose even if an area description is loaded and/or area learning is enabled",
                                        MessageType.Warning);
            }
            
            return hasPermissions == tangoApplication.m_enableAreaDescriptions;
        }

        /// <summary>
        /// Checks whether depth permissions are selected and draws a warning if they are not.
        /// </summary>
        /// <returns><c>true</c>, if depth permissions are enabled, <c>false</c> otherwise.</returns>
        /// <param name="tangoApplication">Prefab inspector's reference to Tango Application.</param>
        public static bool CheckDepthPermissions(TangoApplication tangoApplication)
        {
            bool hasPermissions = tangoApplication.m_enableDepth;
            
            if (!hasPermissions)
            {
                EditorGUILayout.HelpBox("This component needs Depth to be enabled in TangoApplication to function.",
                                        MessageType.Warning);
            }
            
            return hasPermissions;
        }
        
        /// <summary>
        /// Checks whether video overlay permissions are selected and draws a warning if they are not.
        /// </summary>
        /// <returns><c>true</c>, if appropriate video permissions are enabled, <c>false</c> otherwise.</returns>
        /// <param name="tangoApplication">Prefab inspector's reference to Tango Application.</param>
        /// <param name="textureIdMethodRequired"><c>true</c> if texture ID method is required.</param>
        /// <param name="byteBufferMethodRequired"><c>true</c> if byte buffer method is required.</param>
        public static bool CheckVideoOverlayPermissions(TangoApplication tangoApplication,
                                                        bool textureIdMethodRequired, bool byteBufferMethodRequired)
        {
            bool hasNeededVideoPermissions = tangoApplication.m_enableVideoOverlay;
            
            if (textureIdMethodRequired && !tangoApplication.m_videoOverlayUseTextureMethod)
            {
                hasNeededVideoPermissions = false;
            }
            else if (byteBufferMethodRequired && !tangoApplication.m_videoOverlayUseByteBufferMethod)
            {
                hasNeededVideoPermissions = false;
            }
            
            if (!hasNeededVideoPermissions)
            {
                if (textureIdMethodRequired || byteBufferMethodRequired)
                {
                    string requirementsString;
                    if (textureIdMethodRequired && byteBufferMethodRequired)
                    {
                        requirementsString = "\"Both\"";
                    }
                    else if (textureIdMethodRequired)
                    {
                        requirementsString = "\"Texture ID\" or \"Both\"";
                    }
                    else
                    {
                        requirementsString = "\"Raw Bytes\" or \"Both\"";
                    }
                    
                    EditorGUILayout.HelpBox("This component needs Video Overlay to be enabled and set to method of "
                                            + requirementsString
                                            + " in TangoApplication to function.",
                                            MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("This component needs Video Overlay to be enabled in " 
                                            + "TangoApplication to function.",
                                            MessageType.Warning);
                }
            }
            
            return hasNeededVideoPermissions;
        }
        
        /// <summary>
        /// Checks whether 3D Reconstruction permissions are selected and draws a warning if they are not.
        /// </summary>
        /// <returns><c>true</c>, if 3D Reconstruction permissions are enabled, <c>false</c> otherwise.</returns>
        /// <param name="tangoApplication">Prefab inspector's reference to Tango Application.</param>
        public static bool Check3dReconstructionPermissions(TangoApplication tangoApplication)
        {
            bool hasPermissions = tangoApplication.m_enable3DReconstruction;
            
            if (!hasPermissions)
            {
                EditorGUILayout.HelpBox("This component needs 3D Reconstruction to be enabled in "
                                        + "TangoApplication to function.",
                                        MessageType.Warning);
            }
            
            return hasPermissions;
        }
    }
}
