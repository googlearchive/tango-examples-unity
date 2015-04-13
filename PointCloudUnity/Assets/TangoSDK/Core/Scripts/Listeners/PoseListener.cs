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
using UnityEngine;

namespace Tango
{
    public delegate void OnTangoPoseAvailableEventHandler(TangoPoseData poseData);

    /// <summary>
    /// Abstract base class that can be used to
    /// automatically register for onPoseAvailable
    /// callbacks from the Tango Service.
    /// </summary>
    public class PoseListener
    {
        public Tango.PoseProvider.TangoService_onPoseAvailable m_poseAvailableCallback;

        private TangoPoseData m_motionTrackingData;
        private TangoPoseData m_areaLearningData;
        private TangoPoseData m_relocalizationData;
        private OnTangoPoseAvailableEventHandler m_onTangoPoseAvailable;
        private TangoEnums.TangoPoseStatusType m_latestPoseStatus = TangoEnums.TangoPoseStatusType.NA;

        private bool m_hasNewMotionTrackingData = false;
        private bool m_hasNewAreaLearningData = false;
        private bool m_hasNewRelocalizationData = false;
        private bool m_isDirty = false;

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
                    if(m_hasNewMotionTrackingData)
                    {
                        m_onTangoPoseAvailable(m_motionTrackingData);
                        m_hasNewMotionTrackingData = false;
                    }
                    if(m_hasNewAreaLearningData)
                    {
                        m_onTangoPoseAvailable(m_areaLearningData);
                        m_hasNewAreaLearningData = false;
                    }
                    if(m_hasNewRelocalizationData)
                    {
                        m_onTangoPoseAvailable(m_relocalizationData);
                        m_hasNewRelocalizationData = false;
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

            m_motionTrackingData = new TangoPoseData();
            m_areaLearningData = new TangoPoseData();
            m_relocalizationData = new TangoPoseData();
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
                m_motionTrackingData.framePair = pose.framePair;
                m_motionTrackingData.status_code = pose.status_code;
                m_motionTrackingData.orientation = pose.orientation;
                m_motionTrackingData.translation = pose.translation;
                m_motionTrackingData.timestamp = pose.timestamp;
                m_motionTrackingData.confidence = pose.confidence;
                m_motionTrackingData.accuracy = pose.accuracy;
                m_hasNewMotionTrackingData = true;
            }
            // ADF Localized
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                m_areaLearningData.framePair = pose.framePair;
                m_areaLearningData.status_code = pose.status_code;
                m_areaLearningData.orientation = pose.orientation;
                m_areaLearningData.translation = pose.translation;
                m_areaLearningData.timestamp = pose.timestamp;
                m_areaLearningData.confidence = pose.confidence;
                m_areaLearningData.accuracy = pose.accuracy;
                m_hasNewAreaLearningData = true;
            } 
            // Relocalized against ADF
            else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                     pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
            {
                m_relocalizationData.framePair = pose.framePair;
                m_relocalizationData.status_code = pose.status_code;
                m_relocalizationData.orientation = pose.orientation;
                m_relocalizationData.translation = pose.translation;
                m_relocalizationData.timestamp = pose.timestamp;
                m_relocalizationData.confidence = pose.confidence;
                m_relocalizationData.accuracy = pose.accuracy;
                m_hasNewRelocalizationData = true;
            }

			m_isDirty = true;
        }
    }
}