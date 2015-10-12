//-----------------------------------------------------------------------
// <copyright file="AugmentedRealityGUIController.cs" company="Google">
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
using System.Collections.Generic;
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
    public const float UI_BUTTON_SIZE_X = 250.0f;
    public const float UI_BUTTON_SIZE_Y = 130.0f;
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
    /// How big (in pixels) is a tap.
    /// </summary>
    public const float TAP_PIXEL_TOLERANCE = 40;

    /// <summary>
    /// Minimum inlier percentage to consider a plane a fit.
    /// </summary>
    public const float MIN_PLANE_FIT_PERCENTAGE = 0.8f;

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
    private TangoARPoseController m_tangoPose;
    private string m_tangoServiceVersion;

    /// <summary>
    /// If set, this is the selected marker.
    /// </summary>
    private ARLocationMarker m_selectedMarker;

    /// <summary>
    /// If set, this is the rectangle bounding the selected marker.
    /// </summary>
    private Rect m_selectedRect;

    /// <summary>
    /// If set, this is the rectangle for the Hide All button.
    /// </summary>
    private Rect m_hideAllRect;

    /// <summary>
    /// If set, show debug text.
    /// </summary>
    private bool m_showDebug = false;
    
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
        m_tangoPose = FindObjectOfType<TangoARPoseController>();
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

        _UpdateLocationMarker();
    }
    
    /// <summary>
    /// Display simple GUI.
    /// </summary>
    public void OnGUI()
    {
        if (m_showDebug && m_tangoApplication.HasRequestedPermissions())
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
            
            Vector3 pos = m_tangoPose.transform.position;
            Quaternion quat = m_tangoPose.transform.rotation;
            string positionString = pos.x.ToString(UI_FLOAT_FORMAT) + ", " + 
                pos.y.ToString(UI_FLOAT_FORMAT) + ", " + 
                    pos.z.ToString(UI_FLOAT_FORMAT);
            string rotationString = quat.x.ToString(UI_FLOAT_FORMAT) + ", " + 
                quat.y.ToString(UI_FLOAT_FORMAT) + ", " + 
                    quat.z.ToString(UI_FLOAT_FORMAT) + ", " + 
                    quat.w.ToString(UI_FLOAT_FORMAT);
            string statusString = String.Format(UX_STATUS,
                                                _GetLoggingStringFromPoseStatus(m_tangoPose.m_poseStatus),
                                                _GetLoggingStringFromFrameCount(m_tangoPose.m_poseCount),
                                                positionString, rotationString);
            GUI.Label(new Rect(UI_LABEL_START_X, 
                               UI_POSE_LABEL_START_Y,
                               UI_LABEL_SIZE_X, 
                               UI_LABEL_SIZE_Y), 
                      UI_FONT_SIZE + statusString + "</size>");
            GUI.color = oldColor;
        }

        if (m_selectedMarker != null)
        {
            Renderer selectedRenderer = m_selectedMarker.GetComponent<Renderer>();

            // GUI's Y is flipped from the mouse's Y
            Rect screenRect = WorldBoundsToScreen(Camera.main, selectedRenderer.bounds);
            float yMin = Screen.height - screenRect.yMin;
            float yMax = Screen.height - screenRect.yMax;
            screenRect.yMin = Mathf.Min(yMin, yMax);
            screenRect.yMax = Mathf.Max(yMin, yMax);

            if (GUI.Button(screenRect, "<size=30>Hide</size>"))
            {
                m_selectedMarker.SendMessage("Hide");
                m_selectedMarker = null;
                m_selectedRect = new Rect();
            }
            else
            {
                m_selectedRect = screenRect;
            }
        }
        else
        {
            m_selectedRect = new Rect();
        }

        if (GameObject.FindObjectOfType<ARLocationMarker>() != null)
        {
            m_hideAllRect = new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X,
                                     Screen.height - UI_BUTTON_SIZE_Y - UI_BUTTON_GAP_X,
                                     UI_BUTTON_SIZE_X,
                                     UI_BUTTON_SIZE_Y);
            if (GUI.Button(m_hideAllRect, "<size=30>Hide All</size>"))
            {
                foreach (ARLocationMarker marker in GameObject.FindObjectsOfType<ARLocationMarker>())
                {
                    marker.SendMessage("Hide");
                }
            }
        }
        else
        {
            m_hideAllRect = new Rect(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Convert a 3D bounding box into a 2D Rect.
    /// </summary>
    /// <returns>The 2D Rect in Screen coordinates.</returns>
    /// <param name="cam">Camera to use.</param>
    /// <param name="bounds">3D bounding box.</param>
    private Rect WorldBoundsToScreen(Camera cam, Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Bounds screenBounds = new Bounds(cam.WorldToScreenPoint(center), Vector3.zero);
        
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, +extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, +extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, -extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, -extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, +extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, +extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, -extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, -extents.y, -extents.z)));
        return Rect.MinMaxRect(screenBounds.min.x, screenBounds.min.y, screenBounds.max.x, screenBounds.max.y);
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
    /// Update location marker state.
    /// </summary>
    private void _UpdateLocationMarker()
    {
        if (Input.touchCount == 1)
        {
            // Single tap -- place new location or select existing location.
            Touch t = Input.GetTouch(0);
            Vector2 guiPosition = new Vector2(t.position.x, Screen.height - t.position.y);
            Camera cam = Camera.main;
            RaycastHit hitInfo;

            if (t.phase != TouchPhase.Began)
            {
                return;
            }

            if (m_selectedRect.Contains(guiPosition) || m_hideAllRect.Contains(guiPosition))
            {
                // do nothing, the button will handle it
            }
            else if (Physics.Raycast(cam.ScreenPointToRay(t.position), out hitInfo))
            {
                // Found a marker, select it (so long as it isn't disappearing)!
                GameObject tapped = hitInfo.collider.gameObject;
                if (!tapped.GetComponent<Animation>().isPlaying)
                {
                    m_selectedMarker = tapped.GetComponent<ARLocationMarker>();
                }
            }
            else
            {
                // Place a new point at that location, clear selection
                Vector3 planeCenter;
                Plane plane;
                if (!m_pointCloud.FindPlane(cam, t.position,
                                            TAP_PIXEL_TOLERANCE, MIN_PLANE_FIT_PERCENTAGE,
                                            out planeCenter, out plane))
                {
                    return;
                }

                // Ensure the location is always facing the camera.  This is like a LookRotation, but for the Y axis.
                Vector3 up = plane.normal;
                Vector3 forward;
                if (Vector3.Angle(plane.normal, cam.transform.forward) < 175)
                {
                    Vector3 right = Vector3.Cross(up, cam.transform.forward).normalized;
                    forward = Vector3.Cross(right, up).normalized;
                }
                else
                {
                    // Normal is nearly parallel to camera look direction, the cross product would have too much
                    // floating point error in it.
                    forward = Vector3.Cross(up, cam.transform.right);
                }
                Instantiate(m_prefabLocation, planeCenter, Quaternion.LookRotation(forward, up));
                m_selectedMarker = null;
            }
        }
        if (Input.touchCount == 2)
        {
            // Two taps -- toggle debug text
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t0.phase != TouchPhase.Began && t1.phase != TouchPhase.Began)
            {
                return;
            }

            m_showDebug = !m_showDebug;
            return;
        }

        if (Input.touchCount != 1)
        {
            return;
        }
    }
}
