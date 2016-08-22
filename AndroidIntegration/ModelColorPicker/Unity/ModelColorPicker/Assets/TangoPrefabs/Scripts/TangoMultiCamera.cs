// <copyright file="TangoMultiCamera.cs" company="Google">
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
using System.Collections;
using Tango;
using UnityEngine;

/// <summary>
/// Multiple type camera.
/// 
/// Can switch between First Person, Third Person, and Top Down camera types.
/// </summary>
public class TangoMultiCamera : MonoBehaviour
{
    /// <summary>
    /// The target object to follow.
    /// </summary>
    public GameObject m_targetFollowingObject;
    
    /// <summary>
    /// If set, display the camera switching UI via OnGUI.
    /// </summary>
    public bool m_enableCameraTypeUI = false;
    
    /// <summary>
    /// The default camera type.
    /// </summary>
    public CameraType m_defaultCameraType = CameraType.FirstPerson;

    // UI fields.
    private const float UI_BUTTON_SIZE_X = 125.0f;
    private const float UI_BUTTON_SIZE_Y = 65.0f;
    private const float UI_BUTTON_GAP_X = 5.0f;
    private const float UI_BUTTON_GAP_Y = 3.0f;

    /// <summary>
    /// Scaling factor when doing a pinch to zoom gesture.
    /// </summary>
    private const float PINCH_TO_ZOOM_SCALE = 10f;

    /// <summary>
    /// Scaling factor when doing a pan gesture.
    /// </summary>
    private const float TOP_DOWN_PAN_SCALE = 1 / 300f;
    
    /// <summary>
    /// Minimum zoom allowed when in third person mode.
    /// </summary>
    private const float THIRD_PERSON_PINCH_TO_ZOOM_MIN = 0.5f;
    
    /// <summary>
    /// Maximum zoom allowed when in third person mode.
    /// </summary>
    private const float THIRD_PERSON_PINCH_TO_ZOOM_MAX = 20f;
    
    /// <summary>
    /// Minimum zoom allowed when in top down mode.
    /// </summary>
    private const float TOP_DOWN_PINCH_TO_ZOOM_MIN = 1.5f;
    
    /// <summary>
    /// Maximum zoom allowed when in top down mode.
    /// </summary>
    private const float TOP_DOWN_PINCH_TO_ZOOM_MAX = 100f;

    /// <summary>
    /// The current camera type.
    /// </summary>
    private CameraType m_currentCamera;

    /// <summary>
    /// The starting position of a single finger touch.
    /// 
    /// Only valid during the single finger touch.
    /// </summary>
    private Vector2 m_touchStartPosition;

    /// <summary>
    /// The starting Manhattan distance between two fingers.
    /// 
    /// Only valid while both fingers are both part of the touch.
    /// </summary>
    private float m_touchStartDistance;

    /// <summary>
    /// The camera's rotation stored as Euler angles.
    /// 
    /// Used while using the ThirdPerson camera type.
    /// </summary>
    private Vector3 m_thirdPersonRotationEuler;

    /// <summary>
    /// Stores the starting value of <c>m_thirdPersonRotationEuler</c> while a gesture is in progress.
    /// </summary>
    private Vector3 m_thirdPersonRotationEulerStart;

    /// <summary>
    /// The camera's distance from the target.
    /// 
    /// Used while using the ThirdPerson camera type.
    /// </summary>
    private float m_thirdPersonDistance;

    /// <summary>
    /// Stores the starting value of <c>m_thirdPersonDistance</c> while a gesture is in progress.
    /// </summary>
    private float m_thirdPersonDistanceStart;

    /// <summary>
    /// The offset relative to the Tango pose for the camera.
    ///
    /// Used while using the TopDown camera type.
    /// </summary>
    private Vector3 m_topDownOffset = new Vector3(0.0f, 7.0f, 0.0f);

    /// <summary>
    /// Stores the starting value of <c>m_topDownCamOffset</c> while a gesture is in progress.
    /// </summary>
    private Vector3 m_topDownOffsetStart;

    /// <summary>
    /// The different camera types supported by this multi-camera.
    /// </summary>
    public enum CameraType
    {
        FirstPerson,
        ThirdPerson,
        TopDown
    }

    /// @cond
    /// <summary>
    /// Start is called on the frame when a script is enabled.
    /// </summary>
    public void Start() 
    {
        EnableCamera(m_defaultCameraType);
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called.
    /// </summary>
    public void LateUpdate()
    {
        switch (m_currentCamera)
        {
        case CameraType.FirstPerson:
            transform.position = m_targetFollowingObject.transform.position;
            transform.rotation = m_targetFollowingObject.transform.rotation;
            break;
            
        case CameraType.ThirdPerson:
            if (Input.touchCount == 1)
            {
                // Single touch rotates around
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    m_touchStartPosition = touch.position;
                    m_thirdPersonRotationEulerStart = m_thirdPersonRotationEuler;
                }
                else if (touch.phase == TouchPhase.Moved && GUIUtility.hotControl == 0)
                {
                    Vector2 delta = touch.position - m_touchStartPosition;
                    
                    m_thirdPersonRotationEuler.x = Mathf.Clamp(m_thirdPersonRotationEulerStart.x - delta.y, -90, 90);
                    m_thirdPersonRotationEuler.y = m_thirdPersonRotationEulerStart.y + delta.x;
                }
            }

            if (Input.touchCount == 2)
            {
                // Multiple touch does pinch to zoom.
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                if (touch1.phase == TouchPhase.Began)
                {
                    float xDist = Mathf.Abs(touch0.position.x - touch1.position.x);
                    float yDist = Mathf.Abs(touch0.position.y - touch1.position.y);
                    m_touchStartDistance = xDist + yDist;
                    m_thirdPersonDistanceStart = m_thirdPersonDistance;
                }
                else if ((touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                         && GUIUtility.hotControl == 0)
                {
                    float xDist = Mathf.Abs(touch0.position.x - touch1.position.x);
                    float yDist = Mathf.Abs(touch0.position.y - touch1.position.y);
                    float curTouchDist = xDist + yDist;
                    float delta = PINCH_TO_ZOOM_SCALE * (m_touchStartDistance - curTouchDist) / (Screen.width + Screen.height);
                    
                    m_thirdPersonDistance = Mathf.Clamp(m_thirdPersonDistanceStart + delta,
                                                        THIRD_PERSON_PINCH_TO_ZOOM_MIN,
                                                        THIRD_PERSON_PINCH_TO_ZOOM_MAX);
                }
                else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
                {
                    if (touch0.phase == TouchPhase.Ended)
                    {
                        m_touchStartPosition = touch1.position;
                        m_thirdPersonRotationEulerStart = m_thirdPersonRotationEuler;
                    }
                    else if (touch1.phase == TouchPhase.Ended)
                    {
                        m_touchStartPosition = touch0.position;
                        m_thirdPersonRotationEulerStart = m_thirdPersonRotationEuler;
                    }
                }
            }
            
            Vector3 camOffset = Quaternion.Euler(m_thirdPersonRotationEuler) * Vector3.back * m_thirdPersonDistance;
            transform.position = m_targetFollowingObject.transform.position + camOffset;
            transform.LookAt(m_targetFollowingObject.transform.position);
            break;

        case CameraType.TopDown:
            if (Input.touchCount == 1)
            {
                // Single touch pans around.
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    m_touchStartPosition = touch.position;
                    m_topDownOffsetStart = m_topDownOffset;
                }
                else if (touch.phase == TouchPhase.Moved && GUIUtility.hotControl == 0)
                {
                    Vector2 delta = TOP_DOWN_PAN_SCALE * (touch.position - m_touchStartPosition);
                    
                    m_topDownOffset.x = m_topDownOffsetStart.x - delta.x;
                    m_topDownOffset.z = m_topDownOffsetStart.z - delta.y;
                }
            }
            else if (Input.touchCount == 2)
            {
                // Multiple touch does pinch to zoom.
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                if (touch1.phase == TouchPhase.Began)
                {
                    float xDist = Mathf.Abs(touch0.position.x - touch1.position.x);
                    float yDist = Mathf.Abs(touch0.position.y - touch1.position.y);
                    m_touchStartDistance = xDist + yDist;
                    m_topDownOffsetStart = m_topDownOffset;
                }
                else if ((touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                         && GUIUtility.hotControl == 0)
                {
                    float xDist = Mathf.Abs(touch0.position.x - touch1.position.x);
                    float yDist = Mathf.Abs(touch0.position.y - touch1.position.y);
                    float curTouchDist = xDist + yDist;
                    float delta = PINCH_TO_ZOOM_SCALE * (m_touchStartDistance - curTouchDist) / (Screen.width + Screen.height);

                    m_topDownOffset.y = Mathf.Clamp(m_topDownOffsetStart.y + delta,
                                                    TOP_DOWN_PINCH_TO_ZOOM_MIN,
                                                    TOP_DOWN_PINCH_TO_ZOOM_MAX);
                }
                else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
                {
                    if (touch0.phase == TouchPhase.Ended)
                    {
                        m_touchStartPosition = touch1.position;
                        m_topDownOffsetStart = m_topDownOffset;
                    }
                    else if (touch1.phase == TouchPhase.Ended)
                    {
                        m_touchStartPosition = touch0.position;
                        m_topDownOffsetStart = m_topDownOffset;
                    }
                }
            }

            transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
            transform.position = m_targetFollowingObject.transform.position + m_topDownOffset;
            break;
        }
    }
    
    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// </summary>
    public void OnGUI()
    {
        if (!m_enableCameraTypeUI)
        {
            return;
        }

        if (GUI.Button(new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X, 
                                Screen.height - ((UI_BUTTON_SIZE_Y + UI_BUTTON_GAP_Y) * 3),
                                UI_BUTTON_SIZE_X, 
                                UI_BUTTON_SIZE_Y),
                       "<size=20>First</size>"))
        {
            EnableCamera(CameraType.FirstPerson);
        }

        if (GUI.Button(new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X, 
                                Screen.height - ((UI_BUTTON_SIZE_Y + UI_BUTTON_GAP_Y) * 2),
                                UI_BUTTON_SIZE_X, 
                                UI_BUTTON_SIZE_Y),
                       "<size=20>Third</size>"))
        {
            EnableCamera(CameraType.ThirdPerson);
        }

        if (GUI.Button(new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X, 
                                Screen.height - (UI_BUTTON_SIZE_Y + UI_BUTTON_GAP_Y),
                                UI_BUTTON_SIZE_X, 
                                UI_BUTTON_SIZE_Y),
                       "<size=20>Top</size>"))
        {
            EnableCamera(CameraType.TopDown);
        }
    }

    /// @endcond
    /// <summary>
    /// Set the active camera type.
    /// </summary>
    /// <param name="cameraType">Camera type.</param>
    public void EnableCamera(CameraType cameraType)
    {
        switch (cameraType)
        {
        case CameraType.FirstPerson:
            // Nothing to do, first person camera has no state.
            break;

        case CameraType.ThirdPerson:
            m_thirdPersonRotationEuler.Set(45, -45, 0);
            m_thirdPersonDistance = 7;
            break;
            
        case CameraType.TopDown:
            m_topDownOffset.Set(0, 7, 0);
            break;
        }

        m_currentCamera = cameraType;
    }
}
