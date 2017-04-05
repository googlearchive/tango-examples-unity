//-----------------------------------------------------------------------
// <copyright file="PointCloudGUIController.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// FPS counter.
/// </summary>
public class PointCloudGUIController : MonoBehaviour
{
    public const float UI_LABEL_START_X = 15.0f;
    public const float UI_LABEL_START_Y = 15.0f;
    public const float UI_LABEL_SIZE_X = 1920.0f;
    public const float UI_LABEL_SIZE_Y = 35.0f;
    public const float UI_LABEL_GAP_Y = 3.0f;
    public const float UI_BUTTON_SIZE_X = 125.0f;
    public const float UI_BUTTON_SIZE_Y = 65.0f;
    public const float UI_BUTTON_GAP_X = 5.0f;
    public const float UI_CAMERA_BUTTON_OFFSET = UI_BUTTON_SIZE_X + UI_BUTTON_GAP_X;
    public const float UI_LABEL_OFFSET = UI_LABEL_GAP_Y + UI_LABEL_SIZE_Y;
    public const float UI_FPS_LABEL_START_Y = UI_LABEL_START_Y + UI_LABEL_OFFSET;
    public const float UI_EVENT_LABEL_START_Y = UI_FPS_LABEL_START_Y + UI_LABEL_OFFSET;
    public const float UI_POSE_LABEL_START_Y = UI_EVENT_LABEL_START_Y + UI_LABEL_OFFSET;
    public const float UI_DEPTH_LABLE_START_Y = UI_POSE_LABEL_START_Y + UI_LABEL_OFFSET;
    public const string UI_FLOAT_FORMAT = "F3";
    public const string UI_FONT_SIZE = "<size=25>";

    public const float UI_TANGO_VERSION_X = UI_LABEL_START_X;
    public const float UI_TANGO_VERSION_Y = UI_LABEL_START_Y;
    public const float UI_TANGO_APP_SPECIFIC_START_X = UI_TANGO_VERSION_X;
    public const float UI_TANGO_APP_SPECIFIC_START_Y = UI_TANGO_VERSION_Y + (UI_LABEL_OFFSET * 2);

    public const string UX_SERVICE_VERSION = "Service version: {0}";
    public const string UX_TANGO_SERVICE_VERSION = "Tango service version: {0}";
    public const string UX_TANGO_SYSTEM_EVENT = "Tango system event: {0}";
    public const string UX_TARGET_TO_BASE_FRAME = "Target->{0}, Base->{1}:";
    public const string UX_STATUS = "Position(m): [{0}], orientation: [{1}]";
    public const float SECOND_TO_MILLISECOND = 1000.0f;
    public TangoPoseController m_tangoPoseController;
    public TangoPointCloud m_pointcloud;

    private const float UPDATE_FREQUENCY = 1.0f;
    private string m_fpsText;
    private int m_currentFPS;
    private int m_framesSinceUpdate;
    private float m_accumulation;
    private float m_currentTime;

    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        m_currentFPS = 0;
        m_framesSinceUpdate = 0;
        m_currentTime = 0.0f;
        m_fpsText = "FPS = Calculating";
        m_tangoApplication = FindObjectOfType<TangoApplication>();
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        m_currentTime += Time.deltaTime;
        ++m_framesSinceUpdate;
        m_accumulation += Time.timeScale / Time.deltaTime;
        if (m_currentTime >= UPDATE_FREQUENCY)
        {
            m_currentFPS = (int)(m_accumulation / m_framesSinceUpdate);
            m_currentTime = 0.0f;
            m_framesSinceUpdate = 0;
            m_accumulation = 0.0f;
            m_fpsText = "FPS: " + m_currentFPS;
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            // This is a fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application
            // immediately results in a deadlocked app.
            AndroidHelper.AndroidQuit();
        }
    }

    /// <summary>
    /// Unity GUI callback.
    /// </summary>
    public void OnGUI()
    {
        if (m_tangoApplication.HasRequiredPermissions)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.black;

            GUI.Label(new Rect(UI_LABEL_START_X, UI_LABEL_START_Y, UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + String.Format(UX_TANGO_SERVICE_VERSION, TangoApplication.GetTangoServiceVersion()) + "</size>");

            GUI.Label(new Rect(UI_LABEL_START_X, UI_FPS_LABEL_START_Y, UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + m_fpsText + "</size>");

            // MOTION TRACKING
            GUI.Label(new Rect(UI_LABEL_START_X, UI_POSE_LABEL_START_Y - UI_LABEL_OFFSET, UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + String.Format(UX_TARGET_TO_BASE_FRAME, "Device", "Start") + "</size>");

            string logString = String.Format(UX_STATUS, 
                _GetLoggingStringFromVec3(m_tangoPoseController.transform.position),
                _GetLoggingStringFromQuaternion(m_tangoPoseController.transform.rotation));

            GUI.Label(new Rect(UI_LABEL_START_X, UI_POSE_LABEL_START_Y, UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + logString + "</size>");

            GUI.Label(new Rect(UI_LABEL_START_X, UI_DEPTH_LABLE_START_Y, UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + "Average depth (m): " + m_pointcloud.m_overallZ.ToString() + "</size>");

            GUI.Label(new Rect(UI_LABEL_START_X, UI_DEPTH_LABLE_START_Y + (UI_LABEL_OFFSET * 1.0f), UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + "Point count: " + m_pointcloud.m_pointsCount.ToString() + "</size>");

            GUI.Label(new Rect(UI_LABEL_START_X, UI_DEPTH_LABLE_START_Y + (UI_LABEL_OFFSET * 2.0f), UI_LABEL_SIZE_X, UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + "Frame delta time (ms): " + m_pointcloud.m_depthDeltaTime.ToString(UI_FLOAT_FORMAT) + "</size>");

            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// Construct readable string from TangoPoseStatusType.
    /// </summary>
    /// <returns>Printable string.</returns>
    /// <param name="status">A Tango pose status.</param>
    private string _GetLoggingStringFromPoseStatus(TangoEnums.TangoPoseStatusType status)
    {
        string statusString = string.Empty;
        switch (status)
        {
        case TangoEnums.TangoPoseStatusType.TANGO_POSE_INITIALIZING:
            statusString = "initializing";
            break;
        case TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID:
            statusString = "invalid";
            break;
        case TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN:
            statusString = "unknown";
            break;
        case TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID:
            statusString = "valid";
            break;
        default:
            statusString = "N/A";
            break;
        }

        return statusString;
    }

    /// <summary>
    /// Reformat string from vector3 type for data logging.
    /// </summary>
    /// <returns>Printable string.</returns>
    /// <param name="vec">A vector.</param>
    private string _GetLoggingStringFromVec3(Vector3 vec)
    {
        if (vec == Vector3.zero)
        {
            return "N/A";
        }
        else
        {
            return string.Format("{0}, {1}, {2}",
                                 vec.x.ToString(UI_FLOAT_FORMAT),
                                 vec.y.ToString(UI_FLOAT_FORMAT),
                                 vec.z.ToString(UI_FLOAT_FORMAT));
        }
    }

    /// <summary>
    /// Reformat string from quaternion type for data logging.
    /// </summary>
    /// <returns>Printable string.</returns>
    /// <param name="quat">A quaternion.</param>
    private string _GetLoggingStringFromQuaternion(Quaternion quat)
    {
        if (quat == Quaternion.identity)
        {
            return "N/A";
        }
        else
        {
            return string.Format("{0}, {1}, {2}, {3}",
                                 quat.x.ToString(UI_FLOAT_FORMAT),
                                 quat.y.ToString(UI_FLOAT_FORMAT),
                                 quat.z.ToString(UI_FLOAT_FORMAT),
                                 quat.w.ToString(UI_FLOAT_FORMAT));
        }
    }

    /// <summary>
    /// Return a string to the get logging from frame count.
    /// </summary>
    /// <returns>The get logging string from frame count.</returns>
    /// <param name="frameCount">Frame count.</param>
    private string _GetLoggingStringFromFrameCount(int frameCount)
    {
        if (frameCount == -1.0)
        {
            return "N/A";
        }
        else
        {
            return frameCount.ToString();
        }
    }

    /// <summary>
    /// Return a string to get logging of FrameDeltaTime.
    /// </summary>
    /// <returns>The logging string from frame delta time.</returns>
    /// <param name="frameDeltaTime">Frame delta time.</param>
    private string _GetLoggingStringFromFrameDeltaTime(float frameDeltaTime)
    {
        if (frameDeltaTime == -1.0)
        {
            return "N/A";
        }
        else
        {
            return (frameDeltaTime * SECOND_TO_MILLISECOND).ToString(UI_FLOAT_FORMAT);
        }
    }
}
