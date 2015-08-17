// <copyright file="CustomTangoController.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// This is a basic movement controller based on
/// pose estimation returned from the Tango Service.
/// </summary>
public class CustomTangoController : MonoBehaviour, ITangoPose
{
    /// <summary>
    /// Tracking state of Tango.
    /// </summary>
    public enum TrackingTypes
    {
        NONE,
        MOTION,
        ADF,
        RELOCALIZED
    }

    [HideInInspector]
    public float m_metersToWorldUnitsScaler = 1.0f;
    public bool enableInterpolation = true;
    public bool isShowingDebugButton = true;
    
    private TangoApplication m_tangoApplication;
    private PopupManager popManager;

    private Vector3 m_zeroPosition;
    private Quaternion m_zeroRotation;
    private Vector3 m_startPosition;
    private Quaternion m_startRotation;

    private TangoPoseData prevPose = new TangoPoseData();
    private TangoPoseData currPose = new TangoPoseData();
    private float unityTimestampOffset = 0;

    /// <summary>
    /// Handle the callback sent by the Tango Service
    /// when a new pose is sampled.
    /// </summary>
    /// <param name="pose">Pose.</param>
    public void OnTangoPoseAvailable(Tango.TangoPoseData pose)
    {
        // The callback pose is for device with respect to start of service pose.
        if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
            pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
        {
            if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                popManager.tangoInitialized = true;
                popManager.TriggerAPICallbackFPS();
                
                UpdateInterpolationData(pose);
                if (enableInterpolation)
                {
                    if (unityTimestampOffset > float.Epsilon)
                    {
                        UpdateUsingInterpolatedPose(Time.realtimeSinceStartup + unityTimestampOffset);
                    }
                }
                else
                {
                    UpdateUsingInterpolatedPose(currPose.timestamp);
                }
            }
        }
    }
    
    /// <summary>
    /// Initialize the controller.
    /// </summary>
    private void Awake()
    {
        m_startPosition = transform.position;
        m_startRotation = transform.rotation;

        popManager = GetComponent<PopupManager>();
    }
    
    /// <summary>
    /// Start this instance.
    /// </summary>
    private void Start()
    {
        Application.targetFrameRate = 60;

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
    /// Informs the user that they should install Tango Core via Android toast.
    /// </summary>
    /// <returns>Coroutine enumerator.</returns>
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
        popManager.TriggerUpdateFPS();
        #if UNITY_ANDROID && !UNITY_EDITOR
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
        
        #else
        popManager.tangoInitialized = true;
        Vector3 tempPosition = transform.position;
        Quaternion tempRotation = transform.rotation;
        PoseProvider.GetMouseEmulation(ref tempPosition, ref tempRotation);
        transform.rotation = tempRotation;
        transform.position = transform.position + ((tempPosition - transform.position) * m_metersToWorldUnitsScaler);
        #endif

        popManager.debugText = "Interpolation: " + enableInterpolation;
    }

    /// <summary>
    /// Updates the using interpolated pose.
    /// </summary>
    /// <param name="t">Current time.</param>
    private void UpdateUsingInterpolatedPose(double t)
    {
        float dt = (float)((t - prevPose.timestamp) / (currPose.timestamp - prevPose.timestamp));

        // restrict this, so it isn't doesn't swing out of control
        if (dt > 4)
        {
            dt = 4;
        }

        Vector3 currPos = new Vector3();
        Vector3 prevPos = new Vector3();
        Quaternion currRot = new Quaternion();
        Quaternion prevRot = new Quaternion();
        
        ComputeTransformUsingPose(out currPos, out currRot, currPose);
        ComputeTransformUsingPose(out prevPos, out prevRot, prevPose);
        
        // hack for rotation, should be a slerp
        transform.rotation = m_startRotation * new Quaternion((dt * (currRot[0] - prevRot[0])) + prevRot[0],
                                                              (dt * (currRot[1] - prevRot[1])) + prevRot[1],
                                                              (dt * (currRot[2] - prevRot[2])) + prevRot[2],
                                                              (dt * (currRot[3] - prevRot[3])) + prevRot[3]);
        transform.position = (m_startRotation * (new Vector3((dt * (currPos[0] - prevPos[0])) + prevPos[0],
                                                             (dt * (currPos[1] - prevPos[1])) + prevPos[1],
                                                             (dt * (currPos[2] - prevPos[2])) + prevPos[2]) - m_zeroPosition) * m_metersToWorldUnitsScaler) + m_startPosition;
    }

    /// <summary>
    /// Extract the position, rotation for a Tango pose.
    /// </summary>
    /// <param name="position">Output position.</param>
    /// <param name="rot">Output rotation.</param>
    /// <param name="pose">Tango Pose.</param>
    private void ComputeTransformUsingPose(out Vector3 position, out Quaternion rot, TangoPoseData pose)
    {
        position = new Vector3((float)pose.translation[0],
                               (float)pose.translation[2],
                               (float)pose.translation[1]);
        
        rot = new Quaternion((float)pose.orientation[0],
                             (float)pose.orientation[2], // these rotation values are swapped on purpose
                             (float)pose.orientation[1],
                             (float)pose.orientation[3]);

        // This rotation needs to be put into Unity coordinate space.
        Quaternion axisFix = Quaternion.Euler(-rot.eulerAngles.x, -rot.eulerAngles.z, rot.eulerAngles.y);
        Quaternion rotationFix = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        rot = rotationFix * axisFix;
    }

    /// <summary>
    /// Return a clone of a Tango pose.
    /// </summary>
    /// <returns>The cloned tango pose.</returns>
    /// <param name="other">Tango pose.</param>
    private TangoPoseData DeepCopyTangoPose(TangoPoseData other)
    {
        TangoPoseData poseCopy = new TangoPoseData();
        poseCopy.version = other.version;
        poseCopy.timestamp = other.timestamp;
        poseCopy.orientation = other.orientation;
        poseCopy.translation = other.translation;
        poseCopy.status_code = other.status_code;
        poseCopy.framePair.baseFrame = other.framePair.baseFrame;
        poseCopy.framePair.targetFrame = other.framePair.targetFrame;
        poseCopy.confidence = other.confidence;
        poseCopy.accuracy = other.accuracy;
        return poseCopy;
    }

    /// <summary>
    /// Update the timestamp offset.
    /// </summary>
    /// <param name="pose">Tango pose to update for.</param>
    private void UpdateInterpolationData(TangoPoseData pose)
    {
        prevPose = currPose;
    
        // We need to make sure to deep copy the pose because it is
        // only guaranteed to be valid for the duration of our callback.
        currPose = DeepCopyTangoPose(pose);
        float timestampSmoothing = 0.95f;
        if (unityTimestampOffset < float.Epsilon)
        {
            unityTimestampOffset = (float)pose.timestamp - Time.realtimeSinceStartup;
        }
        else
        {
            unityTimestampOffset = (timestampSmoothing * unityTimestampOffset) + ((1 - timestampSmoothing) * ((float)pose.timestamp - Time.realtimeSinceStartup));
        }
    }

    /// <summary>
    /// Callback for tango permissions.
    /// </summary>
    /// <param name="permissionsGranted">If set to <c>true</c> permissions granted.</param>
    private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
    {
        popManager.StartApiFailCheck();
        if (permissionsGranted)
        {
            m_tangoApplication.InitApplication();
            m_tangoApplication.InitProviders(String.Empty);
            m_tangoApplication.ConnectToService();
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
        }
    }

    /// <summary>
    /// Unity 2D GUI.
    /// </summary>
    private void OnGUI()
    {
        // TODO(jason): temporarily comment out this part, to do is to move this button to someother debug functionality class.
        if (isShowingDebugButton)
        {
            if (GUI.Button(new Rect(Screen.width - 200, 50, 150, 80), "Reset Position"))
            {
                ComputeTransformUsingPose(out m_zeroPosition, out m_zeroRotation, currPose);
            }
        }
    }
}