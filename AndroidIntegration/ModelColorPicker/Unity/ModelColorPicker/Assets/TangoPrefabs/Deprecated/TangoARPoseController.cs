//-----------------------------------------------------------------------
// <copyright file="TangoARPoseController.cs" company="Google">
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
using System;
using System.Collections;
using Tango;
using UnityEngine;

/// <summary>
/// DEPRECATED -- This controller is deprecated.  Please use TangoPoseController
/// instead.
///
/// A movement controller that automatically sets the position and rotation of
/// the GameObject this is attached to. Movement matches what comes from Tango
/// and is synchronized with the color camera. Used by the Tango AR Camera
/// prefab to provide an augmented reality experience.
/// </summary>
[RequireComponent(typeof(TangoARScreen))]
public class TangoARPoseController : MonoBehaviour, ITangoLifecycle
{
    /// <summary>
    /// If set, use the Area Description base frame for the pose.
    /// </summary>
    public bool m_useAreaDescriptionPose = false;

    /// <summary>
    /// When enabled (which is the default), pose positions are based on the
    /// timestamp of the most recent video camera update.
    ///
    /// Can be disabled for whenever this behavior is not desired: For instance, in
    /// an application where only some segments of the experience display an
    /// active camera feed.
    /// </summary>
    [Tooltip("This should always be enabled when using AR. Can be disabled "
                 + "to get smoother motion in non-AR parts of an app.")]
    public bool m_syncToARScreen = true;

    /// <summary>
    /// The number of poses applied by this controller. Resets to 0 if motion
    /// tracking goes invalid or is reset.
    /// </summary>
    [HideInInspector]
    public int m_poseCount;

    /// <summary>
    /// The status of the most recent pose used by this controller.
    /// </summary>
    [HideInInspector]
    public TangoEnums.TangoPoseStatusType m_poseStatus;

    /// <summary>
    /// The timestamp of the most recent pose used by this controller.
    /// </summary>
    [HideInInspector]
    public double m_poseTimestamp;

    /// <summary>
    /// The position from the most recent pose used by this controller.
    /// </summary>
    [HideInInspector]
    public Vector3 m_tangoPosition;

    /// <summary>
    /// The rotation from the most recent pose used by this controller.
    /// </summary>
    [HideInInspector]
    public Quaternion m_tangoRotation;

    // We use multiple matrix transformations to convert a pose from the Tango
    // coordinate system to the Unity coordinate system.
    // The full equation is:
    //     Matrix4x4 uwTuc = uwTss * ssTd * dTuc;
    //
    // uwTuc: The Unity camera with respect to the Unity world coordinate frame;
    //        this is the desired matrix.
    // uwTss: A constant matrix converting the start of service coordinate frame
    //        to the Unity world coordinate frame.
    // ssTd:  The device frame with respect to start of service frame; this
    //        matrix comes from the Tango pose data.
    // dTuc:  A constant matrix converting the Unity world coordinate frame to
    //        the device coordinate frame.
    //
    // For more information, see the Tango coordinate system documentation:
    //     https://developers.google.com/project-tango/overview/coordinate-systems

    /// <summary>
    /// The transformation matrix that converts from the left-handed Unity
    /// Camera coordinate frame to the right-handed Device coordinate frame.
    /// </summary>
    [HideInInspector]
    public Matrix4x4 m_dTuc;

    /// <summary>
    /// The TangoARScreen that is being updated.
    /// </summary>
    private TangoARScreen m_tangoARScreen;

    /// <summary>
    /// Reference to TangoApplication object.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// @cond
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    public void Awake()
    {
        m_poseTimestamp = -1.0f;
        m_poseCount = -1;
        m_poseStatus = TangoEnums.TangoPoseStatusType.NA;
        m_tangoRotation = Quaternion.identity;
        m_tangoPosition = Vector3.zero;
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled.
    /// </summary>
    public void Start()
    {
        m_tangoARScreen = GetComponent<TangoARScreen>();

        m_tangoApplication = FindObjectOfType<TangoApplication>();
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);

            // If already connected to a service, then do initialization now.
            if (m_tangoApplication.IsServiceConnected)
            {
                OnTangoServiceConnected();
            }
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }
    }

    /// <summary>
    /// Update is called every frame.
    /// </summary>
    public void Update()
    {
        if (m_syncToARScreen)
        {
            if (m_tangoARScreen.m_screenUpdateTime != m_poseTimestamp)
            {
                _UpdateTransformation(m_tangoARScreen.m_screenUpdateTime);
            }
        }
        else
        {
            _UpdateTransformation(0);
        }
    }

    /// <summary>
    /// Unity callback when application is paused.
    /// </summary>
    /// <param name="pauseStatus">The pauseStatus as reported by Unity.</param>
    public void OnApplicationPause(bool pauseStatus)
    {
        m_poseTimestamp = -1.0f;
        m_poseCount = -1;
        m_poseStatus = TangoEnums.TangoPoseStatusType.NA;
    }

    /// <summary>
    /// Unity callback when the component gets destroyed.
    /// </summary>
    public void OnDestroy()
    {
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Unregister(this);
        }
    }

    /// <summary>
    /// Called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
    }

    /// <summary>
    /// Called when successfully connected to the Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
        _SetCameraExtrinsics();
    }

    /// <summary>
    /// Called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// @endcond
    /// <summary>
    /// Updates the transformation to the pose for that timestamp.
    /// </summary>
    /// <param name="timestamp">Time in seconds to update the transformation to.</param>
    private void _UpdateTransformation(double timestamp)
    {
        TangoPoseData pose = new TangoPoseData();
        TangoCoordinateFramePair pair;

        // Choose the proper pair according to the properties of this controller
        if (m_useAreaDescriptionPose)
        {
            if (m_tangoApplication.m_enableCloudADF)
            {
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_GLOBAL_WGS84;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            }
            else
            {
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            }
        }
        else
        {
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        }

        PoseProvider.GetPoseAtTime(pose, timestamp, pair);

        // Update properties from pose
        if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
        {
            DMatrix4x4 globalTLocal;
            bool success = m_tangoApplication.GetGlobalTLocal(out globalTLocal);
            if (!success)
            {
                Debug.LogError("Unable to obtain GlobalTLocal from TangoApplication.");
                return;
            }

            DMatrix4x4 uwTDevice = DMatrix4x4.FromMatrix4x4(TangoSupport.UNITY_WORLD_T_START_SERVICE) *
                                   globalTLocal.Inverse * DMatrix4x4.TR(pose.translation, pose.orientation);

            // Calculate matrix for the camera in the Unity world, taking into account offsets.
            Matrix4x4 uwTuc = uwTDevice.ToMatrix4x4() * m_dTuc * TangoSupport.m_colorCameraPoseRotation;

            // Extract final position, rotation.
            m_tangoPosition = uwTuc.GetColumn(3);
            m_tangoRotation = Quaternion.LookRotation(uwTuc.GetColumn(2), uwTuc.GetColumn(1));

            // Other pose data -- Pose count gets reset if pose status just became valid.
            if (pose.status_code != m_poseStatus)
            {
                m_poseCount = 0;
            }

            m_poseCount++;

            // Other pose data -- Pose time.
            m_poseTimestamp = timestamp;
        }

        m_poseStatus = pose.status_code;

        // Apply final position and rotation.
        transform.position = m_tangoPosition;
        transform.rotation = m_tangoRotation;
    }

    /// <summary>
    /// Gets device and camera extrinsics necessary for the transformations done
    /// by this controller. Extrinsics queries use GetPoseAtTime() with a
    /// specific frame pair, and can only be done after the Tango service is
    /// connected.
    ///
    /// The transform for the device with respect to the color camera frame is
    /// not directly queryable from API, so we use the IMU frame to get the
    /// transformation between the two.
    /// </summary>
    private void _SetCameraExtrinsics()
    {
        double timestamp = 0.0;
        TangoCoordinateFramePair pair;
        TangoPoseData poseData = new TangoPoseData();

        // Get the transformation of device frame with respect to IMU frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        Matrix4x4 imuTd = poseData.ToMatrix4x4();

        // Get the transformation of IMU frame with respect to color camera frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        Matrix4x4 imuTc = poseData.ToMatrix4x4();

        // Get the transform of the Unity Camera frame with respect to the Color Camera frame.
        Matrix4x4 cTuc = new Matrix4x4();
        cTuc.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        cTuc.SetColumn(1, new Vector4(0.0f, -1.0f, 0.0f, 0.0f));
        cTuc.SetColumn(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        cTuc.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        m_dTuc = Matrix4x4.Inverse(imuTd) * imuTc * cTuc;
    }
}
