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
    public class PoseListener
    {
        /// <summary>
        /// Called when a new Tango pose is available.
        /// </summary>
        private Tango.PoseProvider.TangoService_onPoseAvailable m_poseAvailableCallback;

        private TangoPoseData m_motionTrackingData = new TangoPoseData();
        private TangoPoseData m_areaLearningData = new TangoPoseData();
        private TangoPoseData m_relocalizationData = new TangoPoseData();
        private OnTangoPoseAvailableEventHandler m_onTangoPoseAvailable;

        private bool m_isMotionTrackingPoseAvailable = false;
        private bool m_isAreaLearningPoseAvailable = false;
        private bool m_isRelocalizaitonPoseAvailable = false;
        private object m_lockObject = new object();

#if UNITY_EDITOR
        private double m_mostRecentEmulatedRelocalizationTimestamp = -1.0;
#endif

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PoseListener"/> is using auto reset.
        /// </summary>
        /// <value><c>true</c> if auto reset; otherwise, <c>false</c>.</value>
        internal bool AutoReset
        {
            get;
            set;
        }

        /// <summary>
        /// Raise a Tango pose event if there is new data.
        /// </summary>
        /// <param name="emulateAreaDescriptions">If set, Area description poses are emulated.</param>
        internal void SendPoseIfAvailable(bool emulateAreaDescriptions)
        {
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
                                m_isRelocalizaitonPoseAvailable = true;
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

                    if (m_isRelocalizaitonPoseAvailable)
                    {
                        m_onTangoPoseAvailable(m_relocalizationData);
                        m_isRelocalizaitonPoseAvailable = false;
                    }
                }
            }
        }

        /// <summary>
        /// Register to get Tango pose callbacks for specific reference frames.
        /// 
        /// NOTE: Tango pose callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        /// <param name="framePairs">The reference frames to get callbacks for.</param>
        internal void SetCallback(TangoCoordinateFramePair[] framePairs)
        {
            m_poseAvailableCallback = new Tango.PoseProvider.TangoService_onPoseAvailable(_OnPoseAvailable);
            Tango.PoseProvider.SetCallback(framePairs, m_poseAvailableCallback);
        }
        
        /// <summary>
        /// Register a Unity main thread handler for the Tango pose event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterTangoPoseAvailable(OnTangoPoseAvailableEventHandler handler)
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
        internal void UnregisterTangoPoseAvailable(OnTangoPoseAvailableEventHandler handler)
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
        private void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose)
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
                    m_isRelocalizaitonPoseAvailable = true;
                }
            }
        }
    }
}
