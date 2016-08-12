//-----------------------------------------------------------------------
// <copyright file="EmulatedAreaDescriptionHelper.cs" company="Google">
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
#if UNITY_EDITOR
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    /// <summary>
    /// Helper for Area Description emulation.
    /// Keeps track of correct emulation settings for currently selected ADF.
    /// </summary>
    internal static class EmulatedAreaDescriptionHelper
    {
        /// <summary>
        /// Whether the Area Description -> Device frame should ever be valid in emulation.
        /// </summary>
        public static bool m_usingEmulatedDescriptionFrames;

        /// <summary>
        /// <c>true</c> if Area Description frames should be available at start, <c>false</c>
        /// if there should be a delay.
        /// </summary>
        public static bool m_areaDescriptionFramesAvailableAtStart;
        
        /// <summary>
        /// UUID of currently 'loaded' emulated area description.
        /// </summary>
        public static string m_currentUUID;

        /// <summary>
        /// Artificial offset between Start of Service and Area Description
        /// for emulation purposes.
        /// </summary>
        public static Vector3 m_emulationAreaOffset;

        /// <summary>
        /// Inits the emulation for a given area description UUID and set of Tango Application settings.
        /// </summary>
        /// <param name="uuid">UUID of the Area Description that 
        /// TangoApplication.Startup() was called with.</param>
        /// <param name="haveAreaDescriptionPermissions">If set to <c>true</c>, assume area description
        /// permissions have been requested and 'granted'.</param>
        /// <param name="learningMode">If set to <c>true</c>, service is in learning mode.</param>
        /// <param name="artificialOffset">Artificial offset fom Start of Service to Area Description.</param>
        public static void InitEmulationForUUID(string uuid, bool haveAreaDescriptionPermissions,
                                                bool learningMode, bool driftCorrection, Vector3 artificialOffset)
        {
            m_usingEmulatedDescriptionFrames = false;
            m_currentUUID = uuid;

            if (haveAreaDescriptionPermissions)
            {
                if (!string.IsNullOrEmpty(uuid))
                {
                    if (AreaDescription.ForUUID(uuid).GetMetadata() != null)
                    {
                        m_usingEmulatedDescriptionFrames = true;
                        m_areaDescriptionFramesAvailableAtStart = false;
                    }
                    else
                    {
                        m_currentUUID = string.Empty;
                        Debug.LogError("Requested Area Description UUID does not exist.");
                    }
                }
                else if (learningMode || driftCorrection)
                {
                    m_usingEmulatedDescriptionFrames = true;
                    m_areaDescriptionFramesAvailableAtStart = true;
                }
            }

            m_emulationAreaOffset = artificialOffset;
        }
    }
#endif
}