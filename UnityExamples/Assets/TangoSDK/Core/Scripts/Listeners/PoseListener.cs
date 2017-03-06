//-----------------------------------------------------------------------
// <copyright file="PoseListener.cs" company="Google">
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
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango pose events.
    /// </summary>
    /// <param name="poseData">The pose data from Tango.</param>
    internal delegate void OnTangoPoseAvailableEventHandler(TangoPoseData poseData);

    /// <summary>
    /// Marshals Tango pose data between the C callbacks in one thread and the main Unity thread.
    /// </summary>
    internal static class PoseListener
    {
        /// <summary>
        /// The lock object used as a mutex.
        /// </summary>
        private static System.Object m_lockObject = new System.Object();

        /// <summary>
        /// Called when a new Tango pose is available.
        /// </summary>
        private static PoseProvider.APIOnPoseAvailable m_poseAvailableCallback;

        private static TangoPoseData m_motionTrackingData = new TangoPoseData();
        private static TangoPoseData m_areaLearningData = new TangoPoseData();
        private static TangoPoseData m_relocalizationData = new TangoPoseData();
        private static TangoPoseData m_cloudPoseData = new TangoPoseData();
        private static OnTangoPoseAvailableEventHandler m_onTangoPoseAvailable;

        private static bool m_isMotionTrackingPoseAvailable = false;
        private static bool m_isAreaLearningPoseAvailable = false;
        private static bool m_isRelocalizationPoseAvailable = false;
        private static bool m_isCloudPoseAvailable = false;

#if UNITY_EDITOR
        private static double m_mostRecentEmulatedRelocalizationTimestamp;
#endif

        /// <summary>
        /// Initializes the <see cref="Tango.PoseListener"/> class.
        /// </summary>
        static PoseListener()
        {
            Reset();
        }

        /// <summary>
        /// Stop getting Tango pose callbacks.
        /// </summary>
        internal static void Reset()
        {
            // Avoid calling into tango_client_api before the correct library is loaded.
            if (m_poseAvailableCallback != null)
            {
                PoseProvider.ClearCallback();
            }

            m_poseAvailableCallback = null;
            m_motionTrackingData = new TangoPoseData();
            m_areaLearningData = new TangoPoseData();
            m_relocalizationData = new TangoPoseData();
            m_cloudPoseData = new TangoPoseData();
            m_onTangoPoseAvailable = null;
            m_isMotionTrackingPoseAvailable = false;
            m_isAreaLearningPoseAvailable = false;
            m_isRelocalizationPoseAvailable = false;
            m_isCloudPoseAvailable = false;

#if UNITY_EDITOR
            m_mostRecentEmulatedRelocalizationTimestamp = -1;
#endif
        }

        /// <summary>
        /// Register to get Tango pose callbacks for specific reference frames.
        /// 
        /// NOTE: Tango pose callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        /// <param name="framePairs">The reference frames to get callbacks for.</param>
        internal static void SetCallback(TangoCoordinateFramePair[] framePairs)
        {
            if (m_poseAvailableCallback != null)
            {
                Debug.Log("PoseListener.SetCallback() called when callback is already set.");
                return;
            }
            
            Debug.Log("PoseListener.SetCallback()");
            m_poseAvailableCallback = new PoseProvider.APIOnPoseAvailable(_OnPoseAvailable);
            PoseProvider.SetCallback(framePairs, m_poseAvailableCallback);
        }

        /// <summary>
        /// Raise a Tango pose event if there is new data.
        /// </summary>
        /// <param name="emulateAreaDescriptions">If set, Area description poses are emulated.</param>
        internal static void SendIfAvailable(bool emulateAreaDescriptions)
        {
            if (m_poseAvailableCallback == null)
            {
                return;
            }

#if UNITY_EDITOR
            lock (m_lockObject)
            {
                if (PoseProvider.m_emulationIsDirty)
                {
                    PoseProvider.m_emulationIsDirty = false;

                    if (m_onTangoPoseAvailable != null)
                    {
                        TangoCoordinateFramePair framePair;

                        framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                        framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                        PoseProvider.GetPoseAtTime(m_motionTrackingData, 0, framePair);
                        m_isMotionTrackingPoseAvailable = true;

                        if (emulateAreaDescriptions)
                        {
                            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                            framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                            PoseProvider.GetPoseAtTime(m_areaLearningData, 0, framePair);
                            if (m_areaLearningData.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
                            {
                                m_isAreaLearningPoseAvailable = true;
                            }

                            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                            framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                            PoseProvider.GetPoseAtTime(m_relocalizationData, 0, framePair);
                            if (m_relocalizationData.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID
                                && m_relocalizationData.timestamp != m_mostRecentEmulatedRelocalizationTimestamp)
                            {
                                m_mostRecentEmulatedRelocalizationTimestamp = m_relocalizationData.timestamp;
                                m_isRelocalizationPoseAvailable = true;
                            }
                        }
                    }
                }
            }
#endif

            if (m_onTangoPoseAvailable != null)
            {
                // NOTE: If this becomes a performance issue, this could be changed to use 
                // Interlocked.CompareExchange to "consume" the motion tracking data.
                lock (m_lockObject)
                {
                    if (m_isMotionTrackingPoseAvailable)
                    {
                        m_onTangoPoseAvailable(m_motionTrackingData);
                        m_isMotionTrackingPoseAvailable = false;
                    }

                    if (m_isAreaLearningPoseAvailable)
                    {
                        m_onTangoPoseAvailable(m_areaLearningData);
                        m_isAreaLearningPoseAvailable = false;
                    }

                    if (m_isRelocalizationPoseAvailable)
                    {
                        m_onTangoPoseAvailable(m_relocalizationData);
                        m_isRelocalizationPoseAvailable = false;
                    }

                    if (m_isCloudPoseAvailable)
                    {
                        m_onTangoPoseAvailable(m_cloudPoseData);
                        m_isCloudPoseAvailable = false;
                    }
                }
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango pose event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterTangoPoseAvailable(OnTangoPoseAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoPoseAvailable += handler;
            }
        }
        
        /// <summary>
        /// Unregister a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterTangoPoseAvailable(OnTangoPoseAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoPoseAvailable -= handler;
            }
        }

        /// <summary>
        /// Handle the callback sent by the Tango Service when a new pose is sampled.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="pose">The pose data returned from Tango.</param>
        [AOT.MonoPInvokeCallback(typeof(PoseProvider.APIOnPoseAvailable))]
        private static void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose)
        {
            if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // MotionTracking
                lock (m_lockObject)
                {
                    m_motionTrackingData.DeepCopy(pose);
                    m_isMotionTrackingPoseAvailable = true;
                }
            }
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // ADF Localized
                lock (m_lockObject)
                {
                    m_areaLearningData.DeepCopy(pose);
                    m_isAreaLearningPoseAvailable = true;
                }
            }
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
            {
                // Relocalized against ADF
                lock (m_lockObject)
                {
                    m_relocalizationData.DeepCopy(pose);
                    m_isRelocalizationPoseAvailable = true;
                }
            }
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_GLOBAL_WGS84 &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // Cloud ADF localized
                lock (m_lockObject)
                {
                    m_cloudPoseData.DeepCopy(pose);
                    m_isCloudPoseAvailable = true;
                }
            }
        }
    }
}
