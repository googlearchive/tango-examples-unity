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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// On tango pose available event handler.
    /// </summary>
    public delegate void OnTangoPoseAvailableEventHandler(TangoPoseData poseData);

    /// <summary>
    /// Abstract base class that can be used to
    /// automatically register for onPoseAvailable
    /// callbacks from the Tango Service.
    /// </summary>
    public class PoseListener
    {
        public Tango.PoseProvider.TangoService_onPoseAvailable m_poseAvailableCallback;

        private const int SIZE_OF_POSE_DATA_POOL = 3;
        private TangoPoseData m_motionTrackingData;
        private TangoPoseData m_areaLearningData;
        private TangoPoseData m_relocalizationData;
        private OnTangoPoseAvailableEventHandler m_onTangoPoseAvailable;
        private TangoEnums.TangoPoseStatusType m_latestPoseStatus = TangoEnums.TangoPoseStatusType.NA;
        private Stack<TangoPoseData> m_poseDataPool;
        private bool m_isDirty = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.PoseListener"/> class.
        /// </summary>
        public PoseListener()
        {
            m_motionTrackingData = null;
            m_areaLearningData = null;
            m_relocalizationData = null;
            m_poseDataPool = new Stack<TangoPoseData>();

            // Add pre-allocated TangoPoseData objects to the
            // pool stack.
            for(int i = 0; i < SIZE_OF_POSE_DATA_POOL; ++i)
            {
                TangoPoseData emptyPose = new TangoPoseData();
                m_poseDataPool.Push(emptyPose);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PoseListener"/>
        /// is using auto reset.
        /// </summary>
        /// <value><c>true</c> if auto reset; otherwise, <c>false</c>.</value>
        public bool AutoReset
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PoseListener"/> use camera intrinsics.
        /// </summary>
        /// <value><c>true</c> if use camera intrinsics; otherwise, <c>false</c>.</value>
        public bool UseCameraIntrinsics
        {
            get;
            set;
        }

        /// <summary>
        /// Sends the pose if available.
        /// </summary>
        /// <returns>The pose status if available.</returns>
        public void SendPoseIfAvailable(bool usingUXLibrary)
        {
            if(m_isDirty)
			{
				if(usingUXLibrary)
				{
					AndroidHelper.ParseTangoPoseStatus((int)m_latestPoseStatus);
                }

                if(m_onTangoPoseAvailable != null)
                {
                    if(m_motionTrackingData != null)
                    {
                        m_onTangoPoseAvailable(m_motionTrackingData);
                        m_poseDataPool.Push(m_motionTrackingData);
                        m_motionTrackingData = null;
                    }
                    if(m_areaLearningData != null)
                    {
                        m_onTangoPoseAvailable(m_areaLearningData);
                        m_poseDataPool.Push(m_areaLearningData);
                        m_areaLearningData = null;
                    }
                    if(m_relocalizationData != null)
                    {
                        m_onTangoPoseAvailable(m_relocalizationData);
                        m_poseDataPool.Push(m_relocalizationData);
                        m_relocalizationData = null;
                    }
                }

				m_isDirty = false;
            }
        }

        /// <summary>
        /// Registers the callback.
        /// </summary>
        /// <param name="framePairs">Frame pairs.</param>
        public void SetCallback(TangoCoordinateFramePair[] framePairs)
        {
            m_poseAvailableCallback = new Tango.PoseProvider.TangoService_onPoseAvailable(_OnPoseAvailable);
            Tango.PoseProvider.SetCallback(framePairs, m_poseAvailableCallback);
        }

        /// <summary>
        /// Registers for Tango pose available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterTangoPoseAvailable(OnTangoPoseAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoPoseAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters the tango pose available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterTangoPoseAvailable(OnTangoPoseAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoPoseAvailable -= handler;
            }
        }

        /// <summary>
        /// Handle the callback sent by the Tango Service
        /// when a new pose is sampled.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="pose">Pose.</param>
        private void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose)
        {
            m_latestPoseStatus = pose.status_code;

            // MotionTracking
            if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // Only set new pose once the previous pose has been returned.
                if(m_motionTrackingData == null)
                {
                    TangoPoseData currentPose = m_poseDataPool.Pop();

                    if(currentPose == null)
                    {
                        return;
                    }
                    else
                    {
                        currentPose.DeepCopy(pose);
                        m_motionTrackingData = currentPose;
                    }
                }
            }
            // ADF Localized
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // Only set new pose once the previous pose has been returned.
                if(m_areaLearningData == null)
                {
                    TangoPoseData currentPose = m_poseDataPool.Pop();
                    
                    if(currentPose == null)
                    {
                        return;
                    }
                    else
                    {
                        currentPose.DeepCopy(pose);
                        m_areaLearningData = currentPose;
                    }
                }
            } 
            // Relocalized against ADF
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
            {
                // Only set new pose once the previous pose has been returned.
                if(m_relocalizationData == null)
                {
                    TangoPoseData currentPose = m_poseDataPool.Pop();
                    
                    if(currentPose == null)
                    {
                        return;
                    }
                    else
                    {
                        currentPose.DeepCopy(pose);
                        m_relocalizationData = currentPose;
                    }
                }
            }

			m_isDirty = true;
        }
    }
}