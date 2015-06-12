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
using System.Collections;

public delegate void OnPauseEventHandler();
public delegate void OnResumeEventHandler();
public delegate void OnActivityResultEventHandler(int requestCode, int resultCode, AndroidJavaObject data);

/// <summary>
/// Binds callbacks directly to Android lifecycle.
/// </summary>
public class AndroidLifecycleCallbacks : AndroidJavaProxy 
{	
	public AndroidLifecycleCallbacks() : base("com.google.unity.GoogleUnityActivity$AndroidLifecycleListener"){}
	
	private static event OnPauseEventHandler m_onPause;
	private static event OnResumeEventHandler m_onResume;
	private static event OnActivityResultEventHandler m_onActivityResult;

	/// <summary>
	/// Registers the on pause callback to Android.
	/// </summary>
	/// <param name="onPause">On pause.</param>
	public void RegisterOnPause(OnPauseEventHandler onPause)
	{
		if(onPause != null)
		{
			m_onPause += onPause;
		}
	}

	/// <summary>
	/// Registers the on resume callback to Android.
	/// </summary>
	/// <param name="onResume">On resume.</param>
	public void RegisterOnResume(OnResumeEventHandler onResume)
	{
		if(onResume != null)
		{
			m_onResume += onResume;
		}
	}

	/// <summary>
	/// Registers the on onActivityResult callback to Android.
	/// </summary>
	/// <param name="onActivityResult">On activity result.</param>
	public void RegisterOnActivityResult(OnActivityResultEventHandler onActivityResult)
	{
		if(onActivityResult != null)
		{
			m_onActivityResult += onActivityResult;
		}
	}

	/// <summary>
	/// Unregisters the on pause callback to Android.
	/// </summary>
	/// <param name="onPause">On pause.</param>
	public void UnregisterOnPause(OnPauseEventHandler onPause)
	{
		if(onPause != null)
		{
			m_onPause -= onPause;
		}
	}

	/// <summary>
	/// Unregisters the on resume callback to Android.
	/// </summary>
	/// <param name="onResume">On resume.</param>
	public void UnregisterOnResume(OnResumeEventHandler onResume)
	{
		if(onResume != null)
		{
			m_onResume -= onResume;
		}
	}

	/// <summary>
	/// Unregisters the on onActivityResult callback to Android.
	/// </summary>
	/// <param name="onActivityResult">On activity result.</param>
	public void UnregisterOnActivityResult(OnActivityResultEventHandler onActivityResult)
	{
		if(onActivityResult != null)
		{
			m_onActivityResult -= onActivityResult;
		}
	}

	/// <summary>
	/// Implements the Android onPause.
	/// </summary>
	protected void onPause()
	{
		if(m_onPause != null)
		{
			Debug.Log("Unity got the Java onPause");
			m_onPause();
		}
	}

	/// <summary>
	/// Implements the Android onResume.
	/// </summary>
	protected void onResume()
	{
		if(m_onResume != null)
		{
			Debug.Log("Unity got the Java onResume");
			m_onResume();
		}
	}

	/// <summary>
	/// Implements the Android onActivityResult.
	/// </summary>
	/// <param name="requestCode">Request code.</param>
	/// <param name="resultCode">Result code.</param>
	/// <param name="data">Data.</param>
	protected void onActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
	{
		if(m_onActivityResult != null)
		{
			Debug.Log("Unity got the Java onActivityResult");
			m_onActivityResult(requestCode, resultCode, data);
		}
	}
}
