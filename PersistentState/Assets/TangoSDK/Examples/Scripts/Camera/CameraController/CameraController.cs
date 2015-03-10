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

/// <summary>
/// Updates the attached camera based on the current
/// active behavior.
/// </summary>
using Tango;


public class CameraController : MonoBehaviour
{
    /// <summary>
    /// Camera type enum.
    /// </summary>
    public enum CameraType
    {
        FIRST_PERSON = 0x1,
        THIRD_PERSON = 0x2,
        TOP_DOWN = 0x4
    }

    public GameObject m_targetObject;

    public bool m_showBehaviorButtons = true;
	public bool m_showCursor = true;
	public bool m_startInThirdPerson = false;

    private const float BUTTON_WIDTH_SCREEN_PERCENT = .15f;
    private const float BUTTON_HEIGHT_SCREEN_PERCENT = .1f;
    private const float BUTTON_X_POSITION_SCREEN_PERCENT = .8f;
    private const float BUTTON_Y_POSITION_SCREEN_PERCENT = .1f;
    private const float BUTTON_Y_SPACING_SCREEN_PERCENT = 0.01f;

    /// <summary>
    /// Property to get/set whether camera
    /// behavior buttons should be drawn to 
    /// the screen.
    /// </summary>
    /// <value> Bool - True if buttons should be shown.</value>
    public bool ShowBehaviorButtons
    {
        get
        {
            return m_showBehaviorButtons;
        }

        set
        {
            m_showBehaviorButtons = value;
        }
    }

    private CameraType m_currentCamera;
    private IBaseCamera m_firstPersonCamera;
    private IBaseCamera m_thirdPersonCamera;
    private IBaseCamera m_topDownCamera;

    /// <summary>
    /// Current camera type.
    /// </summary>
    /// <value> Current camera type to render.</value>
    public CameraType CurrentCamera
    {
        get
        {
            return m_currentCamera;
        }

        set
        {
            m_currentCamera = value;
        }
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
                m_firstPersonCamera.enabled = true;
                m_thirdPersonCamera.enabled = false;
                m_topDownCamera.enabled = false;
                Camera.main.fieldOfView = m_firstPersonCamera.m_fieldOfViewSetting;
                break;
            }
            case CameraType.THIRD_PERSON:
            {
                m_firstPersonCamera.enabled = false;
                m_thirdPersonCamera.enabled = true;
                m_topDownCamera.enabled = false;
                Camera.main.fieldOfView = m_thirdPersonCamera.m_fieldOfViewSetting;
                break;
            }
            case CameraType.TOP_DOWN:
            {
                m_firstPersonCamera.enabled = false;
                m_thirdPersonCamera.enabled = false;
                m_topDownCamera.enabled = true;
                Camera.main.fieldOfView = m_topDownCamera.m_fieldOfViewSetting;
                break;
            }
        }
        m_currentCamera = cameraType;
    }
		
	/// <summary>
	/// Takes care of setting both cursor visibility and lock state.
	/// Note: by default it will also lock the cursor in the center when hiding.
	/// </summary>
	private void _SetCursorState()
	{
		Screen.showCursor = m_showCursor;
		Screen.lockCursor = !m_showCursor;
	}

	/// <summary>
	/// Take care of toggling cursor visibility.
	/// </summary>
	private void _ToggleCursorVisibility()
	{
		m_showCursor = !m_showCursor;
	}

    /// <summary>
    /// Monobehavior Start call back.
    /// Get three type of camera's reference and set initial values.
    /// </summary>
    private void Awake()
    {
        m_firstPersonCamera = gameObject.GetComponent<FirstPersonCamera>() as IBaseCamera;
        if (m_firstPersonCamera == null)
        {
            m_firstPersonCamera = gameObject.AddComponent<FirstPersonCamera>();
        }
        m_firstPersonCamera.SetCamera(m_targetObject, Vector3.zero, 0.5f);

        m_thirdPersonCamera = gameObject.GetComponent<ThirdPersonCamera>() as IBaseCamera;
        if (m_thirdPersonCamera == null)
        {
            m_thirdPersonCamera = gameObject.AddComponent<ThirdPersonCamera>();
        }
        m_thirdPersonCamera.SetCamera(m_targetObject, new Vector3(5f, 5f, -5f), 0.5f);

        m_topDownCamera = gameObject.GetComponent<TopDownCamera>() as IBaseCamera;
        if (m_topDownCamera == null)
        {
            m_topDownCamera = gameObject.AddComponent<TopDownCamera>();
        }
        m_topDownCamera.SetCamera(m_targetObject, new Vector3(0.0f, 15.0f, 0.0f), 0.5f);

		
		if(m_startInThirdPerson)
		{
			EnableCamera(CameraType.THIRD_PERSON);
		}
		else
		{
        	EnableCamera(CameraType.FIRST_PERSON);
		}
		_SetCursorState();
    }

    /// <summary>
    /// Update current behavior. 
    /// DEBUG USE.
    /// </summary>
    private void Update()
    {
	#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            EnableCamera(CameraType.FIRST_PERSON);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            EnableCamera(CameraType.THIRD_PERSON);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            EnableCamera(CameraType.TOP_DOWN);
        }
        if (Input.GetKeyDown(KeyCode.H)) 
        {
            _ToggleCursorVisibility();
            _SetCursorState();
        }
    #endif
    }

    /// <summary>
    /// Draw buttons to swap current behavior. 
    /// DEBUG USE.
    /// </summary>
    private void OnGUI()
    {
        if (m_showBehaviorButtons)
        {
            
			if (GUI.Button(new Rect(Screen.width - Common.UI_BUTTON_SIZE_X - Common.UI_BUTTON_GAP_X, 
			                        Screen.height - ((Common.UI_BUTTON_SIZE_Y + Common.UI_LABEL_GAP_Y) * 3),
			                        Common.UI_BUTTON_SIZE_X, 
			                        Common.UI_BUTTON_SIZE_Y), "<size=20>First</size>"))
			{
				EnableCamera(CameraType.FIRST_PERSON);
			}
			if (GUI.Button(new Rect(Screen.width - Common.UI_BUTTON_SIZE_X - Common.UI_BUTTON_GAP_X, 
			                        Screen.height - ((Common.UI_BUTTON_SIZE_Y + Common.UI_LABEL_GAP_Y) * 2),
			                        Common.UI_BUTTON_SIZE_X, 
			                        Common.UI_BUTTON_SIZE_Y), "<size=20>Third</size>"))
			{
				EnableCamera(CameraType.THIRD_PERSON);
			}
			if (GUI.Button(new Rect(Screen.width - Common.UI_BUTTON_SIZE_X - Common.UI_BUTTON_GAP_X, 
			                        Screen.height - (Common.UI_BUTTON_SIZE_Y + Common.UI_LABEL_GAP_Y),
			                        Common.UI_BUTTON_SIZE_X, 
			                        Common.UI_BUTTON_SIZE_Y), "<size=20>Top</size>"))
			{
				EnableCamera(CameraType.TOP_DOWN);
			}
        }
    }
}
