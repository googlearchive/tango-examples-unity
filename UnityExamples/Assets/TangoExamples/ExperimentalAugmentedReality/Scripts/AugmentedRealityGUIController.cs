//-----------------------------------------------------------------------
// <copyright file="AugmentedRealityGUIController.cs" company="Google">
//   
// Copyright 2015 Google Inc. All Rights Reserved.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using UnityEngine;
using Tango;

/// <summary>
/// GUI controller controls all the debug overlay to show the data for poses.
/// </summary>
public class AugmentedRealityGUIController : MonoBehaviour
{
    // Constant value for controlling the position and size of debug overlay.
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
    public const string UX_STATUS = "\tstatus: {0}, count: {1}, position (m): [{2}], orientation: [{3}]";
    public const float SECOND_TO_MILLISECOND = 1000.0f;

    /// <summary>
    /// How big (in pixels) is a tap?
    /// </summary>
    public const int TAP_PIXEL_TOLERANCE = 40;

    public ARScreen m_arScreen;

    /// <summary>
    /// The location prefab to place on taps.
    /// </summary>
    public GameObject m_prefabLocation;

    /// <summary>
    /// The point cloud object in the scene.
    /// </summary>
    public TangoPointCloud m_pointCloud;
    
    private const float FPS_UPDATE_FREQUENCY = 1.0f;
    private string m_fpsText;
    private int m_currentFPS;
    private int m_framesSinceUpdate;
    private float m_accumulation;
    private float m_currentTime;
    
    private Rect m_label;
    private TangoApplication m_tangoApplication;
    private string m_tangoServiceVersion;

    private GameObject m_placedLocation = null;
    
    /// <summary>
    /// Unity Start() callback, we set up some initial values here.
    /// </summary>
    public void Start() 
    {
        m_currentFPS = 0;
        m_framesSinceUpdate = 0;
        m_currentTime = 0.0f;
        m_fpsText = "FPS = Calculating";
        m_label = new Rect((Screen.width * 0.025f) - 50, (Screen.height * 0.96f) - 25, 600.0f, 50.0f);
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoServiceVersion = TangoApplication.GetTangoServiceVersion();
    }
    
    /// <summary>
    /// Updates UI and handles player input.
    /// </summary>
    public void Update() 
    {
        m_currentTime += Time.deltaTime;
        ++m_framesSinceUpdate;
        m_accumulation += Time.timeScale / Time.deltaTime;
        if (m_currentTime >= FPS_UPDATE_FREQUENCY)
        {
            m_currentFPS = (int)(m_accumulation / m_framesSinceUpdate);
            m_currentTime = 0.0f;
            m_framesSinceUpdate = 0;
            m_accumulation = 0.0f;
            m_fpsText = "FPS: " + m_currentFPS;
        }

        _UpdatePlacedLocation();
    }
    
    /// <summary>
    /// Display simple GUI.
    /// </summary>
    public void OnGUI()
    {
        if (m_tangoApplication.HasRequestedPermissions())
        {
            Color oldColor = GUI.color;
            GUI.color = Color.white;
            
            GUI.color = Color.black;
            GUI.Label(new Rect(UI_LABEL_START_X, 
                               UI_LABEL_START_Y, 
                               UI_LABEL_SIZE_X, 
                               UI_LABEL_SIZE_Y), 
                      UI_FONT_SIZE + String.Format(UX_TANGO_SERVICE_VERSION, m_tangoServiceVersion) + "</size>");
            
            GUI.Label(new Rect(UI_LABEL_START_X, 
                               UI_FPS_LABEL_START_Y, 
                               UI_LABEL_SIZE_X, 
                               UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + m_fpsText + "</size>");
            
            // MOTION TRACKING
            GUI.Label(new Rect(UI_LABEL_START_X, 
                               UI_POSE_LABEL_START_Y - UI_LABEL_OFFSET,
                               UI_LABEL_SIZE_X, 
                               UI_LABEL_SIZE_Y),
                      UI_FONT_SIZE + String.Format(UX_TARGET_TO_BASE_FRAME, "Device", "Start") + "</size>");
            
            Vector3 pos = m_arScreen.transform.position;
            Quaternion quat = m_arScreen.transform.rotation;
            string positionString = pos.x.ToString(UI_FLOAT_FORMAT) + ", " + 
                pos.y.ToString(UI_FLOAT_FORMAT) + ", " + 
                    pos.z.ToString(UI_FLOAT_FORMAT);
            string rotationString = quat.x.ToString(UI_FLOAT_FORMAT) + ", " + 
                quat.y.ToString(UI_FLOAT_FORMAT) + ", " + 
                    quat.z.ToString(UI_FLOAT_FORMAT) + ", " + 
                    quat.w.ToString(UI_FLOAT_FORMAT);
            string statusString = String.Format(UX_STATUS,
                                                _GetLoggingStringFromPoseStatus(m_arScreen.m_status),
                                                _GetLoggingStringFromFrameCount(m_arScreen.m_frameCount),
                                                positionString, rotationString);
            GUI.Label(new Rect(UI_LABEL_START_X, 
                               UI_POSE_LABEL_START_Y,
                               UI_LABEL_SIZE_X, 
                               UI_LABEL_SIZE_Y), 
                      UI_FONT_SIZE + statusString + "</size>");
            GUI.color = oldColor;
        }
    }
    
    /// <summary>
    /// Construct readable string from TangoPoseStatusType.
    /// </summary>
    /// <param name="status">Pose status from Tango.</param>
    /// <returns>Readable string corresponding to status.</returns>
    private string _GetLoggingStringFromPoseStatus(TangoEnums.TangoPoseStatusType status)
    {
        string statusString;
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
    /// <param name="vec">Position to display.</param>
    /// <returns>Readable string corresponding to vec.</returns>
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
    /// <param name="quat">Quaternion to display.</param>
    /// <returns>Readable string corresponding to quat.</returns>
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
    /// <returns>The get loggin string from frame delta time.</returns>
    /// <param name="frameDeltaTime">Frame delta time.</param>
    private string _GetLogginStringFromFrameDeltaTime(float frameDeltaTime)
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

    /// <summary>
    /// Updates the active placed location.
    /// </summary>
    private void _UpdatePlacedLocation()
    {
        if (Input.touchCount != 1)
        {
            return;
        }
        
        Touch t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began)
        {
            return;
        }

        Camera cam = m_arScreen.m_renderCamera;
        int closestIndex = m_pointCloud.FindClosestPoint(cam, t.position, TAP_PIXEL_TOLERANCE);
        if (closestIndex < 0)
        {
            return;
        }

        if (m_placedLocation)
        {
            m_placedLocation.SendMessage("Hide");
        }

        float closestDepth = cam.WorldToScreenPoint(m_pointCloud.m_points[closestIndex]).z;
        Ray touchRay = cam.ScreenPointToRay(new Vector3(t.position[0], t.position[1], 0));
        Vector3 pos = touchRay.origin + (touchRay.direction * closestDepth);
                
        Vector3 rot = cam.transform.eulerAngles;
        rot[0] = rot[2] = 0;
        m_placedLocation = (GameObject)Instantiate(m_prefabLocation, pos, Quaternion.Euler(rot));
    }
}