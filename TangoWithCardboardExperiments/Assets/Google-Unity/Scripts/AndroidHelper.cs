//-----------------------------------------------------------------------
// <copyright file="AndroidHelper.cs" company="Google">
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

#if UNITY_ANDROID && !UNITY_EDITOR
#define ANDROID_DEVICE
#endif

using System;
using System.Collections;
using UnityEngine;

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
    Justification = "Files can start with an interface that has a different name.")]

/// <summary>
/// Instance wrapper interface for static functionality of AndroidHelper.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
internal interface IAndroidHelperWrapper
{
    bool CheckPermission(string permissionType);

    bool ApplicationHasTangoPermissions(string permissionType);

    void StartTangoPermissionsActivity(string permissionType);

    void RequestPermission(string permissionType, int requestCode);

    bool BindTangoService();

    Tango.OrientationManager.Rotation GetDisplayRotation();

    Tango.OrientationManager.Rotation GetColorCameraRotation();

    int TangoSetBinder(AndroidJavaObject binder);

    void RegisterPauseEvent(OnPauseEventHandler handler);

    void RegisterResumeEvent(OnResumeEventHandler handler);

    void RegisterOnActivityResultEvent(OnActivityResultEventHandler handler);

    void RegisterOnDisplayChangedEvent(global::OnDisplayChangedEventHandler handler);

    void RegisterOnTangoServiceConnected(AndroidHelper.OnTangoServiceConnected handler);

    void RegisterOnTangoServiceDisconnected(AndroidHelper.OnTangoServiceDisconnected handler);

    void RegisterOnRequestPermissionsResultEvent(OnRequestPermissionsResultHandler handler);

    void UnregisterPauseEvent(OnPauseEventHandler handler);

    void UnregisterResumeEvent(OnResumeEventHandler handler);

    void UnregisterOnActivityResultEvent(OnActivityResultEventHandler handler);

    void UnregisterOnDisplayChangedEvent(global::OnDisplayChangedEventHandler handler);

    bool IsRunningOnAndroid();
}

/// <summary>
/// Helper functions for common android functionality.
/// </summary>
public partial class AndroidHelper : MonoBehaviour
{
#pragma warning disable 414
    private static AndroidJavaObject m_unityActivity = null;
#pragma warning restore 414

    private static AndroidLifecycleCallbacks m_callbacks;

    /// <summary>
    /// The display time length of Android Toast.
    /// </summary>
    public enum ToastLength
    {
        SHORT = 0x00000000,
        LONG = 0x00000001
    }

    /// <summary>
    /// Registers for the Android start event.
    /// </summary>
    /// <param name="onStart">On start.</param>
   public static void RegisterStartEvent(OnStartEventHandler onStart)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
        }

        m_callbacks.RegisterOnStart(onStart);
        #endif
    }

    /// <summary>
    /// Registers for the Android stop event.
    /// </summary>
    /// <param name="onStop">On stop.</param>
    public static void RegisterStopEvent(OnStopEventHandler onStop)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
        }

        m_callbacks.RegisterOnStop(onStop);
        #endif
    }

    /// <summary>
    /// Registers for the Android pause event.
    /// </summary>
    /// <param name="onPause">On pause.</param>
    public static void RegisterPauseEvent(OnPauseEventHandler onPause)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
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
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
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
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
        }

        m_callbacks.RegisterOnActivityResult(onActivityResult);
        #endif
    }

    /// <summary>
    /// Registers for on orientation change event.
    /// </summary>
    /// <param name="onChanged">Delegate to call when the screen orientation changes.</param>
    public static void RegisterOnDisplayChangedEvent(OnDisplayChangedEventHandler onChanged)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
        }

        m_callbacks.RegisterOnDisplayChanged(onChanged);
        #endif
    }

    /// <summary>
    /// Registers for the Android on request permissions result event.
    /// </summary>
    /// <param name="onRequestPermissionsResult">On request permissions result.</param>
    public static void RegisterOnRequestPermissionsResultEvent(
        OnRequestPermissionsResultHandler onRequestPermissionsResult)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            _RegisterCallbacks();
        }

        m_callbacks.RegisterOnActivityResult(onRequestPermissionsResult);
        #endif
    }

    /// <summary>
    /// Unregisters for the Android start event.
    /// </summary>
    /// <param name="onStart">On start.</param>
    public static void UnregisterStartEvent(OnStartEventHandler onStart)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnStart(onStart);
        #endif
    }

    /// <summary>
    /// Unregisters for the Android stop event.
    /// </summary>
    /// <param name="onStop">On stop.</param>
    public static void UnregisterStopEvent(OnStopEventHandler onStop)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnStop(onStop);
        #endif
    }

    /// <summary>
    /// Unregisters for the Android pause event.
    /// </summary>
    /// <param name="onPause">On pause.</param>
    public static void UnregisterPauseEvent(OnPauseEventHandler onPause)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnPause(onPause);
        #endif
    }

    /// <summary>
    /// Unregisters for the Android resume event.
    /// </summary>
    /// <param name="onResume">On resume.</param>
    public static void UnregisterResumeEvent(OnResumeEventHandler onResume)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnResume(onResume);
        #endif
    }

    /// <summary>
    /// Unregisters for the Android on activity result event.
    /// </summary>
    /// <param name="onActivityResult">On activity result.</param>
    public static void UnregisterOnActivityResultEvent(OnActivityResultEventHandler onActivityResult)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnActivityResult(onActivityResult);
        #endif
    }

    /// <summary>
    /// Unregisters for on orientation change event.
    /// </summary>
    /// <param name="onChanged">Delegate to call when the screen orientation changes.</param>
    public static void UnregisterOnDisplayChangedEvent(OnDisplayChangedEventHandler onChanged)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnDisplayChanged(onChanged);
        #endif
    }

    /// <summary>
    /// Unregisters for the Android on request permissions result event.
    /// </summary>
    /// <param name="onRequestPermissionsResult">On request permissions result.</param>
    public static void UnregisterOnActivityResultEvent(OnRequestPermissionsResultHandler onRequestPermissionsResult)
    {
        #if ANDROID_DEVICE
        if (m_callbacks == null)
        {
            return;
        }

        m_callbacks.UnregisterOnRequestPermissionsResult(onRequestPermissionsResult);
        #endif
    }

    /// <summary>
    /// Gets the unity activity.
    /// </summary>
    /// <returns>The unity activity.</returns>
    public static AndroidJavaObject GetUnityActivity()
    {
        #if ANDROID_DEVICE
        if (m_unityActivity == null)
        {
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                m_unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
                m_unityActivity = null;
            }
        }

        return m_unityActivity;
        #else
        return null;
        #endif
    }

    /// <summary>
    /// Gets the current application label.
    /// </summary>
    /// <returns>The current application label.</returns>
    public static string GetCurrentApplicationLabel()
    {
        string applicationLabelName = "Not Set";
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                string currentPackageName = GetCurrentPackageName();
                AndroidJavaObject packageManager = unityActivity.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", currentPackageName, 0);
                AndroidJavaObject applicationInfo = packageInfo.Get<AndroidJavaObject>("applicationInfo");
                AndroidJavaObject applicationLabel = packageManager.Call<AndroidJavaObject>("getApplicationLabel", applicationInfo);
                applicationLabelName = applicationLabel.Call<string>("toString");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
                applicationLabelName = string.Empty;
            }
        }

        return applicationLabelName;
    }

    /// <summary>
    /// Gets the name of the current package.
    /// </summary>
    /// <returns>The current package name.</returns>
    public static string GetCurrentPackageName()
    {
        string packageName = "Not Set";
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                packageName = unityActivity.Call<string>("getPackageName");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }

        return packageName;
    }

    /// <summary>
    /// Gets the package info.
    /// </summary>
    /// <returns>The package info.</returns>
    /// <param name="packageName">Package name.</param>
    public static AndroidJavaObject GetPackageInfo(string packageName)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();
        if (unityActivity != null && !string.IsNullOrEmpty(packageName))
        {
            AndroidJavaObject packageManager = unityActivity.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject packageInfo = null;

            try
            {
                packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
                packageInfo = null;
            }

            return packageInfo;
        }

        return null;
    }

    /// <summary>
    /// Used for performance logging from the Android side.
    /// </summary>
    /// <param name="message">Message string to log.</param>
    public static void PerformanceLog(string message)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();
        if (unityActivity != null)
        {
            try
            {
                unityActivity.Call("logAndroidErrorMessage", message);
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }
    }

    /// <summary>
    /// Gets the name of the version.
    /// </summary>
    /// <returns>The version name.</returns>
    /// <param name="packageName">Package name.</param>
    public static string GetVersionName(string packageName)
    {
        string versionName = "Not Set";
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null && !string.IsNullOrEmpty(packageName))
        {
            try
            {
                AndroidJavaObject packageInfo = GetPackageInfo(packageName);
                versionName = packageInfo.Get<string>("versionName");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
                versionName = string.Empty;
            }
        }

        return versionName;
    }

    /// <summary>
    /// Gets the version code.
    /// </summary>
    /// <returns>The version code.</returns>
    /// <param name="packageName">Package name.</param>
    public static int GetVersionCode(string packageName)
    {
        int versionCode = -1;
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null && !string.IsNullOrEmpty(packageName))
        {
            try
            {
                AndroidJavaObject packageInfo = GetPackageInfo(packageName);
                versionCode = packageInfo.Get<int>("versionCode");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
                versionCode = -1;
            }
        }

        return versionCode;
    }

    /// <summary>
    /// Starts the activity for the provided class name.
    /// </summary>
    /// <param name="className">Class name.</param>
    public static void StartActivity(string className)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                string packageName = GetCurrentPackageName();
                AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
                intentObject.Call<AndroidJavaObject>("setClassName", packageName, className);
                unityActivity.Call("startActivity", intentObject);
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }
    }

    /// DEPRECATED: Use the other two ShowAndroidToastMessage funcitons instead.
    /// <summary>
    /// Shows the android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    /// <param name="callFinish">If set to <c>true</c> call finish on the unity activity.</param>
    public static void ShowAndroidToastMessage(string message, bool callFinish)
    {
        ShowAndroidToastMessage(message);

        if (callFinish)
        {
            AndroidFinish();
        }
    }

    /// <summary>
    /// Shows the android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    public static void ShowAndroidToastMessage(string message)
    {
        _ShowAndroidToastMessage(message, ToastLength.LONG);
    }

    /// <summary>
    /// Shows the android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    /// <param name="length">Toast message time length.</param>
    public static void ShowAndroidToastMessage(string message, ToastLength length)
    {
        _ShowAndroidToastMessage(message, length);
    }

    /// <summary>
    /// Calls finish on the Unity Activity.
    /// </summary>
    public static void AndroidFinish()
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                unityActivity.Call("finish");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }
    }

    /// <summary>
    /// Calls quit on the Unity Activity.
    /// </summary>
    public static void AndroidQuit()
    {
        #if ANDROID_DEVICE
        try
        {
            AndroidJavaClass system = new AndroidJavaClass("java.lang.System");
            system.CallStatic("exit", 0);
        }
        catch (AndroidJavaException e)
        {
            Debug.Log("AndroidJavaException : " + e.Message);
        }
        #endif
    }

    /// <summary>
    /// Check if an Android permission is granted.
    /// </summary>
    /// <returns><c>true</c>, if permission is granted, <c>false</c> otherwise.</returns>
    /// <param name="permission">Android permission to check.</param>
    public static bool CheckPermission(string permission)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                return unityActivity.Call<bool>("checkAndroidPermission", permission);
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }

        return false;
    }

    /// <summary>
    /// Request an Android permission.
    ///
    /// The result of the request is reported to a delegate registered via
    /// RegisterOnRequestPermissionsResultEvent.
    /// </summary>
    /// <param name="permission">Permission to request.</param>
    /// <param name="requestCode">
    /// Request code passed to RegisterOnRequestPermissionsResultEvent.
    /// </param>
    public static void RequestPermission(string permission, int requestCode)
    {
        RequestPermissions(new string[] { permission }, requestCode);
    }

    /// <summary>
    /// Request multiple Android permissions.
    ///
    /// The result of the request is reported to a delegate registered via
    /// RegisterOnRequestPermissionsResultEvent.
    /// </summary>
    /// <param name="permissions">Permissions to request.</param>
    /// <param name="requestCode">
    /// Request code passed to RegisterOnRequestPermissionsResultEvent.
    /// </param>
    public static void RequestPermissions(string[] permissions, int requestCode)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                unityActivity.Call("requestAndroidPermissions", permissions, requestCode);
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }
    }

    /// <summary>
    /// Check if the App should show rationale for requesting a permission.
    ///
    /// Corresponds to ActivityCompat.shouldShowRequestPermissionRationale.
    /// </summary>
    /// <returns><c>true</c>, if the app should show rationale, <c>false</c> otherwise.</returns>
    /// <param name="permission">Permission to show rationale for.</param>
    public static bool ShouldShouldRequestPermissionRationale(string permission)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                return unityActivity.Call<bool>("shouldShowRequestAndroidPermissionRationale", permission);
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }

        return false;
    }

    /// <summary>
    /// Launch this application's detailed settings.
    ///
    /// This is useful to provide the user a way to turn settings back on if they have denied a
    /// critical permission and asked to not be notified again.
    /// </summary>
    public static void LaunchApplicationDetailsSettings()
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                unityActivity.Call("launchApplicationDetailsSettings");
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }
    }

    /// <summary>
    /// Shows the android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    /// <param name="length">Toast message time length.</param>
    private static void _ShowAndroidToastMessage(string message, ToastLength length)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            try
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, (int)length);
                    toastObject.Call("show");
                }));
            }
            catch (AndroidJavaException e)
            {
                Debug.Log("AndroidJavaException : " + e.Message);
            }
        }
    }

    /// <summary>
    /// Initializes the AndroidJavaProxy for the Android lifecycle callbacks.
    /// </summary>
    private static void _RegisterCallbacks()
    {
        #if ANDROID_DEVICE
        m_callbacks = new AndroidLifecycleCallbacks();

        m_unityActivity = GetUnityActivity();
        if (m_unityActivity != null)
        {
            Debug.Log("AndroidLifecycle callback set");
            m_unityActivity.Call("attachLifecycleListener", m_callbacks);
        }
        #endif
    }
}

/// <summary>
/// Instance wrapper for static functionality of AndroidHelper.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
internal class AndroidHelperWrapper : IAndroidHelperWrapper
{
    private static AndroidHelperWrapper m_instance;

    public static AndroidHelperWrapper Instance
    {
        get
        {
            return m_instance != null ? m_instance : m_instance = new AndroidHelperWrapper();
        }
    }

    public bool CheckPermission(string permission)
    {
        return AndroidHelper.CheckPermission(permission);
    }

    public bool ApplicationHasTangoPermissions(string permissionType)
    {
        return AndroidHelper.ApplicationHasTangoPermissions(permissionType);
    }

    public void StartTangoPermissionsActivity(string permissionType)
    {
        AndroidHelper.StartTangoPermissionsActivity(permissionType);
    }

    public void RequestPermission(string permissionType, int requestCode)
    {
        AndroidHelper.RequestPermission(permissionType, requestCode);
    }

    public bool BindTangoService()
    {
        return AndroidHelper.BindTangoService();
    }

    public Tango.OrientationManager.Rotation GetDisplayRotation()
    {
        return AndroidHelper.GetDisplayRotation();
    }

    public Tango.OrientationManager.Rotation GetColorCameraRotation()
    {
        return AndroidHelper.GetColorCameraRotation();
    }

    public int TangoSetBinder(AndroidJavaObject binder)
    {
        return AndroidHelper.TangoSetBinder(binder);
    }

    public void RegisterPauseEvent(OnPauseEventHandler handler)
    {
        AndroidHelper.RegisterPauseEvent(handler);
    }

    public void RegisterResumeEvent(OnResumeEventHandler handler)
    {
        AndroidHelper.RegisterResumeEvent(handler);
    }

    public void RegisterOnActivityResultEvent(OnActivityResultEventHandler handler)
    {
        AndroidHelper.RegisterOnActivityResultEvent(handler);
    }

    public void RegisterOnDisplayChangedEvent(global::OnDisplayChangedEventHandler handler)
    {
        AndroidHelper.RegisterOnDisplayChangedEvent(handler);
    }

    public void RegisterOnTangoServiceConnected(AndroidHelper.OnTangoServiceConnected handler)
    {
        AndroidHelper.RegisterOnTangoServiceConnected(handler);
    }

    public void RegisterOnTangoServiceDisconnected(AndroidHelper.OnTangoServiceDisconnected handler)
    {
        AndroidHelper.RegisterOnTangoServiceDisconnected(handler);
    }

    public void RegisterOnRequestPermissionsResultEvent(OnRequestPermissionsResultHandler handler)
    {
        AndroidHelper.RegisterOnRequestPermissionsResultEvent(handler);
    }

    public void UnregisterPauseEvent(OnPauseEventHandler handler)
    {
        AndroidHelper.UnregisterPauseEvent(handler);
    }

    public void UnregisterResumeEvent(OnResumeEventHandler handler)
    {
        AndroidHelper.UnregisterResumeEvent(handler);
    }

    public void UnregisterOnActivityResultEvent(OnActivityResultEventHandler handler)
    {
        AndroidHelper.UnregisterOnActivityResultEvent(handler);
    }

    public void UnregisterOnDisplayChangedEvent(global::OnDisplayChangedEventHandler handler)
    {
        AndroidHelper.UnregisterOnDisplayChangedEvent(handler);
    }

    public bool IsRunningOnAndroid()
    {
        return Application.platform == RuntimePlatform.Android;
    }
}
