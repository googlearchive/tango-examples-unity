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

/// <summary>
/// About screen to display information about an Android application.
/// </summary>
public class AboutScreen : MonoBehaviour
{
	public Texture2D m_backgroundTexture;
	public bool m_showLinkToChangeList = false;

	private const string APPLICATION_INFORMATION = "<size=20>{0}\nVersion = {1}\nLibrary={2}\n\n\n</size>";

	private const float BUTTON_X_POSITION_RIGHT_ALIGNED = 0.995f;
	private const float BUTTON_Y_POSITION_RIGHT_ALIGNED = 0.005f;
	private const float BUTTON_WIDTH = 150.0f;
	private const float BUTTON_HEIGHT = 75.0f;

	private const float ABOUT_SCREEN_WIDTH = 1350;
	private const float ABOUT_SCREEN_HEIGHT = 100;

	private Rect m_aboutScreenRect;
	private Rect m_buttonRect;
	private bool m_isActive;

	private string m_applicationName = string.Empty;
	private string m_applicationVersion = string.Empty;

	// Use this for initialization
	void Start () 
	{
		m_isActive = false;

		m_buttonRect = new Rect((Screen.width * BUTTON_X_POSITION_RIGHT_ALIGNED) - BUTTON_WIDTH,
		                        Screen.height * BUTTON_Y_POSITION_RIGHT_ALIGNED,
		                        BUTTON_WIDTH,
		                        BUTTON_HEIGHT);

		m_aboutScreenRect = new Rect((Screen.width * 0.5f) - (ABOUT_SCREEN_WIDTH * 0.5f),
		                             (Screen.height * 0.5f) - (ABOUT_SCREEN_HEIGHT * 0.5f),
		                             ABOUT_SCREEN_WIDTH,
		                             m_showLinkToChangeList ? ABOUT_SCREEN_HEIGHT * 3.0f : ABOUT_SCREEN_HEIGHT);

		m_applicationName = AndroidHelper.GetCurrentApplicationLabel();
		m_applicationVersion = AndroidHelper.GetVersionName(AndroidHelper.GetCurrentPackageName());
	}

	private void OnGUI()
	{
		if(m_isActive) // Draw "about" screen
		{
			_AboutWindow();
		}
		else  // Draw "about" button
		{
			if(GUI.Button(m_buttonRect, "<size=15>About</size>"))
			{
				m_isActive = true;
			}
		}
	}
	
	/// <summary>
	/// Perform updates for the next frame after the
	/// normal update has run. This loads next frame.
	/// </summary>
	private void LateUpdate()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if(m_isActive)
			{
				m_isActive = false;
			}
			else
			{
				TangoApplication application = FindObjectOfType<TangoApplication>();
				if(application != null)
				{
					application.Shutdown();
				}
				Application.Quit();
			}
		}
	}

	private void _AboutWindow()
	{
		if(m_backgroundTexture != null)
		{
			GUI.DrawTexture(m_aboutScreenRect, m_backgroundTexture, ScaleMode.StretchToFill);
		}

		GUILayout.BeginArea(m_aboutScreenRect);

		Color oldBackgroundColor = GUI.backgroundColor;
		Color oldColor = GUI.color;

		GUI.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
		GUI.color = Color.gray;

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("<size=20>" + m_applicationName + "</size>");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("<size=20>Version = " + m_applicationVersion + "</size>");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("<size=20>Library = " + Tango.TangoApplication.GetTangoServiceVersion() + "</size>");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		if(m_showLinkToChangeList)
		{
			GUILayout.Space(40);

			GUI.color = Color.blue;
			if(GUILayout.Button("<size=20>Release Notes</size>"))
			{
				//m_isActive = false;

	#if !UNITY_EDITOR && UNITY_ANDROID
				string packageName = AndroidHelper.GetCurrentPackageName();
				Application.OpenURL ("market://details?q=pname:" + packageName + "/");
	#else
				Application.OpenURL("http://play.google.com");
	#endif
			}
		}

		GUI.color = oldColor;
		GUI.backgroundColor = oldBackgroundColor;

		GUILayout.EndArea();
	}
}
