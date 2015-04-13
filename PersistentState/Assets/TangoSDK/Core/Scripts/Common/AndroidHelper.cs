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
/// Helper functions for common android functionality.
/// </summary>
using System;


public class AndroidHelper : MonoBehaviour
{
	private const string PERMISSION_REQUESTER = "com.projecttango.permissionrequester.RequestManagerActivity";
#pragma warning disable 414
	private static AndroidJavaObject m_unityActivity = null;
	private static AndroidJavaObject m_tangoHelper = null;
#pragma warning restore 414

	private static AndroidLifecycleCallbacks m_callbacks;

	/// <summary>
	/// Registers for the Android pause event.
	/// </summary>
	/// <param name="onPause">On pause.</param>
	public static void RegisterPauseEvent(OnPauseEventHandler onPause)
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(m_callbacks == null)
		{
			RegisterCallbacks();
		}

		m_callbacks.RegisterOnPause(onPause);
		#endif
	}

	/// <summary>
	/// Registers for the Android resume event.
	/// </summary>
	/// <param name="onResume">On resume.</param>
	public static void RegisterResumeEvent(OnResumeEventHandler onResume)
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(m_callbacks == null)
		{
			RegisterCallbacks();
		}
		
		m_callbacks.RegisterOnResume(onResume);
		#endif
	}

	/// <summary>
	/// Registers for the Android on activity result event.
	/// </summary>
	/// <param name="onActivityResult">On activity result.</param>
	public static void RegisterOnActivityResultEvent(OnActivityResultEventHandler onActivityResult)
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(m_callbacks == null)
		{
			RegisterCallbacks();
		}
		
		m_callbacks.RegisterOnActivityResult(onActivityResult);
		#endif
    }

	/// <summary>
	/// Inializes the AndroidJavaProxy for the Android lifecycle callbacks.
	/// </summary>
	private static void RegisterCallbacks()
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		m_callbacks = new AndroidLifecycleCallbacks();

		m_unityActivity = GetUnityActivity();
		if(m_unityActivity != null)
		{
			Debug.Log("AndroidLifecycle callback set");
			m_unityActivity.Call("attachLifecycleListener", m_callbacks);
		}
		#endif
	}

	/// <summary>
	/// Gets the unity activity.
	/// </summary>
	/// <returns>The unity activity.</returns>
	public static AndroidJavaObject GetUnityActivity()
	{
	#if UNITY_ANDROID && !UNITY_EDITOR
		if(m_unityActivity == null)
		{
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

			if(unityPlayer != null)
			{
				m_unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			}
		}
		return m_unityActivity;
	#else
		return null;
	#endif
	}

	public static AndroidJavaObject GetTangoHelperObject()
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        if(m_tangoHelper == null)
        {
		    m_tangoHelper = new AndroidJavaObject("com.projecttango.unity.TangoUnityHelper", GetUnityActivity());
        }
		return m_tangoHelper;
    #endif
        return null;
	}

	/// <summary>
	/// Gets the current application label.
	/// </summary>
	/// <returns>The current application label.</returns>
	public static string GetCurrentApplicationLabel()
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		if(unityActivity != null)
		{
			string currentPackageName = GetCurrentPackageName();
			AndroidJavaObject packageManager = unityActivity.Call<AndroidJavaObject>("getPackageManager");
			AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", currentPackageName, 0);

			if(packageInfo != null)
			{
				AndroidJavaObject applicationInfo = packageInfo.Get<AndroidJavaObject>("applicationInfo");
				AndroidJavaObject applicationLabel = packageManager.Call<AndroidJavaObject>("getApplicationLabel", applicationInfo);
				
				return applicationLabel.Call<string>("toString");
			}
		}

		return "Not Set";
	}

	/// <summary>
	/// Gets the name of the current package.
	/// </summary>
	/// <returns>The current package name.</returns>
	public static string GetCurrentPackageName()
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		if(unityActivity != null)
		{
			return unityActivity.Call<string>("getPackageName");
		}

		return "Not Set";
	}

	/// <summary>
	/// Gets the package info.
	/// </summary>
	/// <returns>The package info.</returns>
	/// <param name="packageName">Package name.</param>
	public static AndroidJavaObject GetPackageInfo(string packageName)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		if(unityActivity != null && !string.IsNullOrEmpty(packageName))
		{
			AndroidJavaObject packageManager = unityActivity.Call<AndroidJavaObject>("getPackageManager");
			AndroidJavaObject packageInfo = null;

			try
			{
				packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
			}
			catch(AndroidJavaException e)
			{
				Debug.Log("AndroidJavaException : " + e.Message);
				packageInfo = null;
			}

			return packageInfo;
		}

		return null;
	}

	public static void PerformanceLog(string message)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		if(unityActivity != null)
		{
			unityActivity.Call("logAndroidErrorMessage", message);
		}
	}

	/// <summary>
	/// Gets the name of the version.
	/// </summary>
	/// <returns>The version name.</returns>
	/// <param name="packageName">Package name.</param>
	public static string GetVersionName(string packageName)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		if(unityActivity != null && !string.IsNullOrEmpty(packageName))
		{
			AndroidJavaObject packageInfo = GetPackageInfo(packageName);

			if(packageInfo != null)
			{
				return packageInfo.Get<string>("versionName");
			}
		}

		return "Not Set";
	}

	/// <summary>
	/// Gets the version code.
	/// </summary>
	/// <returns>The version code.</returns>
	/// <param name="packageName">Package name.</param>
	public static int GetVersionCode(string packageName)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		if(unityActivity != null && !string.IsNullOrEmpty(packageName))
		{
			AndroidJavaObject packageInfo = GetPackageInfo(packageName);
			
			if(packageInfo != null)
			{		
				return packageInfo.Get<int>("versionCode");
			}
		}

		return -1;
	}

	/// <summary>
	/// Starts the activity for the provided class name.
	/// </summary>
	/// <param name="className">Class name.</param>
	public static void StartActivity(string className)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();

		if(unityActivity != null)
		{
			string packageName = GetCurrentPackageName();

			AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

			intentObject.Call<AndroidJavaObject>("setClassName", packageName, className);

			unityActivity.Call("startActivity", intentObject);
		}
	}

	/// <summary>
	/// Starts the tango permissions activity of the provided type.
	/// </summary>
	/// <param name="permissionsType">Permissions type.</param>
	public static void StartTangoPermissionsActivity(string permissionsType)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		
		if(unityActivity != null)
		{
			int requestCode = 0;
			string[] args = new string[1];

			if(permissionsType == Tango.Common.TANGO_MOTION_TRACKING_PERMISSIONS)
			{
				requestCode = Tango.Common.TANGO_MOTION_TRACKING_PERMISSIONS_REQUEST_CODE;
				args[0] = "PERMISSIONTYPE:" + Tango.Common.TANGO_MOTION_TRACKING_PERMISSIONS;
			}
			else if(permissionsType == Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS)
			{
				requestCode = Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE;
				args[0] = "PERMISSIONTYPE:" + Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS;
			}

			if(requestCode != 0)
			{
				unityActivity.Call("LaunchIntent", "com.projecttango.tango", "com.google.atap.tango.RequestPermissionActivity", args, requestCode);
			}
			else
			{
				Debug.Log("Invalid permission request");
			}
		}
	}

	public static void ParseTangoEvent(double timestamp, int eventType, string key, string value)
	{
		AndroidJavaObject tangoObject = GetTangoHelperObject();
		if(tangoObject != null)
		{
			tangoObject.Call("showTangoEvent", timestamp, eventType, key, value);
		}
	}

	public static void ParseTangoPoseStatus(int poseStatus)
	{
		AndroidJavaObject tangoObject = GetTangoHelperObject();
		if(tangoObject != null)
		{
			tangoObject.Call("showTangoPoseStatus", poseStatus);
		}
	}

	/// <summary>
	/// Determines if is tango core present.
	/// </summary>
	/// <returns><c>true</c> if is tango core present; otherwise, <c>false</c>.</returns>
	public static bool IsTangoCorePresent()
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		
		if(unityActivity != null)
		{
			if(GetPackageInfo("com.projecttango.tango") != null)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Determines if the application has Tango permissions.
	/// </summary>
	/// <returns><c>true</c> if application has tango permissions; otherwise, <c>false</c>.</returns>
	public static bool ApplicationHasTangoPermissions(string permissionType)
	{
		AndroidJavaObject tangoObject = GetTangoHelperObject();
		if(tangoObject != null)
		{
			return tangoObject.Call<bool>("hasPermission", permissionType);
        }
        
        return false;
	}

	/// <summary>
	/// Shows the android toast message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="callFinish">If set to <c>true</c> call finish on the unity activity.</param>
	public static void ShowAndroidToastMessage(string message, bool callFinish)
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		
		if(unityActivity != null)
		{
			AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");

			unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
				AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0x00000001);
				toastObject.Call("show");
			}));
        }

		if(callFinish)
		{
			AndroidFinish();
		}
	}

	/// <summary>
	/// Shows the standard tango exceptions UI.
	/// </summary>
	public static void ShowStandardTangoExceptionsUI()
	{
		AndroidJavaObject tangoObject = GetTangoHelperObject();
		if(tangoObject != null)
		{
			Debug.Log("Show UX exceptions");
			tangoObject.Call("enableTangoExceptions");
		}
	}

    /// <summary>
    /// Finds the tango exceptions user interface layout.
    /// </summary>
    public static bool FindTangoExceptionsUILayout()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            Debug.Log("Find UX exceptions layout");
            return tangoObject.Call<bool>("findExceptionsLayout");
        }
        return false;
    }

	/// <summary>
	/// Sets the tango exceptions listener.
	/// </summary>
	public static void SetTangoExceptionsListener()
	{
		AndroidJavaObject tangoObject = GetTangoHelperObject();
		if(tangoObject != null)
		{
			Debug.Log("Setting UX callbacks");
			tangoObject.Call("setTangoExceptionsListener", UxExceptionListener.GetInstance);
		}
	}

	/// <summary>
	/// Calls finish on the Unity Activity.
	/// </summary>
	public static void AndroidFinish()
	{
		AndroidJavaObject unityActivity = GetUnityActivity();
		
		if(unityActivity != null)
		{
			unityActivity.Call("finish");
		}
	}
	
	/// <summary>
	/// Calls quit on the Unity Activity.
	/// </summary>
	public static void AndroidQuit()
	{
		AndroidJavaClass system = new AndroidJavaClass("java.lang.System");
		system.CallStatic("exit", 0);
    }
}
