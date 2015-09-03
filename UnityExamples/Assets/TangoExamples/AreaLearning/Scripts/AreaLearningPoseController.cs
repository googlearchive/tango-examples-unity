//-----------------------------------------------------------------------
// <copyright file="AreaLearningPoseController.cs" company="Google">
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
/// This is forked from the <c>TangoDeltaPoseController</c>.
/// 
/// This takes into account the different pose update frames you can get when
/// AreaLearning is enabled.
/// </summary>
public class AreaLearningPoseController : MonoBehaviour, ITangoPose
{
    /// <summary>
    /// The different Pose frames of reference stored.
    /// </summary>
    public enum PoseFrame
    {
        DeviceToStart = 0,
        DeviceToADF = 1,
        StartToADF = 2,
        Count = 3
    }

    /// <summary>
    /// The change in time since the last pose update.
    /// </summary>
    [HideInInspector]
    public float[] m_poseDeltaTime;
    
    /// <summary>
    /// Total number of poses ever received by this controller.
    /// </summary>
    [HideInInspector]
    public int[] m_poseCount;
    
    /// <summary>
    /// The most recent pose status received.
    /// </summary>
    [HideInInspector]
    public TangoEnums.TangoPoseStatusType[] m_poseStatus;
    
    /// <summary>
    /// The most recent Tango rotation.
    /// 
    /// This is different from the pose's rotation because it takes into
    /// account teleporting and the clutch.
    /// 
    /// Only public for GUI logging.
    /// </summary>
    [HideInInspector]
    public Vector3[] m_tangoPosition;
    
    /// <summary>
    /// The most recent Tango position.
    /// 
    /// This is different from the pose's position because it takes into
    /// account teleporting and the clutch.
    /// 
    /// Only public for GUI logging.
    /// </summary>
    [HideInInspector]
    public Quaternion[] m_tangoRotation;

    /// <summary>
    /// The TangoApplication being listened to.
    /// </summary>
    private TangoApplication m_tangoApplication;
    
    /// <summary>
    /// The most recent pose timestamp received.
    /// </summary>
    private float[] m_poseTimestamp;
    
    /// <summary>
    /// If the most recent pose was localized.
    /// </summary>
    private bool m_poseLocalized = false;
    
    /// <summary>
    /// The previous Tango's position.
    /// 
    /// This is different from the pose's position because it takes into
    /// account teleporting and the clutch.
    /// </summary>
    private Vector3[] m_prevTangoPosition;
    
    /// <summary>
    /// The previous Tango's rotation.
    /// 
    /// This is different from the pose's rotation because it takes into
    /// account teleporting and the clutch.
    /// </summary>
    private Quaternion[] m_prevTangoRotation;
    
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
        
        // Constant matrix converting Unity world frame frame to device frame.
        m_dTuc = new Matrix4x4();
        m_dTuc.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_dTuc.SetColumn(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_dTuc.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
        m_dTuc.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        
        m_poseDeltaTime = new float[] { -1.0f, -1.0f, -1.0f };
        m_poseTimestamp = new float[] { -1.0f, -1.0f, -1.0f };
        m_poseCount = new int[] { -1, -1, -1 };
        m_poseStatus = new TangoEnums.TangoPoseStatusType[]
        {
            TangoEnums.TangoPoseStatusType.NA, TangoEnums.TangoPoseStatusType.NA, TangoEnums.TangoPoseStatusType.NA
        };
        m_prevTangoRotation = new Quaternion[] { Quaternion.identity, Quaternion.identity, Quaternion.identity };
        m_tangoRotation = new Quaternion[] { Quaternion.identity, Quaternion.identity, Quaternion.identity };
        m_prevTangoPosition = new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero };
        m_tangoPosition = new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero };
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        
        if (m_tangoApplication != null)
        {
            if (AndroidHelper.IsTangoCorePresent())
            {
                // Request Tango permissions
                m_tangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
                m_tangoApplication.RequestNecessaryPermissionsAndConnect();
                m_tangoApplication.Register(this);
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
        #if UNITY_ANDROID && !UNITY_EDITOR
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
        #else
        Vector3 tempPosition = transform.position;
        Quaternion tempRotation = transform.rotation;
        PoseProvider.GetMouseEmulation(ref tempPosition, ref tempRotation);
        transform.rotation = tempRotation;
        transform.position = tempPosition;
        #endif
    }
    
    /// <summary>
    /// Unity callback when application is paused.
    /// </summary>
    /// <param name="pauseStatus">The pauseStatus as reported by Unity.</param>
    public void OnApplicationPause(bool pauseStatus)
    {
        for (int it = 0; it != (int)PoseFrame.Count; ++it)
        {
            m_poseDeltaTime[it] = -1.0f;
            m_poseTimestamp[it] = -1.0f;
            m_poseCount[it] = -1;
            m_poseStatus[it] = TangoEnums.TangoPoseStatusType.NA;
        }
    }

    /// <summary>
    /// OnTangoPoseAvailable is called from Tango when a new Pose is available.
    /// </summary>
    /// <param name="pose">The new Tango pose.</param>
    public void OnTangoPoseAvailable(TangoPoseData pose)
    {
        // Get out of here if the pose is null
        if (pose == null)
        {
            Debug.Log("TangoPoseDate is null.");
            return;
        }

        int currentFrame;

        // Only interested in specific pose updates
        if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
            pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
        {
            // The callback pose is for device with respect to start of service pose.
            currentFrame = (int)PoseFrame.DeviceToStart;
        }
        else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                 pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
        {
            // The callback pose is for device with respect to area description file pose.
            currentFrame = (int)PoseFrame.DeviceToADF;
        }
        else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
                 pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
        {
            // The callback pose is for start of service with respect to area description file pose.
            currentFrame = (int)PoseFrame.StartToADF;
            m_poseLocalized = true;
        }
        else
        {
            // Not a pose frame we are interested in.
            return;
        }
        Debug.Log("OnPose(" + currentFrame.ToString() + ")");

        // Remember the previous position, so you can do delta motion
        m_prevTangoPosition[currentFrame] = m_tangoPosition[currentFrame];
        m_prevTangoRotation[currentFrame] = m_tangoRotation[currentFrame];
        
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
            m_tangoPosition[currentFrame] = uwTuc.GetColumn(3);
            m_tangoRotation[currentFrame] = Quaternion.LookRotation(uwTuc.GetColumn(2), uwTuc.GetColumn(1));

            // Other pose data -- Pose count gets reset if pose status just became valid.
            if (pose.status_code != m_poseStatus[currentFrame])
            {
                m_poseCount[currentFrame] = 0;
            }
            m_poseCount[currentFrame]++;

            // Other pose data -- Pose delta time.
            m_poseDeltaTime[currentFrame] = (float)pose.timestamp - m_poseTimestamp[currentFrame];
            m_poseTimestamp[currentFrame] = (float)pose.timestamp;
        }
        else
        {
            // if the current pose is not valid assume everything is invalid.
            m_tangoPosition[currentFrame] = Vector3.zero;
            m_tangoRotation[currentFrame] = Quaternion.identity;
            m_poseLocalized = false;
        }
        m_poseStatus[currentFrame] = pose.status_code;

        if (currentFrame != (int)PoseFrame.StartToADF)
        {
            // Only update position and rotation if the update was for the device's pose.
            // 
            // Calculate final position and rotation deltas and apply them. 
            transform.position = m_tangoPosition[currentFrame];
            transform.rotation = m_tangoRotation[currentFrame];
        }
    }
    
    /// <summary>
    /// Determines whether motion tracking is localized.
    /// </summary>
    /// <returns><c>true</c> if motion tracking is localized; otherwise, <c>false</c>.</returns>
    public bool IsLocalized()
    {
        return m_poseLocalized;
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

            // Load the most recent ADF.
            PoseProvider.RefreshADFList();
            string uuid = PoseProvider.GetLatestADFUUID().GetStringDataUUID();
            m_tangoApplication.InitProviders(uuid);

            m_tangoApplication.ConnectToService();
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking and Area Learning Permissions Needed", true);
        }
    }
}
