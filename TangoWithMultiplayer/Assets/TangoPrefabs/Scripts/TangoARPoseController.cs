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
/// This is a movement controller based on the poses returned from the Tango service, using the correct timing needed
/// for augmented reality.
/// </summary>
[RequireComponent(typeof(TangoARScreen))]
public class TangoARPoseController : MonoBehaviour, ITangoLifecycle
{
    /// <summary>
    /// If set, this contoller will use the Device with respect Area Description frame pose.
    /// </summary>
    public bool m_useAreaDescriptionPose = false;

    /// <summary>
    /// Total number of poses ever applied by this controller.
    /// </summary>
    [HideInInspector]
    public int m_poseCount;

    /// <summary>
    /// The most recent pose status applied.
    /// </summary>
    [HideInInspector]
    public TangoEnums.TangoPoseStatusType m_poseStatus;

    /// <summary>
    /// The most recent pose timestamp applied.
    /// </summary>
    [HideInInspector]
    public double m_poseTimestamp;

    /// <summary>
    /// The most recent Tango rotation.
    /// </summary>
    [HideInInspector]
    public Vector3 m_tangoPosition;

    /// <summary>
    /// The most recent Tango position.
    /// </summary>
    [HideInInspector]
    public Quaternion m_tangoRotation;

    /// <summary>
    /// Matrix that transforms from the Unity Camera to Device.
    /// </summary>
    [HideInInspector]
    public Matrix4x4 m_dTuc;

    // We use couple of matrix transformation to convert the pose from Tango coordinate
    // frame to Unity coordinate frame.
    // The full equation is:
    //     Matrix4x4 uwTuc = uwTss * ssTd * dTuc;
    //
    // uwTuc: Unity camera with respect to Unity world, this is the desired matrix.
    // uwTss: Constant matrix converting start of service frame to Unity world frame.
    // ssTd: Device frame with repect to start of service frame, this matrix denotes the 
    //       pose transform we get from pose callback.
    // dTuc: Constant matrix converting Unity world frame frame to device frame.
    //
    // Please see the coordinate system section online for more information:
    //     https://developers.google.com/project-tango/overview/coordinate-systems

    /// <summary>
    /// Matrix that transforms from Start of Service to the Unity World.
    /// </summary>
    [HideInInspector]
    public Matrix4x4 m_uwTss;

    /// <summary>
    /// The TangoARScreen that is being updated.
    /// </summary>
    private TangoARScreen m_tangoARScreen;

    /// @cond
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    public void Awake()
    {
        // Constant matrix converting start of service frame to Unity world frame.
        m_uwTss = new Matrix4x4();
        m_uwTss.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        m_uwTss.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

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

        TangoApplication tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication != null)
        {
            tangoApplication.Register(this);

            // If already connected to a service, then do initialization now.
            if (tangoApplication.IsServiceConnected)
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
        if (m_tangoARScreen.m_screenUpdateTime != m_poseTimestamp)
        {
            _UpdateTransformation(m_tangoARScreen.m_screenUpdateTime);
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
        TangoApplication tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication != null)
        {
            tangoApplication.Unregister(this);
        }
    }

    /// <summary>
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
    }

    /// <summary>
    /// This is called when succesfully connected to the Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
        _SetCameraExtrinsics();
    }

    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// @endcond
    /// <summary>
    /// Update the transformation to the pose for that timestamp.
    /// </summary>
    /// <param name="timestamp">Time in seconds to update the transformation to.</param>
    private void _UpdateTransformation(double timestamp)
    {
        TangoPoseData pose = new TangoPoseData();
        TangoCoordinateFramePair pair;
        if (!m_useAreaDescriptionPose)
        {
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        }
        else
        {
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        }

        PoseProvider.GetPoseAtTime(pose, timestamp, pair);

        // The callback pose is for device with respect to start of service pose.
        if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
        {
            // Construct matrix for the start of service with respect to device from the pose.
            Matrix4x4 ssTd = pose.ToMatrix4x4();
            
            // Calculate matrix for the camera in the Unity world, taking into account offsets.
            Matrix4x4 uwTuc = m_uwTss * ssTd * m_dTuc;
            
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
    /// The function is for querying the camera extrinsic, for example: the transformation between
    /// IMU and device frame. These extrinsics is used to transform the pose from the color camera frame
    /// to the device frame. Because the extrinsic is being queried using the GetPoseAtTime()
    /// with a desired frame pair, it can only be queried after the ConnectToService() is called.
    ///
    /// The device with respect to IMU frame is not directly queryable from API, so we use the IMU
    /// frame as a temporary value to get the device frame with respect to IMU frame.
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
