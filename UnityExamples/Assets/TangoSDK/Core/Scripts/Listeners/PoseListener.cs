//-----------------------------------------------------------------------
// <copyright file="PoseListener.cs" company="Google">
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
using System.Collections.Generic;
using UnityEngine;

namespace Tango
{
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
        private const int SIZE_OF_POSE_DATA_POOL = 3;

        /// <summary>
        /// Called when a new Tango pose is available.
        /// </summary>
        private Tango.PoseProvider.TangoService_onPoseAvailable m_poseAvailableCallback;

        private TangoPoseData m_motionTrackingData = null;
        private TangoPoseData m_areaLearningData = null;
        private TangoPoseData m_relocalizationData = null;
        private OnTangoPoseAvailableEventHandler m_onTangoPoseAvailable;
        private Stack<TangoPoseData> m_poseDataPool;
        private bool m_isDirty = false;
        private object m_lockObject = new object();

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
        /// Initializes a new instance of the <see cref="Tango.PoseListener"/> class.
        /// </summary>
        internal PoseListener()
        {
            m_poseDataPool = new Stack<TangoPoseData>();

            // Add pre-allocated TangoPoseData objects to the
            // pool stack.
            for (int i = 0; i < SIZE_OF_POSE_DATA_POOL; ++i)
            {
                TangoPoseData emptyPose = new TangoPoseData();
                m_poseDataPool.Push(emptyPose);
            }
        }

        /// <summary>
        /// Raise a Tango pose event if there is new data.
        /// </summary>
        internal void SendPoseIfAvailable()
        {
#if UNITY_EDITOR
            if (TangoApplication.m_mouseEmulationViaPoseUpdates)
            {
                PoseProvider.UpdateTangoEmulation();
                lock (m_lockObject)
                {
                    if (m_onTangoPoseAvailable != null)
                    {
                        FillEmulatedPoseData(ref m_motionTrackingData, 
                                             TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE,
                                             TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE);
                        FillEmulatedPoseData(ref m_areaLearningData,
                                             TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION,
                                             TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE);
                        m_isDirty = true;
                    }
                }
            }
#endif

            if (m_isDirty)
            {
                if (m_onTangoPoseAvailable != null)
                {
                    // NOTE: If this becomes a performance issue, this could be changed to use 
                    // Interlocked.CompareExchange to "consume" the motion tracking data.
                    lock (m_lockObject)
                    {
                        if (m_motionTrackingData != null)
                        {
                            m_onTangoPoseAvailable(m_motionTrackingData);
                            m_poseDataPool.Push(m_motionTrackingData);
                            m_motionTrackingData = null;
                        }
                        if (m_areaLearningData != null)
                        {
                            m_onTangoPoseAvailable(m_areaLearningData);
                            m_poseDataPool.Push(m_areaLearningData);
                            m_areaLearningData = null;
                        }
                        if (m_relocalizationData != null)
                        {
                            m_onTangoPoseAvailable(m_relocalizationData);
                            m_poseDataPool.Push(m_relocalizationData);
                            m_relocalizationData = null;
                        }
                    }
                }

                m_isDirty = false;
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
        /// <param name="pose">Pose.</param>
        private void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose)
        {
            if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // MotionTracking
                lock (m_lockObject)
                {
                    // Only set new pose once the previous pose has been returned.
                    if (m_motionTrackingData == null)
                    {
                        TangoPoseData currentPose = m_poseDataPool.Pop();
                        currentPose.DeepCopy(pose);
                        m_motionTrackingData = currentPose;
                    }
                }
            }
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // ADF Localized
                lock (m_lockObject)
                {
                    // Only set new pose once the previous pose has been returned.
                    if (m_areaLearningData == null)
                    {
                        TangoPoseData currentPose = m_poseDataPool.Pop();
                        currentPose.DeepCopy(pose);
                        m_areaLearningData = currentPose;
                    }
                }
            }
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
            {
                // Relocalized against ADF
                lock (m_lockObject)
                {
                    // Only set new pose once the previous pose has been returned.
                    if (m_relocalizationData == null)
                    {
                        TangoPoseData currentPose = m_poseDataPool.Pop();
                        currentPose.DeepCopy(pose);
                        m_relocalizationData = currentPose;
                    }
                }
            }

            m_isDirty = true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Fill out <c>poseData</c> with emulated values from Tango.
        /// </summary>
        /// <param name="poseData">The poseData to fill out.</param>
        /// <param name="baseFrame">Base frame to set.</param>
        /// <param name="targetFrame">Target frame to set.</param>
        private void FillEmulatedPoseData(ref TangoPoseData poseData, TangoEnums.TangoCoordinateFrameType baseFrame,
                                          TangoEnums.TangoCoordinateFrameType targetFrame)
        {
            if (poseData == null)
            {
                TangoPoseData currentPose = m_poseDataPool.Pop();

                if (currentPose != null)
                {
                    Vector3 position;
                    Quaternion rotation;
                    PoseProvider.GetTangoEmulation(out position, out rotation);

                    currentPose.framePair.baseFrame = baseFrame;
                    currentPose.framePair.targetFrame = targetFrame;

                    currentPose.timestamp = Time.time * 1000; // timestamp is in ms, time is in sec.
                    currentPose.version = 0; // Not actually used
                    currentPose.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;

                    currentPose.translation[0] = position.x;
                    currentPose.translation[1] = position.y;
                    currentPose.translation[2] = position.z;
                    currentPose.orientation[0] = rotation.x;
                    currentPose.orientation[1] = rotation.y;
                    currentPose.orientation[2] = rotation.z;
                    currentPose.orientation[3] = rotation.w;
                    poseData = currentPose;
                }
            }
        }
#endif
    }
}
