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
using System.Collections;
using UnityEngine;
using Tango;
using System;

/// <summary>
/// This is a basic movement controller based on
/// pose estimation returned from the Tango Service.
/// </summary>
public class PoseController : PoseListener
{
    public enum TrackingTypes
    {
        NONE,
        MOTION,
        ADF,
        RELOCALIZED
    }
    
    private bool m_alreadyInitialized = false;
    private TangoApplication m_tangoApplication;
    
    // Tango pose data for debug logging and transform update.
    [HideInInspector]
    public string m_tangoServiceVersionName = string.Empty;
    [HideInInspector]
    public float m_frameDeltaTime;
    [HideInInspector]
    public int m_frameCount;
    [HideInInspector]
    public TangoEnums.TangoPoseStatusType m_status;
    private float m_prevFrameTimestamp;
    
    // Tango pose data.
    private Quaternion m_tangoRotation;
    private Vector3 m_tangoPosition;
    private bool m_isDirty = false;
    
    // Matrix for Tango coordinate frame to Unity coordinate frame conversion.
    // Start of service frame with respect to Unity world frame.
    private Matrix4x4 m_uwTss;
    // Unity camera frame with respect to device frame.
    private Matrix4x4 m_cTuc;
    // IMU frame with respect to device frame.
    private Matrix4x4 m_imuTd;
    // IMU frame with respect to color camera frame (as well as depth camera frame)
    private Matrix4x4 m_imuTc;
    
    
    // Flag for initilizing Tango.
    private bool m_shouldInitTango = false;
    
    /// <summary>
    /// Initialize the controller.
    /// </summary>
    private void Awake()
    {
		// Constant matrix converting start of service frame to Unity world frame.
        m_uwTss = new Matrix4x4();
        m_uwTss.SetColumn (0, new Vector4 (1.0f, 0.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn (1, new Vector4 (0.0f, 0.0f, 1.0f, 0.0f));
        m_uwTss.SetColumn (2, new Vector4 (0.0f, 1.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn (3, new Vector4 (0.0f, 0.0f, 0.0f, 1.0f));
        
		// Constant matrix converting Unity world frame frame to device frame.
        m_cTuc.SetColumn (0, new Vector4 (1.0f, 0.0f, 0.0f, 0.0f));
        m_cTuc.SetColumn (1, new Vector4 (0.0f, -1.0f, 0.0f, 0.0f));
        m_cTuc.SetColumn (2, new Vector4 (0.0f, 0.0f, 1.0f, 0.0f));
        m_cTuc.SetColumn (3, new Vector4 (0.0f, 0.0f, 0.0f, 1.0f));
        
        m_isDirty = false;
        m_frameDeltaTime = -1.0f;
        m_prevFrameTimestamp = -1.0f;
        m_frameCount = -1;
        m_status = TangoEnums.TangoPoseStatusType.NA;
        m_tangoRotation = Quaternion.identity;
        m_tangoPosition = Vector3.zero;
    }
    
    /// <summary>
    /// Start this instance.
    /// </summary>
    private void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        
        if(m_tangoApplication != null)
        {
            if(AndroidHelper.IsTangoCorePresent())
            {
                // Request Tango permissions
                m_tangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
                m_tangoApplication.RequestNecessaryPermissionsAndConnect();
                m_tangoServiceVersionName = TangoApplication.GetTangoServiceVersion();
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
    /// Informs the user that they should install Tango Core via Android toast.
    /// </summary>
    private IEnumerator _InformUserNoTangoCore()
    {
        AndroidHelper.ShowAndroidToastMessage("Please install Tango Core", false);
        yield return new WaitForSeconds(2.0f);
        Application.Quit();
    }
    
    /// <summary>
    /// Apply any needed changes to the pose.
    /// </summary>
    private void Update()
    {
        if (m_shouldInitTango)
        {
            m_tangoApplication.InitApplication();
            m_tangoApplication.InitProviders(string.Empty);
            m_tangoApplication.ConnectToService();
            m_shouldInitTango = false;

            double timestamp = 0.0;
            TangoCoordinateFramePair pair;
            TangoPoseData poseData = new TangoPoseData();
            
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
            Vector3 position = new Vector3((float)poseData.translation[0], (float)poseData.translation[1], (float)poseData.translation[2]);
            Quaternion quat = new Quaternion((float)poseData.orientation[0], (float)poseData.orientation[1], (float)poseData.orientation[2], (float)poseData.orientation[3]);
            m_imuTd = Matrix4x4.TRS(position, quat, new Vector3 (1.0f, 1.0f, 1.0f));
            
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
            PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
            position = new Vector3((float)poseData.translation[0], (float)poseData.translation[1], (float)poseData.translation[2]);
            quat = new Quaternion((float)poseData.orientation[0], (float)poseData.orientation[1], (float)poseData.orientation[2], (float)poseData.orientation[3]);
            m_imuTc = Matrix4x4.TRS(position, quat, new Vector3 (1.0f, 1.0f, 1.0f));
        }
        if (m_isDirty)
        {
            Matrix4x4 ssTd = Matrix4x4.TRS(m_tangoPosition, m_tangoRotation, Vector3.one);
            Matrix4x4 uwTuc = m_uwTss * ssTd * Matrix4x4.Inverse(m_imuTd) * m_imuTc * m_cTuc;
            
            // Extract new local position
            transform.position = uwTuc.GetColumn(3);
            
            // Extract new local rotation
            transform.rotation = Quaternion.LookRotation(uwTuc.GetColumn(2), uwTuc.GetColumn(1));
        }
        
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(m_tangoApplication != null)
            {
                m_tangoApplication.Shutdown();
            }
            
            // This is a temporary fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application immediately,
            // results in a hard crash.
            AndroidHelper.AndroidQuit();
        }
    }
    
    /// <summary>
    /// Unity callback when application is paused.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        m_isDirty = false;
        m_frameDeltaTime = -1.0f;
        m_prevFrameTimestamp = -1.0f;
        m_frameCount = -1;
        m_status = TangoEnums.TangoPoseStatusType.NA;
        m_tangoRotation = Quaternion.identity;
        m_tangoPosition = Vector3.zero;
    }
    
    /// <summary>
    /// Handle the callback sent by the Tango Service
    /// when a new pose is sampled.
    /// DO NOT USE THE UNITY API FROM INSIDE THIS FUNCTION!
    /// </summary>
    /// <param name="callbackContext">Callback context.</param>
    /// <param name="pose">Pose.</param>
    protected override void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose)
    {
        // Get out of here if the pose is null
        if (pose == null)
        {
            Debug.Log("TangoPoseDate is null.");
            return;
        }
        
        // The callback pose is for device with respect to start of service pose.
        if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
            pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
        {
            if(pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                // Cache the position and rotation to be set in the update function.
                // This needs to be done because this callback does not
                // happen in the main game thread.
                m_tangoPosition = new Vector3((float)pose.translation [0],
                                              (float)pose.translation [1],
                                              (float)pose.translation [2]);
                
                m_tangoRotation = new Quaternion((float)pose.orientation [0],
                                                 (float)pose.orientation [1],
                                                 (float)pose.orientation [2],
                                                 (float)pose.orientation [3]);
            }
            else // if the current pose is not valid we set the pose to identity
            {
                m_tangoPosition = Vector3.zero;
                m_tangoRotation = Quaternion.identity;
            }
        }
        
        // Reset the current status frame count if the status code changed.
        if (pose.status_code != m_status)
        {
            m_frameCount = 0;
        }
        
        // Update the stats for the pose for the debug text
        m_status = pose.status_code;
        m_frameCount++;
        
        // Compute delta frame timestamp.
        m_frameDeltaTime = (float)pose.timestamp - m_prevFrameTimestamp;
        m_prevFrameTimestamp = (float)pose.timestamp;
        
        // Switch m_isDirty to true, so that the new pose get rendered in update.
        m_isDirty = (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID);
    }
    
    private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
    {
        if(permissionsGranted)
        {
            m_shouldInitTango = true;
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
        }
    }
}
