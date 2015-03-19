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
using UnityEngine;
using System;
using Tango;

/// <summary>
/// FPS counter.
/// </summary>
public class GUIController : MonoBehaviour {
    private const float m_updateFrequency = 1.0f;

    public EventLogger m_tangoEventController;
    public PoseController m_tangoPoseController;

    private string m_FPSText;
    private int m_currentFPS;
    private int m_framesSinceUpdate;
    private float m_accumulation;
    private float m_currentTime;

    private Rect m_label;
    private TangoApplication m_tangoApplication;
    
    // Use this for initialization
    void Start () 
    {
        m_currentFPS = 0;
        m_framesSinceUpdate = 0;
        m_currentTime = 0.0f;
        m_FPSText = "FPS = Calculating";
        m_label = new Rect(Screen.width * 0.025f - 50, Screen.height * 0.96f - 25, 600.0f, 50.0f);
        m_tangoApplication = FindObjectOfType<TangoApplication>();
    }

    // Update is called once per frame
    void Update () 
    {
        m_currentTime += Time.deltaTime;
        ++m_framesSinceUpdate;
        m_accumulation += Time.timeScale / Time.deltaTime;
        if(m_currentTime >= m_updateFrequency)
        {
            m_currentFPS = (int)(m_accumulation/m_framesSinceUpdate);
            m_currentTime = 0.0f;
            m_framesSinceUpdate = 0;
            m_accumulation = 0.0f;
            m_FPSText = "FPS: " + m_currentFPS;
        }
    }
    
    /// <summary>
    /// Construct readable string from TangoPoseStatusType.
    /// </summary>
    private string _GetLoggingStringFromPoseStatus(TangoEnums.TangoPoseStatusType status)
    {
        string statusString = "";
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
    private string _GetLoggingStringFromVec3(Vector3 vec)
    {
        if(vec == Vector3.zero)
        {
            return "N/A";
        }
        else
        {
            return string.Format("{0}, {1}, {2}", 
                                 vec.x.ToString(Common.UI_FLOAT_FORMAT),
                                 vec.y.ToString(Common.UI_FLOAT_FORMAT),
                                 vec.z.ToString(Common.UI_FLOAT_FORMAT));
        }
    }
    
    /// <summary>
    /// Reformat string from quaternion type for data logging.
    /// </summary>
    private string _GetLoggingStringFromQuaternion(Quaternion quat)
    {
        if(quat == Quaternion.identity)
        {
            return "N/A";
        }
        else
        {
            return string.Format("{0}, {1}, {2}, {3}",
                                 quat.x.ToString(Common.UI_FLOAT_FORMAT),
                                 quat.y.ToString(Common.UI_FLOAT_FORMAT),
                                 quat.z.ToString(Common.UI_FLOAT_FORMAT),
                                 quat.w.ToString(Common.UI_FLOAT_FORMAT));
        }
    }
    
    /// <summary>
    /// Return a string to the get logging from frame count.
    /// </summary>
    /// <returns>The get logging string from frame count.</returns>
    /// <param name="frameCount">Frame count.</param>
    private string _GetLoggingStringFromFrameCount(int frameCount)
    {
        if(frameCount == -1.0)
        {
            return "N/A";
        }
        else
        {
            return frameCount.ToString();
        }
    }
    
    /// <summary>
    /// Return a string to get logging of FrameDeltaTime
    /// </summary>
    /// <returns>The get loggin string from frame delta time.</returns>
    /// <param name="frameDeltaTime">Frame delta time.</param>
    private string _GetLogginStringFromFrameDeltaTime(float frameDeltaTime)
    {
        if(frameDeltaTime == -1.0)
        {
            return "N/A";
        }
        else
        {
            return (frameDeltaTime * Common.SECOND_TO_MILLISECOND).ToString(Common.UI_FLOAT_FORMAT);
        }
    }

    void OnGUI()
    {
        if(m_tangoApplication.HasRequestedPermissions())
        {
            Color oldColor = GUI.color;
            GUI.color = Color.white;
            
            if (GUI.Button(new Rect(Common.UI_BUTTON_GAP_X, 
                                    Screen.height - (Common.UI_BUTTON_SIZE_Y + Common.UI_LABEL_GAP_Y),
                                    Common.UI_BUTTON_SIZE_X + 100, 
                                    Common.UI_BUTTON_SIZE_Y), "<size=20>Reset motion tracking</size>"))
            {
                PoseProvider.ResetMotionTracking();
            }

            GUI.color = Color.black;
            GUI.Label(new Rect(Common.UI_LABEL_START_X, 
                               Common.UI_LABEL_START_Y, 
                               Common.UI_LABEL_SIZE_X , 
                               Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TANGO_SERVICE_VERSION, m_tangoPoseController.m_tangoServiceVersionName) + "</size>");

            GUI.Label(new Rect(Common.UI_LABEL_START_X, 
                               Common.UI_FPS_LABEL_START_Y, 
                               Common.UI_LABEL_SIZE_X , 
                               Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + m_FPSText + "</size>");

            GUI.Label(new Rect(Common.UI_LABEL_START_X,
                               Common.UI_EVENT_LABEL_START_Y, 
                               Common.UI_LABEL_SIZE_X ,
                               Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TANGO_SYSTEM_EVENT, m_tangoEventController.m_eventString) + "</size>");

            // MOTION TRACKING
            GUI.Label( new Rect(Common.UI_LABEL_START_X, 
                                Common.UI_POSE_LABEL_START_Y,
                                Common.UI_LABEL_SIZE_X , 
                                Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TARGET_TO_BASE_FRAME,
                                                                         "Device",
                                                                         "Start") + "</size>");
            
            GUI.Label( new Rect(Common.UI_LABEL_START_X, 
                                Common.UI_POSE_LABEL_START_Y + Common.UI_LABEL_OFFSET,
                                Common.UI_LABEL_SIZE_X , 
                                Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_STATUS,
                                                                         _GetLoggingStringFromPoseStatus(m_tangoPoseController.m_status),
                                                                         _GetLoggingStringFromFrameCount(m_tangoPoseController.m_frameCount),
                                                                         _GetLogginStringFromFrameDeltaTime(m_tangoPoseController.m_frameDeltaTime),
                                                                         _GetLoggingStringFromVec3(m_tangoPoseController.transform.position),
                                                                         _GetLoggingStringFromQuaternion(m_tangoPoseController.transform.rotation)) + "</size>");

            
            GUI.color = oldColor;
        }
    }
}
