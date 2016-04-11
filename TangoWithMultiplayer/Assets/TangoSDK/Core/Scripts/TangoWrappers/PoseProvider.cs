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
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// C API wrapper for the Tango pose interface.
    /// </summary>
    public class PoseProvider
    {
        private const float MOUSE_LOOK_SENSITIVITY = 100.0f;
        private const float TRANSLATION_SPEED = 2.0f;

#if UNITY_EDITOR
        /// <summary>
        /// The amount of seconds to keep emulated poses around.
        /// </summary>
        private const float EMULATION_POSE_KEEP_TIME_SECS = 60 * 60;
#endif

        private static readonly string CLASS_NAME = "PoseProvider";

#if UNITY_EDITOR
        /// <summary>
        /// History of emulated poses.  Used for Tango emulation on PC.
        /// </summary>
        private static List<EmulatedPose> m_emulatedPoseHistory = new List<EmulatedPose>();
#endif

        /// <summary>
        /// Tango pose C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="pose">Pose data.</param> 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onPoseAvailable(IntPtr callbackContext, [In, Out] TangoPoseData pose);

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
            bool baseFrameIsWorld = 
                framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            bool targetFrameIsWorld =
                framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;

            // Area Descriptions are explicitly not supported yet.
            if (framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION
                || framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION)
            {
                Debug.Log(CLASS_NAME + ".GetPoseAtTime() emulation does not support Area Descriptions.");
                poseData.framePair = framePair;
                poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID;
                poseData.timestamp = 0;
                poseData.translation[0] = Vector3.zero.x;
                poseData.translation[1] = Vector3.zero.y;
                poseData.translation[2] = Vector3.zero.z;
                poseData.orientation[0] = Quaternion.identity.x;
                poseData.orientation[1] = Quaternion.identity.y;
                poseData.orientation[2] = Quaternion.identity.z;
                poseData.orientation[3] = Quaternion.identity.w;
                return;
            }
            
            if (baseFrameIsWorld && !targetFrameIsWorld)
            {
                if (timeStamp == 0)
                {
                    float poseTimestamp;
                    Vector3 posePosition;
                    Quaternion poseRotation;
                    if (GetTangoEmulationCurrent(out poseTimestamp, out posePosition, out poseRotation))
                    {
                        poseData.framePair = framePair;
                        poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;
                        poseData.timestamp = poseTimestamp;
                        poseData.translation[0] = posePosition.x;
                        poseData.translation[1] = posePosition.y;
                        poseData.translation[2] = posePosition.z;
                        poseData.orientation[0] = poseRotation.x;
                        poseData.orientation[1] = poseRotation.y;
                        poseData.orientation[2] = poseRotation.z;
                        poseData.orientation[3] = poseRotation.w;
                        return;
                    }
                }
                else
                {
                    Vector3 posePosition;
                    Quaternion poseRotation;
                    if (GetTangoEmulationAtTime((float)timeStamp, out posePosition, out poseRotation))
                    {
                        poseData.framePair = framePair;
                        poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;
                        poseData.timestamp = timeStamp;
                        poseData.translation[0] = posePosition.x;
                        poseData.translation[1] = posePosition.y;
                        poseData.translation[2] = posePosition.z;
                        poseData.orientation[0] = poseRotation.x;
                        poseData.orientation[1] = poseRotation.y;
                        poseData.orientation[2] = poseRotation.z;
                        poseData.orientation[3] = poseRotation.w;
                        return;
                    }
                }
            }
            else if (baseFrameIsWorld == targetFrameIsWorld)
            {
                poseData.framePair = framePair;
                poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;
                poseData.timestamp = timeStamp;
                poseData.translation[0] = Vector3.zero.x;
                poseData.translation[1] = Vector3.zero.y;
                poseData.translation[2] = Vector3.zero.z;
                poseData.orientation[0] = Quaternion.identity.x;
                poseData.orientation[1] = Quaternion.identity.y;
                poseData.orientation[2] = Quaternion.identity.z;
                poseData.orientation[3] = Quaternion.identity.w;
                return;
            }

            Debug.Log(string.Format(
                CLASS_NAME + ".GetPoseAtTime() Could not get pose at time : ts={0}, framePair={1},{2}",
                timeStamp, framePair.baseFrame, framePair.targetFrame));
            poseData.framePair = framePair;
            poseData.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID;
            poseData.timestamp = 0;
            poseData.translation[0] = Vector3.zero.x;
            poseData.translation[1] = Vector3.zero.y;
            poseData.translation[2] = Vector3.zero.z;
            poseData.orientation[0] = Quaternion.identity.x;
            poseData.orientation[1] = Quaternion.identity.y;
            poseData.orientation[2] = Quaternion.identity.z;
            poseData.orientation[3] = Quaternion.identity.w;
#else
            int returnValue = PoseProviderAPI.TangoService_getPoseAtTime(timeStamp, framePair, poseData);
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
            PoseProviderAPI.TangoService_resetMotionTracking();
        }

        /// <summary>
        /// Set the C callback for the Tango pose interface.
        /// </summary>
        /// <param name="framePairs">Passed in to the C API.</param>
        /// <param name="callback">Callback method.</param>
        internal static void SetCallback(TangoCoordinateFramePair[] framePairs, TangoService_onPoseAvailable callback)
        {
            int returnValue = PoseProviderAPI.TangoService_connectOnPoseAvailable(framePairs.Length, framePairs, callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was not set!");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".SetCallback() OnPose callback was set!");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Update the Tango emulation state for pose data.
        /// 
        /// Make this this is only called once per frame.
        /// </summary>
        internal static void UpdateTangoEmulation()
        {
            EmulatedPose pose;
            if (m_emulatedPoseHistory.Count > 0)
            {
                pose = new EmulatedPose(m_emulatedPoseHistory[m_emulatedPoseHistory.Count - 1]);
            }
            else
            {
                pose = new EmulatedPose();
            }

            // Update the emulated rotation (do this first to make sure the position is rotated)
            //
            // Note: We need to use Input.GetAxis here because Unity3D does not provide a way to get the underlying
            // mouse delta.
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                pose.m_anglesFromForward.y -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
                pose.m_anglesFromForward.x += Input.GetAxis("Mouse Y") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            }
            else
            {
                pose.m_anglesFromForward.z -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            }
            
            // Update the emulated position
            Quaternion poseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(pose.m_anglesFromForward);
            Vector3 directionRight = poseRotation * new Vector3(1, 0, 0);
            Vector3 directionForward = poseRotation * new Vector3(0, 0, -1);
            Vector3 directionUp = poseRotation * new Vector3(0, 1, 0);
            
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
            float now = Time.realtimeSinceStartup;
            pose.m_timestamp = now;
            while (m_emulatedPoseHistory.Count > 0 
                   && m_emulatedPoseHistory[0].m_timestamp < now - EMULATION_POSE_KEEP_TIME_SECS)
            {
                m_emulatedPoseHistory.RemoveAt(0);
            }
            
            m_emulatedPoseHistory.Add(pose);
        }

        /// <summary>
        /// INTERNAL USE: Get a timestamp in the past appropriate for depth emulation.
        /// </summary>
        /// <returns>The timestamp for depth emulation.</returns>
        internal static float GetTimestampForDepthEmulation()
        {
            if (m_emulatedPoseHistory.Count > 1)
            {
                int mostRecentPose = m_emulatedPoseHistory.Count - 1;
                return Mathf.Lerp(m_emulatedPoseHistory[mostRecentPose].m_timestamp,
                                  m_emulatedPoseHistory[mostRecentPose - 1].m_timestamp,
                                  0.5f);
            }
            else if(m_emulatedPoseHistory.Count == 1)
            {
                return m_emulatedPoseHistory[0].m_timestamp;
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// INTERNAL USE: Get the most recent values for Tango emulation.
        /// </summary>
        /// <param name="poseTimestamp">The new Tango emulation timestamp.</param> 
        /// <param name="posePosition">The new Tango emulation position.</param>
        /// <param name="poseRotation">The new Tango emulation rotation.</param>
        private static bool GetTangoEmulationCurrent(
            out float poseTimestamp, out Vector3 posePosition, out Quaternion poseRotation)
        {
            if (m_emulatedPoseHistory.Count > 0)
            {
                EmulatedPose pose = m_emulatedPoseHistory[m_emulatedPoseHistory.Count - 1];
                poseTimestamp = pose.m_timestamp;
                posePosition = pose.m_position;
                poseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(pose.m_anglesFromForward);
                return true;
            }
            else
            {
                poseTimestamp = 0;
                posePosition = Vector3.zero;
                poseRotation = Quaternion.identity;
                return false;
            }

        }

        /// <summary>
        /// INTERNAL USE: Get a historical value for Tango emulation.
        /// </summary>
        /// <returns><c>true</c>, if a historical values was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="timestamp">Time of the historical value.</param>
        /// <param name="posePosition">The emulated pose position if retrieved, <c>Vector3.Zero</c> otherwise.</param>
        /// <param name="poseRotation">
        /// The emulated pose rotation if retrieved, <c>Quaternion.Identity</c> otherwise.
        /// </param>
        private static bool GetTangoEmulationAtTime(
            float timestamp, out Vector3 posePosition, out Quaternion poseRotation)
        {
            EmulatedPose timestampedPose = new EmulatedPose();
            timestampedPose.m_timestamp = timestamp;
            int index = m_emulatedPoseHistory.BinarySearch(timestampedPose, new CompareEmulatedPoseByTimestamp());

            if (index >= 0)
            {
                // Found an exact timestamp match
                EmulatedPose pose = m_emulatedPoseHistory[index];
                posePosition = pose.m_position;
                poseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(pose.m_anglesFromForward);
                return true;
            }
            else if (~index == m_emulatedPoseHistory.Count || ~index == 0)
            {
                // Out of bounds, no good pose
                posePosition = Vector3.zero;
                poseRotation = Quaternion.identity;
                return false;
            }
            {
                // Timestamp is inbetween two pose histories
                EmulatedPose earlierPose = m_emulatedPoseHistory[~index - 1];
                EmulatedPose laterPose = m_emulatedPoseHistory[~index];
                float t = (timestamp - earlierPose.m_timestamp) / (laterPose.m_timestamp - earlierPose.m_timestamp);

                posePosition = Vector3.Lerp(earlierPose.m_position, laterPose.m_position, t);

                Quaternion earlierRot = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(earlierPose.m_anglesFromForward);
                Quaternion laterRot = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(laterPose.m_anglesFromForward);
                poseRotation = Quaternion.Slerp(earlierRot, laterRot, t);
                return true;
            }
        }

        /// <summary>
        /// All the details needed for an individual emulated pose.  These are kept around to emulate GetPoseAtTime.
        /// </summary>
        private class EmulatedPose
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Tango.PoseProvider+EmulatedPose"/> class.
            /// </summary>
            public EmulatedPose()
            {
                m_timestamp = 0;
                m_position = Vector3.zero;
                m_anglesFromForward = Vector3.zero;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Tango.PoseProvider+EmulatedPose"/> class.
            /// </summary>
            /// <param name="other">Other.</param>
            public EmulatedPose(EmulatedPose other)
            {
                m_timestamp = other.m_timestamp;
                m_position = other.m_position;
                m_anglesFromForward = other.m_anglesFromForward;
            }

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
            public Vector3 m_anglesFromForward;
        }

        /// <summary>
        /// Comparer for an EmulatedPose.
        /// </summary>
        private class CompareEmulatedPoseByTimestamp : Comparer<EmulatedPose>
        {
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
        private struct PoseProviderAPI
        { 
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnPoseAvailable(int count,
                                                                         TangoCoordinateFramePair[] framePairs,
                                                                         TangoService_onPoseAvailable onPoseAvailable);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_getPoseAtTime(double timestamp,
                                                                TangoCoordinateFramePair framePair,
                                                                [In, Out] TangoPoseData pose);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_setPoseListenerFrames(int count,
                                                                        ref TangoCoordinateFramePair frames);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern void TangoService_resetMotionTracking();
#else
            public static int TangoService_connectOnPoseAvailable(int count,
                                                                  TangoCoordinateFramePair[] framePairs,
                                                                  TangoService_onPoseAvailable onPoseAvailable)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_getPoseAtTime(double timestamp,
                                                         TangoCoordinateFramePair framePair,
                                                         [In, Out] TangoPoseData pose)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_setPoseListenerFrames(int count,
                                                                 ref TangoCoordinateFramePair frames)
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
