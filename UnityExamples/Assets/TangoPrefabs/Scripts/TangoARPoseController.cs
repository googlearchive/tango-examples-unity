//-----------------------------------------------------------------------
// <copyright file="TangoARPoseController.cs" company="Google">
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
using System.Collections;
using UnityEngine;
using Tango;

/// <summary>
/// This is a movement controller based on the poses returned from the Tango service, using the correct timing needed
/// for augmented reality.
/// </summary>
[RequireComponent(typeof(TangoARScreen))]
public class TangoARPoseController : MonoBehaviour
{
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
    /// The TangoApplication being listened to.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// The TangoARScreen that is being updated.
    /// </summary>
    private TangoARScreen m_tangoARScreen;

    /// <summary>
    /// The most recent pose timestamp applied.
    /// </summary>
    private double m_poseTimestamp;

    /// <summary>
    /// The most recent Tango rotation.
    /// </summary>
    private Vector3 m_tangoPosition;

    /// <summary>
    /// The most recent Tango position.
    /// </summary>
    private Quaternion m_tangoRotation;

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
    private Matrix4x4 m_uwTss;

    /// <summary>
    /// Matrix that transforms from the Unity Camera to Device.
    /// </summary>
    private Matrix4x4 m_dTuc;

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
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoARScreen = GetComponent<TangoARScreen>();

        if (m_tangoApplication != null)
        {
            if (AndroidHelper.IsTangoCorePresent())
            {
                // Request Tango permissions
                m_tangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
                m_tangoApplication.RequestNecessaryPermissionsAndConnect();
            }
            else
            {
                // If no Tango Core is present let's tell the user to install it!
                StartCoroutine(_InformUserNoTangoCore());
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_tangoApplication != null)
            {
                m_tangoApplication.Shutdown();
            }
            
            // This is a temporary fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application immediately,
            // results in a hard crash.
            AndroidHelper.AndroidQuit();
        }

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
    /// Update the transformation to the pose for that timestamp.
    /// </summary>
    /// <param name="timestamp">Time in seconds to update the transformation to.</param>
    private void _UpdateTransformation(double timestamp)
    {
        TangoPoseData pose = new TangoPoseData();
        TangoCoordinateFramePair pair;
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        PoseProvider.GetPoseAtTime(pose, timestamp, pair);

        // The callback pose is for device with respect to start of service pose.
        if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
        {
            // Construct matrix for the start of service with respect to device from the pose.
            Vector3 posePosition = new Vector3((float)pose.translation[0],
                                               (float)pose.translation[1],
                                               (float)pose.translation[2]);
            Quaternion poseRotation = new Quaternion((float)pose.orientation[0],
                                                     (float)pose.orientation[1],
                                                     (float)pose.orientation[2],
                                                     (float)pose.orientation[3]);
            Matrix4x4 ssTd = Matrix4x4.TRS(posePosition, poseRotation, Vector3.one);
            
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
        Vector3 position = new Vector3((float)poseData.translation[0],
                                       (float)poseData.translation[1],
                                       (float)poseData.translation[2]);
        Quaternion quat = new Quaternion((float)poseData.orientation[0],
                                         (float)poseData.orientation[1],
                                         (float)poseData.orientation[2],
                                         (float)poseData.orientation[3]);
        Matrix4x4 imuTd = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));
        
        // Get the transformation of IMU frame with respect to color camera frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        position = new Vector3((float)poseData.translation[0],
                               (float)poseData.translation[1],
                               (float)poseData.translation[2]);
        quat = new Quaternion((float)poseData.orientation[0],
                              (float)poseData.orientation[1],
                              (float)poseData.orientation[2],
                              (float)poseData.orientation[3]);
        Matrix4x4 imuTc = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));

        // Get the transform of the Unity Camera frame with respect to the Color Camera frame.
        Matrix4x4 cTuc = new Matrix4x4();
        cTuc.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        cTuc.SetColumn(1, new Vector4(0.0f, -1.0f, 0.0f, 0.0f));
        cTuc.SetColumn(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        cTuc.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        m_dTuc = Matrix4x4.Inverse(imuTd) * imuTc * cTuc;
    }

    /// <summary>
    /// Internal callback when no Tango core is present.
    /// </summary>
    /// <returns>Coroutine IEnumerator.</returns>
    private IEnumerator _InformUserNoTangoCore()
    {
        AndroidHelper.ShowAndroidToastMessage("Please install Tango Core", false);
        yield return new WaitForSeconds(2.0f);
        Application.Quit();
    }

    /// <summary>
    /// Internal callback when a permissions event happens.
    /// </summary>
    /// <param name="permissionsGranted">If set to <c>true</c> permissions granted.</param>
    private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            m_tangoApplication.InitApplication();
            m_tangoApplication.InitProviders(string.Empty);
            m_tangoApplication.ConnectToService();

            // Get camera extrinsics to form final matrix transforms
            _SetCameraExtrinsics();
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
        }
    }
}
