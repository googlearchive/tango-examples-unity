// <copyright file="TangoGestureCamera.cs" company="Google">
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
/// Orbit camera.
/// </summary>
public class TangoGestureCamera : MonoBehaviour
{
    public GameObject m_targetFollowingObject;

    // Set this to enable the First / Third / Top UI buttons.
    public bool m_enableCameraModeUI = false;

    // The default camera mode.
    public CameraType m_defaultCameraMode = CameraType.FIRST_PERSON;

    // UI fields.
    private const float UI_BUTTON_SIZE_X = 125.0f;
    private const float UI_BUTTON_SIZE_Y = 65.0f;
    private const float UI_BUTTON_GAP_X = 5.0f;
    private const float UI_BUTTON_GAP_Y = 3.0f;

    private Vector3 m_curOffset;
    
    private Vector3 m_thirdPersonCamOffset = new Vector3(0.0f, 3.0f, -3.0f);
    private Vector3 m_topDownCamOffset = new Vector3(0.0f, 7.0f, 0.0f);
    
    private CameraType m_currentCamera;
    
    private float curThirdPersonRotationX = 180.0f;
    private float curThirdPersonRotationY = 0.0f;

    private float startThirdPersonRotationX = 45.0f;
    private float startThirdPersonRotationY = -45.0f;

    private float startThirdPersonCameraCircleR = 0.0f;
    private float curThirdPersonCameraCircleR = 7.0f;

    private Vector2 touchStartPoint = Vector2.zero;
    private float topDownStartY = 0.0f;
    
    private float touchStartDist = 0.0f;
    
    private Vector2 topDownStartPos = Vector2.zero;
    private Vector3 thirdPersonCamStartOffset;

    /// <summary>
    /// Camera type enum.
    /// </summary>
    public enum CameraType
    {
        FIRST_PERSON = 0x1,
        THIRD_PERSON = 0x2,
        TOP_DOWN = 0x4
    }

    /// <summary>
    /// Enabled based on camera type.
    /// </summary>
    /// <param name="cameraType">Enable which camera.</param>
    public void EnableCamera(CameraType cameraType)
    {
        switch (cameraType)
        {
            case CameraType.FIRST_PERSON:
            {
                transform.position = m_targetFollowingObject.transform.position;
                transform.rotation = m_targetFollowingObject.transform.rotation;
                break;
            }

            case CameraType.THIRD_PERSON:
            {
                startThirdPersonRotationX = 45.0f;
                startThirdPersonRotationY = -45.0f;
                startThirdPersonCameraCircleR = 0.0f;
                curThirdPersonCameraCircleR = 7.0f;
                curThirdPersonRotationX = startThirdPersonRotationX + (Mathf.PI * (0.0f / Screen.width));
                curThirdPersonRotationY = startThirdPersonRotationY + (Mathf.PI * (0.0f / Screen.height));
                
                Vector3 newPos = new Vector3(curThirdPersonCameraCircleR * Mathf.Sin(curThirdPersonRotationX),
                                             curThirdPersonCameraCircleR * Mathf.Cos(curThirdPersonRotationY),
                                             curThirdPersonCameraCircleR * Mathf.Cos(curThirdPersonRotationX));
                m_curOffset = m_thirdPersonCamOffset = newPos;
                
                transform.position = m_targetFollowingObject.transform.position + m_curOffset;
                transform.LookAt(m_targetFollowingObject.transform.position);
                break;
            }

            case CameraType.TOP_DOWN:
            {
                m_topDownCamOffset = new Vector3(0.0f, 7.0f, 0.0f);
                break;
            }
        }

        m_currentCamera = cameraType;
    }

    /// @cond
    /// <summary>
    /// Set up cameras.
    /// </summary>
    private void Start() 
    {
        Application.targetFrameRate = 60;
        EnableCamera(m_defaultCameraMode);
    }

    /// <summary>
    /// Updates, take touching event.
    /// </summary>
    private void LateUpdate()
    {
        if (m_currentCamera == CameraType.FIRST_PERSON)
        {
            transform.position = m_targetFollowingObject.transform.position;
            transform.rotation = m_targetFollowingObject.transform.rotation;
        }

        if (m_currentCamera == CameraType.THIRD_PERSON)
        {
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                touchStartPoint = Input.GetTouch(0).position;
                startThirdPersonRotationX = curThirdPersonRotationX;
                startThirdPersonRotationY = curThirdPersonRotationY;
            }

            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved && GUIUtility.hotControl == 0)
            {
                Vector2 offset = Input.touches[0].deltaPosition;
                curThirdPersonRotationX += -offset.y;
                curThirdPersonRotationY += offset.x;
                curThirdPersonRotationX = Mathf.Clamp(curThirdPersonRotationX, -89, 89);
            }
            
            if (Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began)
            {
                startThirdPersonCameraCircleR = curThirdPersonCameraCircleR;
                touchStartDist = Mathf.Abs(Input.GetTouch(0).position.x - Input.GetTouch(1).position.x) +
                    Mathf.Abs(Input.GetTouch(0).position.y - Input.GetTouch(1).position.y);
            }

            if (Input.touchCount == 2
                && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
                && GUIUtility.hotControl == 0)
            {
                float curTouchDist = Mathf.Abs(Input.GetTouch(0).position.x - Input.GetTouch(1).position.x) +
                    Mathf.Abs(Input.GetTouch(0).position.y - Input.GetTouch(1).position.y);
                
                float tmp = 10 * (touchStartDist - curTouchDist) / (Screen.width + Screen.height);
                curThirdPersonCameraCircleR = startThirdPersonCameraCircleR + tmp;
                curThirdPersonCameraCircleR = Mathf.Clamp(curThirdPersonCameraCircleR, 0.5f, 20.0f);
            }
            
            m_thirdPersonCamOffset = Quaternion.Euler(curThirdPersonRotationX, curThirdPersonRotationY, 0.0f) * new Vector3(0.0f, 0.0f, -curThirdPersonCameraCircleR);
            m_curOffset = m_thirdPersonCamOffset;
            transform.position = m_targetFollowingObject.transform.position + m_curOffset;
            transform.LookAt(m_targetFollowingObject.transform.position);
        }

        if (m_currentCamera == CameraType.TOP_DOWN)
        {
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                touchStartPoint = Input.GetTouch(0).position;
                topDownStartPos = new Vector2(m_topDownCamOffset.x, 
                                              m_topDownCamOffset.z);
            }

            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved && GUIUtility.hotControl == 0)
            {
                Vector2 offset = Input.GetTouch(0).position - touchStartPoint;
                Vector2 curPos = topDownStartPos - (offset / 300.0f);
                
                Vector3 newPos = new Vector3(curPos.x, m_topDownCamOffset.y, curPos.y);
                m_topDownCamOffset = newPos;
            }

            if (Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began)
            {
                touchStartDist = Mathf.Abs(Input.GetTouch(0).position.x - Input.GetTouch(1).position.x) +
                    Mathf.Abs(Input.GetTouch(0).position.y - Input.GetTouch(1).position.y);
                topDownStartY = m_topDownCamOffset.y;
            }

            if (Input.touchCount == 2
                && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
                && GUIUtility.hotControl == 0)
            {
                float curTouchDist = Mathf.Abs(Input.GetTouch(0).position.x - Input.GetTouch(1).position.x) +
                    Mathf.Abs(Input.GetTouch(0).position.y - Input.GetTouch(1).position.y);
                float offset = 10.0f * (touchStartDist - curTouchDist) / (Screen.width + Screen.height);
                Vector3 newPos = new Vector3(m_topDownCamOffset.x, 
                                             Mathf.Clamp(topDownStartY + offset, 1.5f, 100.0f), 
                                             m_topDownCamOffset.z);
                
                m_topDownCamOffset = newPos;
            }

            if (Input.touchCount == 2 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(1).phase == TouchPhase.Ended))
            {
                if (Input.GetTouch(0).phase == TouchPhase.Ended)
                {
                    touchStartPoint = Input.GetTouch(1).position;
                    topDownStartPos = new Vector2(m_topDownCamOffset.x,
                                                  m_topDownCamOffset.z);
                }

                if (Input.GetTouch(1).phase == TouchPhase.Ended)
                {
                    touchStartPoint = Input.GetTouch(0).position;
                    topDownStartPos = new Vector2(m_topDownCamOffset.x,
                                                  m_topDownCamOffset.z);
                }
            }

            transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
            transform.position = m_targetFollowingObject.transform.position + m_topDownCamOffset;
        }
    }
    
    /// <summary>
    /// Draw buttons to swap current behavior. 
    /// </summary>
    private void OnGUI()
    {
        if (!m_enableCameraModeUI)
        {
            return;
        }

        if (GUI.Button(new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X, 
                                Screen.height - ((UI_BUTTON_SIZE_Y + UI_BUTTON_GAP_Y) * 3),
                                UI_BUTTON_SIZE_X, 
                                UI_BUTTON_SIZE_Y), "<size=20>First</size>"))
        {
            EnableCamera(CameraType.FIRST_PERSON);
        }

        if (GUI.Button(new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X, 
                                Screen.height - ((UI_BUTTON_SIZE_Y + UI_BUTTON_GAP_Y) * 2),
                                UI_BUTTON_SIZE_X, 
                                UI_BUTTON_SIZE_Y), "<size=20>Third</size>"))
        {
            EnableCamera(CameraType.THIRD_PERSON);
        }

        if (GUI.Button(new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X, 
                                Screen.height - (UI_BUTTON_SIZE_Y + UI_BUTTON_GAP_Y),
                                UI_BUTTON_SIZE_X, 
                                UI_BUTTON_SIZE_Y), "<size=20>Top</size>"))
        {
            EnableCamera(CameraType.TOP_DOWN);
        }
    }

    /// @endcond
}
