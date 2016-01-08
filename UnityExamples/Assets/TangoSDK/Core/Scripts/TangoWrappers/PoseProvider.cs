//-----------------------------------------------------------------------
// <copyright file="PoseProvider.cs" company="Google">
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
        private static readonly string CLASS_NAME = "PoseProvider";

#if UNITY_EDITOR
        /// <summary>
        /// The emulated pose position.  Used for Tango emulation on PC.
        /// </summary>
        private static Vector3 m_emulatedPosePosition;
        
        /// <summary>
        /// The emulated pose euler angles from forward.  Used for Tango emulation on PC.
        /// 
        /// This is not the pure rotation for Tango, when it is Identity, you are facing forward, not down.
        /// </summary>
        private static Vector3 m_emulatedPoseAnglesFromForward;
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
            int returnValue = PoseProviderAPI.TangoService_getPoseAtTime(timeStamp, framePair, poseData);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".GetPoseAtTime() Could not get pose at time : " + timeStamp);
            }
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
            // Update the emulated rotation (do this first to make sure the position is rotated)
            //
            // Note: We need to use Input.GetAxis here because Unity3D does not provide a way to get the underlying
            // mouse delta.
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                m_emulatedPoseAnglesFromForward.y -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
                m_emulatedPoseAnglesFromForward.x += Input.GetAxis("Mouse Y") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            }
            else
            {
                m_emulatedPoseAnglesFromForward.z -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            }
            
            // Update the emulated position
            Quaternion emulatedPoseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(m_emulatedPoseAnglesFromForward);
            Vector3 directionRight = emulatedPoseRotation * new Vector3(1, 0, 0);
            Vector3 directionForward = emulatedPoseRotation * new Vector3(0, 0, -1);
            Vector3 directionUp = emulatedPoseRotation * new Vector3(0, 1, 0);
            
            if (Input.GetKey(KeyCode.W))
            {
                // Forward
                m_emulatedPosePosition += directionForward * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                // Backward
                m_emulatedPosePosition -= directionForward * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A))
            {
                // Left
                m_emulatedPosePosition -= directionRight * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                // Right
                m_emulatedPosePosition += directionRight * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                // Up
                m_emulatedPosePosition += directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                // Down
                m_emulatedPosePosition -= directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }
        }

        /// <summary>
        /// INTERNAL USE: Get the most recent values for Tango emulation.
        /// </summary>
        /// <param name="posePosition">The new Tango emulation position.</param>
        /// <param name="poseRotation">The new Tango emulation rotation.</param>
        internal static void GetTangoEmulation(out Vector3 posePosition, out Quaternion poseRotation)
        {
            posePosition = m_emulatedPosePosition;
            poseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(m_emulatedPoseAnglesFromForward);
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
