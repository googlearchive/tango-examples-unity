//-----------------------------------------------------------------------
// <copyright file="PoseProvider.cs" company="Google">
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
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// C API wrapper for the Tango pose interface.
    /// </summary>
    public class PoseProvider
    {
#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Flag set to true whenever emulated values have been updated.
        /// </summary>
        internal static bool m_emulationIsDirty;
#endif

        private const float MOUSE_LOOK_SENSITIVITY = 100.0f;
        private const float TRANSLATION_SPEED = 2.0f;

#if UNITY_EDITOR
        /// <summary>
        /// The amount of seconds to keep emulated poses around.
        /// </summary>
        private const float EMULATION_POSE_KEEP_TIME_SECS = 60 * 60;

        /// <summary>
        /// Amount of time before first relocalized emulated area description
        /// should be sent (applicable only when an existing area description is loaded).
        /// </summary>
        private const double EMULATED_RELOCALIZATION_TIME = 0.5;
#endif

        private static readonly string CLASS_NAME = "PoseProvider";

#if UNITY_EDITOR
        /// <summary>
        /// History of emulated poses.  Used for Tango emulation on PC.
        /// </summary>
        private static List<EmulatedPose> m_emulatedPoseHistory = new List<EmulatedPose>();

        /// <summary>
        /// Timestamp of the first pose of pose emulation.
        /// </summary>
        private static float m_beginningOfPoseEmulation = -1f;
#endif

        /// <summary>
        /// Tango pose C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="pose">Pose data.</param> 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void APIOnPoseAvailable(IntPtr callbackContext, [In, Out] TangoPoseData pose);

        /// <summary>
        /// Get a pose at a given timestamp from the base to the target frame.
        /// 
        /// All poses returned are marked as TANGO_POSE_VALID (in the status_code field on TangoPoseData ) even if
        /// they were marked as TANGO_POSE_INITIALIZING in the callback poses.
        /// 
        /// If no pose can be returned, the status_code of the returned pose will be TANGO_POSE_INVALID.
        /// </summary>
        /// <param name="poseData">The pose to return.</param>
        /// <param name="timeStamp">
        /// Time specified in seconds.
        /// 
        /// If not set to 0.0, GetPoseAtTime retrieves the interpolated pose closest to this timestamp. If set to 0.0,
        /// the most recent pose estimate for the target-base pair is returned. The time of the returned pose is
        /// contained in the pose output structure and may differ from the queried timestamp.
        /// </param>
        /// <param name="framePair">
        /// A pair of coordinate frames specifying the transformation to be queried for.
        /// 
        /// For example, typical device motion is given by a target frame of TANGO_COORDINATE_FRAME_DEVICE and a base
        /// frame of TANGO_COORDINATE_FRAME_START_OF_SERVICE .
        /// </param>
        public static void GetPoseAtTime([In, Out] TangoPoseData poseData, 
                                         double timeStamp, 
                                         TangoCoordinateFramePair framePair)
        {
#if UNITY_EDITOR
            GetEmulatedPoseAtTime(poseData, timeStamp, framePair);
#else
            int returnValue = API.TangoService_getPoseAtTime(timeStamp, framePair, poseData);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".GetPoseAtTime() Could not get pose at time : " + timeStamp);
            }
#endif
        }
        
        /// <summary>
        /// Resets the motion tracking system.
        /// 
        /// This reinitializes the <c>TANGO_COORDINATE_FRAME_START_OF_SERVICE</c> coordinate frame to where the
        /// device is when you call this function; afterwards, if you ask for the pose with relation to start of
        /// service, it uses this as the new origin.  You can call this function at any time.
        ///
        /// If you are using Area Learning, the <c>TANGO_COORDINATE_FRAME_AREA_DESCRIPTION</c> coordinate frame
        /// is not affected by calling this function; however, the device needs to localize again before you can use
        /// the area description.
        /// </summary>
        public static void ResetMotionTracking()
        {
            API.TangoService_resetMotionTracking();
        }

        /// <summary>
        /// Set the C callback for the Tango pose interface.
        /// </summary>
        /// <param name="framePairs">Passed in to the C API.</param>
        /// <param name="callback">Callback method.</param>
        internal static void SetCallback(TangoCoordinateFramePair[] framePairs, APIOnPoseAvailable callback)
        {
            int returnValue = API.TangoService_connectOnPoseAvailable(framePairs.Length, framePairs, callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was not set!");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".SetCallback() OnPose callback was set!");
            }
        }

        /// <summary>
        /// Clear the C callback for the Tango pose interface.
        /// </summary>
        internal static void ClearCallback()
        {
            int returnValue = API.TangoService_connectOnPoseAvailable(0, null, null);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Callback was not cleared!");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() OnPose callback was cleared!");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Update the Tango emulation state for pose data.
        /// 
        /// Make sure this is only called once per frame.
        /// </summary>
        internal static void UpdateTangoEmulation()
        {
            EmulatedPose pose;
            float now = Time.realtimeSinceStartup;

            if (m_emulatedPoseHistory.Count > 0)
            {
                pose = new EmulatedPose(m_emulatedPoseHistory[m_emulatedPoseHistory.Count - 1]);
            }
            else
            {
                pose = new EmulatedPose();
                m_beginningOfPoseEmulation = now;
            }

            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftCommand))
            {
                // Update the emulated rotation (do this first to make sure the position is rotated)
                //
                // Note: We need to use Input.GetAxis here because Unity3D does not provide a way to get the underlying
                // mouse delta.
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    pose.m_angles.y += Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
                    pose.m_angles.x -= Input.GetAxis("Mouse Y") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
                }
                else
                {
                    pose.m_angles.z -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
                }
            }
            
            // Update the emulated position
            Quaternion poseRotation = Quaternion.Euler(pose.m_angles);
            Vector3 directionRight = poseRotation * Vector3.right;
            Vector3 directionForward = poseRotation * Vector3.forward;
            Vector3 directionUp = poseRotation * Vector3.up;
            
            if (Input.GetKey(KeyCode.W))
            {
                // Forward
                pose.m_position += directionForward * TRANSLATION_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.S))
            {
                // Backward
                pose.m_position -= directionForward * TRANSLATION_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.A))
            {
                // Left
                pose.m_position -= directionRight * TRANSLATION_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.D))
            {
                // Right
                pose.m_position += directionRight * TRANSLATION_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.E))
            {
                // Up
                pose.m_position += directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                // Down
                pose.m_position -= directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }

            // Record the current state
            pose.m_timestamp = now;
            while (m_emulatedPoseHistory.Count > 0 
                   && m_emulatedPoseHistory[0].m_timestamp < now - EMULATION_POSE_KEEP_TIME_SECS)
            {
                m_emulatedPoseHistory.RemoveAt(0);
            }
            
            m_emulatedPoseHistory.Add(pose);

            m_emulationIsDirty = true;
        }

        /// <summary>
        /// INTERNAL USE: Reset the Tango emulation state for pose data.
        /// 
        /// Used on Shutdown. Among other things, prevents GetPoseAtTime()
        /// from returning valid frames when Tango isn't active.
        /// </summary>
        internal static void ResetTangoEmulation()
        {
            m_emulatedPoseHistory.Clear();
        }

        /// <summary>
        /// Emulation for PoseProvider.GetPoseAtTime().
        /// </summary>
        /// <param name="poseData">Pose data.</param>
        /// <param name="timeStamp">Requested time stamp.</param>
        /// <param name="framePair">Requested frame pair.</param>
        internal static void GetEmulatedPoseAtTime(TangoPoseData poseData, double timeStamp,
                                                   TangoCoordinateFramePair framePair)
        {
            poseData.framePair = framePair;
            
            double adjustedTimeStamp1 = timeStamp;
            double adjustedTimeStamp2 = timeStamp;
            
            Matrix4x4 baseToDevice;
            Matrix4x4 targetToDevice;

            TangoEnums.TangoPoseStatusType status1;
            TangoEnums.TangoPoseStatusType status2;

            _GetFrameToDevicePose(framePair.baseFrame, ref adjustedTimeStamp1, out baseToDevice, out status1);
            _GetFrameToDevicePose(framePair.targetFrame, ref adjustedTimeStamp2, out targetToDevice, out status2);

            // Composit base->device and target->device into base->target.
            Matrix4x4 baseToTarget = baseToDevice * targetToDevice.inverse;
            Quaternion rotation = Quaternion.LookRotation(baseToTarget.GetColumn(2), baseToTarget.GetColumn(1));
            poseData.translation[0] = baseToTarget.m03;
            poseData.translation[1] = baseToTarget.m13;
            poseData.translation[2] = baseToTarget.m23;
            poseData.orientation[0] = rotation.x;
            poseData.orientation[1] = rotation.y;
            poseData.orientation[2] = rotation.z;
            poseData.orientation[3] = rotation.w;
            
            // Use the 'less successful' of the two statuses.
            if (status1 == TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN 
                || status2 == TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN)
            {
                poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN;
            }
            else if (status1 == TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID 
                     || status2 == TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID)
            {
                poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID;
            }
            else if (status1 == TangoEnums.TangoPoseStatusType.TANGO_POSE_INITIALIZING 
                     || status2 == TangoEnums.TangoPoseStatusType.TANGO_POSE_INITIALIZING)
            {
                poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_INITIALIZING;
            }
            else if (status1 == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID 
                     && status2 == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;
            }
            else
            {
                poseData.status_code = TangoEnums.TangoPoseStatusType.NA;
                Debug.Log(string.Format(
                    CLASS_NAME + ".GetPoseAtTime() Could not get pose at time : ts={0}, framePair={1},{2}",
                    timeStamp, framePair.baseFrame, framePair.targetFrame));
            }
            
            // Let most recent timestamp involved in the transformation be the timestamp
            // (relevant when using GetPoseAtTime(0)), 
            // Except when getting relocalization pose (area description <-> start of service),
            // in which case the timestamp should be the (theoretical) relocalization time.
            if ((framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION
                 && framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
                || 
                (framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE
             && framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION))
            {
                if (poseData.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
                {
                    // First assume that relocalization happens at start (e.g. Area Learning for new areas).
                    poseData.timestamp = m_beginningOfPoseEmulation;
                    
                    if (!EmulatedAreaDescriptionHelper.m_areaDescriptionFramesAvailableAtStart)
                    {
                        // Then add EMULATED_RELOCALIZATION_TIME second if an area description was loaded.
                        poseData.timestamp += EMULATED_RELOCALIZATION_TIME;
                    }

                    // The initially requested timestamp is only valid if it:
                    //  A.) Is 0
                    //  B.) Falls within the range of delivered relocalization frames 
                    //      (and we only deliver one in emulation, so it must match that one exactly)
                    bool validRelocalizationTimestamp = (timeStamp == 0) || (timeStamp == poseData.timestamp);
                    if (!validRelocalizationTimestamp)
                    {
                        poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID;
                    }
                }
            }
            else
            {
                poseData.timestamp = System.Math.Max(adjustedTimeStamp1, adjustedTimeStamp2);
            }
        }

        /// <summary>
        /// INTERNAL USE: Get a timestamp in the past appropriate for depth emulation.
        /// </summary>
        /// <returns><c>true</c>, if timestamp for depth emulation is valid, <c>false</c> otherwise.</returns>
        /// <param name="timeStamp">The timestamp for depth emulation.</param>
        internal static bool GetTimestampForDepthEmulation(out float timeStamp)
        {
            if (m_emulatedPoseHistory.Count > 1)
            {
                int mostRecentPose = m_emulatedPoseHistory.Count - 1;
                timeStamp = Mathf.Lerp(m_emulatedPoseHistory[mostRecentPose].m_timestamp,
                                       m_emulatedPoseHistory[mostRecentPose - 1].m_timestamp,
                                       0.5f);
                return true;
            }
            else if (m_emulatedPoseHistory.Count == 1)
            {
                timeStamp = m_emulatedPoseHistory[0].m_timestamp;
                return true;
            }
            else
            {
                timeStamp = -1f;
                return false;
            }
        }

        /// <summary>
        /// INTERNAL USE: Get a timestamp in the past appropriate for color emulation.
        /// </summary>
        /// <returns><c>true</c>, if timestamp for color emulation is valid, <c>false</c> otherwise.</returns>
        /// <param name="timeStamp">The timestamp for color emulation.</param>
        internal static bool GetTimestampForColorEmulation(out float timeStamp)
        {
            if (m_emulatedPoseHistory.Count > 1)
            {
                int mostRecentPose = m_emulatedPoseHistory.Count - 1;
                timeStamp = Mathf.Lerp(m_emulatedPoseHistory[mostRecentPose].m_timestamp,
                                       m_emulatedPoseHistory[mostRecentPose - 1].m_timestamp,
                                       0.7f);
                return true;
            }
            else if (m_emulatedPoseHistory.Count == 1)
            {
                timeStamp = m_emulatedPoseHistory[0].m_timestamp;
                return true;
            }
            else
            {
                timeStamp = -1f;
                return false;
            }
        }

        /// <summary>
        /// INTERNAL USE: Gets a value indicating whether an artifical delay from service start
        /// representing the time it takes to sync to an existing area description has passed.
        /// </summary>
        /// <returns><c>true</c>, if area description delay is over, <c>false</c> otherwise.</returns>
        private static bool _GetAreaDescriptionSyncDelayIsOver()
        {
            if (m_emulatedPoseHistory.Count < 1)
            {
                return false;
            }
            else
            {
                float durationOfPlay = m_emulatedPoseHistory[m_emulatedPoseHistory.Count - 1].m_timestamp 
                    - m_beginningOfPoseEmulation;
                return durationOfPlay > EMULATED_RELOCALIZATION_TIME;
            }
        }

        /// <summary>
        /// GetEmulatedPoseAtTime functions by getting pose information for both base and target
        /// frames relative to the Device frame; this handles compositing information for one of
        /// those frames.
        /// </summary>
        /// <param name="frame">Frame to get frame -> device frame for.</param>
        /// <param name="timeStamp">Time stamp; if 0 may be modified with most up-to-date timestamp.</param>
        /// <param name="frameToDeviceTransformation">Specified frame to device frame transformation.</param>
        /// <param name="status">Status of the given frame to device frame pose.
        /// See comments on GetEmulatedPoseAtTime().</param>
        private static void _GetFrameToDevicePose(TangoEnums.TangoCoordinateFrameType frame,
                                                  ref double timeStamp,
                                                  out Matrix4x4 frameToDeviceTransformation,
                                                  out TangoEnums.TangoPoseStatusType status)
        {
            frameToDeviceTransformation = Matrix4x4.identity;
            status = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;

            bool frameIsWorld = (frame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
                || (frame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION);
            
            // Check that emulation supports this frame:
            if (!_EmulationSupportsReferenceFrame(frame))
            {
                status = TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID;
            }
            
            // Get mouse/keyboard-based emulation if appropriate.
            if (frameIsWorld)
            {
                if (!_GetEmulatedMovementTransformAtTime(ref timeStamp, out frameToDeviceTransformation))
                {
                    status = TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN;
                }
                
                if (frame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION)
                {
                    bool areaDescriptionFramesAreValid = 
                        EmulatedAreaDescriptionHelper.m_usingEmulatedDescriptionFrames
                        && (EmulatedAreaDescriptionHelper.m_areaDescriptionFramesAvailableAtStart
                            || _GetAreaDescriptionSyncDelayIsOver());

                    if (areaDescriptionFramesAreValid)
                    {
                        frameToDeviceTransformation.m03 += EmulatedAreaDescriptionHelper.m_emulationAreaOffset.x;
                        frameToDeviceTransformation.m13 += EmulatedAreaDescriptionHelper.m_emulationAreaOffset.y;
                        frameToDeviceTransformation.m23 += EmulatedAreaDescriptionHelper.m_emulationAreaOffset.z;
                    }
                    else
                    {
                        status = TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID;
                    }
                }
            }

            _GetFrameToDeviceAxisSwaps(frame, ref frameToDeviceTransformation);
        }

        /// <summary>
        /// Gets the refrence frames coordinate system swaps one would get on an actual device going from the specified 
        /// base frame to a target Device reference frame.
        /// 
        /// For movement-related reference frames (e.g. Start of Service, Area Descriptions), applies the appropriate 
        /// coordinate system conversion to the given Unity-space transformation.
        /// 
        /// For static extrinsic reference frames (e.g. IMU, camera positions), overwrites the given matrix with a matrix
        /// representing the appropriate axis swaps for that coordinate system conversion.
        /// </summary>
        /// <param name="baseFrame">Specified base frame.</param>
        /// <param name="transformation">Transformation to apply coordinate system swaps to.</param>
        private static void _GetFrameToDeviceAxisSwaps(TangoEnums.TangoCoordinateFrameType baseFrame,
                                                       ref Matrix4x4 transformation)
        {
            switch (baseFrame)
            {
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE:
                transformation = Matrix4x4.identity;
                break;
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION:
                // Poses are emulated in Unity space, so we have to temporarily move across a handedness
                // swap into tango space to reconstruct the transformation as being SoS -> Device.

                // Constant matrix converting between start of service and Unity world axis conventions.
                Matrix4x4 startOfService_T_unityWorld = new Matrix4x4();
                startOfService_T_unityWorld.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                startOfService_T_unityWorld.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
                startOfService_T_unityWorld.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                startOfService_T_unityWorld.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                
                // Constant matrix converting between Unity world frame and device axis conventions.
                Matrix4x4 unityCamera_T_device = new Matrix4x4();
                unityCamera_T_device.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                unityCamera_T_device.SetColumn(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                unityCamera_T_device.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
                unityCamera_T_device.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                transformation = startOfService_T_unityWorld * transformation * unityCamera_T_device;
                break;
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU:
                transformation.SetRow(0, new Vector4(0, -1, 0, 0));
                transformation.SetRow(1, new Vector4(1, 0, 0, 0));
                transformation.SetRow(2, new Vector4(0, 0, 1, 0));
                transformation.SetRow(3, new Vector4(0, 0, 0, 1));
                break;
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_FISHEYE:
                transformation.SetRow(0, new Vector4(1, 0, 0, 0));
                transformation.SetRow(1, new Vector4(0, -1, 0, 0));
                transformation.SetRow(2, new Vector4(0, 0, -1, 0));
                transformation.SetRow(3, new Vector4(0, 0, 0, 1));
                break;
            default:
                Debug.Log(baseFrame.ToString() + " reference frame not handled by pose emulation.");
                break;
            }
        }

        /// <summary>
        /// Returns whether emulation supports the given reference frame.
        /// </summary>
        /// <returns><c>true</c> if emulation supports the given reference frame, <c>false</c> otherwise.</returns>
        /// <param name="referenceFrame">The reference frame in question.</param>
        private static bool _EmulationSupportsReferenceFrame(TangoEnums.TangoCoordinateFrameType referenceFrame)
        {
            switch (referenceFrame)
            {
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR:
            case TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_FISHEYE:
                return true;
            default:
                Debug.Log(CLASS_NAME + ".GetPoseAtTime() emulation does not support the "
                          + referenceFrame + " reference frame");
                return false;
            }
        }

        /// <summary>
        /// Get transformation from Start of Service to to the Device frame (minus axis swaps) at a specified time.
        /// </summary>
        /// <returns><c>true</c>, if transformation is valid at the given timestamp, <c>false</c> otherwise.</returns>
        /// <param name="timestamp">Time stamp being queried. 
        /// When querying with a timestamp of 0, this will be adjusted to the current pose's timestamp.</param>
        /// <param name="transformation">The transformation Start of Service to the Device frame at the specified time.
        /// If transformation is not valid, this is undefined.</param>
        private static bool _GetEmulatedMovementTransformAtTime(ref double timestamp, out Matrix4x4 transformation)
        {
            bool poseIsValid;
            Vector3 posePosition;
            Quaternion poseRotation;
            
            // Get pose data.
            if (timestamp == 0)
            {
                // Get the most recent value for Tango emulation.
                if (m_emulatedPoseHistory.Count > 0)
                {
                    EmulatedPose pose = m_emulatedPoseHistory[m_emulatedPoseHistory.Count - 1];
                    timestamp = pose.m_timestamp;
                    posePosition = pose.m_position;
                    poseRotation = Quaternion.Euler(pose.m_angles);
                    poseIsValid = true;
                }
                else
                {
                    posePosition = Vector3.zero;
                    poseRotation = Quaternion.identity;
                    poseIsValid = false;
                }
            }
            else
            {
                // Get a historical value for Tango emulation.
                EmulatedPose timestampedPose = new EmulatedPose();
                timestampedPose.m_timestamp = (float)timestamp;
                int index = m_emulatedPoseHistory.BinarySearch(timestampedPose, new CompareEmulatedPoseByTimestamp());
                
                if (index >= 0)
                {
                    // Found an exact timestamp match
                    EmulatedPose pose = m_emulatedPoseHistory[index];
                    posePosition = pose.m_position;
                    poseRotation = Quaternion.Euler(pose.m_angles);
                    poseIsValid = true;
                }
                else if (~index == m_emulatedPoseHistory.Count || ~index == 0)
                {
                    // Out of bounds, no good pose
                    posePosition = Vector3.zero;
                    poseRotation = Quaternion.identity;
                    poseIsValid = false;
                }
                else
                {
                    // Timestamp is inbetween two pose histories
                    EmulatedPose earlierPose = m_emulatedPoseHistory[~index - 1];
                    EmulatedPose laterPose = m_emulatedPoseHistory[~index];
                    float t = Mathf.InverseLerp(earlierPose.m_timestamp, laterPose.m_timestamp, (float)timestamp);
                    
                    posePosition = Vector3.Lerp(earlierPose.m_position, laterPose.m_position, t);
                    
                    Quaternion earlierRot = Quaternion.Euler(earlierPose.m_angles);
                    Quaternion laterRot = Quaternion.Euler(laterPose.m_angles);
                    poseRotation = Quaternion.Slerp(earlierRot, laterRot, t);
                    poseIsValid = true;
                }
            }
            
            // Compose matrix.
            if (poseIsValid)
            {
                transformation = Matrix4x4.TRS(posePosition, poseRotation, Vector3.one);
            }
            else
            {
                transformation = Matrix4x4.identity;
            }
            
            return poseIsValid;
        }
        
        /// <summary>
        /// All the details needed for an individual emulated pose.  These are kept around to emulate GetPoseAtTime.
        /// </summary>
        private class EmulatedPose
        {
            /// <summary>
            /// Emulated timestamp, in seconds.
            /// </summary>
            public float m_timestamp;
            
            /// <summary>
            /// Emulated position.
            /// </summary>
            public Vector3 m_position;
            
            /// <summary>
            /// Emulated rotation stored as euler angles of a rotation from forward.
            /// </summary>
            public Vector3 m_angles;
            
            /// <summary>
            /// Initializes a new instance of the <see cref="Tango.PoseProvider+EmulatedPose"/> class.
            /// </summary>
            public EmulatedPose()
            {
                m_timestamp = 0;
                m_position = Vector3.zero;
                m_angles = Vector3.zero;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Tango.PoseProvider+EmulatedPose"/> class.
            /// </summary>
            /// <param name="other">Emulated pose to copy.</param>
            public EmulatedPose(EmulatedPose other)
            {
                m_timestamp = other.m_timestamp;
                m_position = other.m_position;
                m_angles = other.m_angles;
            }
        }

        /// <summary>
        /// Comparer for an EmulatedPose.
        /// </summary>
        private class CompareEmulatedPoseByTimestamp : Comparer<EmulatedPose>
        {
            /// <summary>
            /// Compare the specified emulated poses by timestamp.
            /// </summary>
            /// <param name="x">The first pose.</param>
            /// <param name="y">The second pose.</param>
            /// <returns>Value appropriate for a comparer sorting by timestamp.</returns>
            public override int Compare(EmulatedPose x, EmulatedPose y)
            {
                return Math.Sign(x.m_timestamp - y.m_timestamp);
            }
        }
#endif

        #region API_Functions
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct API
        { 
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_connectOnPoseAvailable(
                int count, TangoCoordinateFramePair[] framePairs, APIOnPoseAvailable callback);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_getPoseAtTime(
                double timestamp, TangoCoordinateFramePair framePair, [In, Out] TangoPoseData pose);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern void TangoService_resetMotionTracking();
#else
            public static int TangoService_connectOnPoseAvailable(
                int count, TangoCoordinateFramePair[] framePairs, APIOnPoseAvailable callback)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_getPoseAtTime(
                double timestamp, TangoCoordinateFramePair framePair, [In, Out] TangoPoseData pose)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static void TangoService_resetMotionTracking()
            {
            }
#endif
        }
        #endregion
    }
}
