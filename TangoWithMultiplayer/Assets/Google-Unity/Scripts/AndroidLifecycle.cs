//-----------------------------------------------------------------------
// <copyright file="AndroidLifecycle.cs" company="Google">
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
using UnityEngine;

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
                                                         Justification = "Legacy support.")]

/// <summary>
/// Delegate for the Android onStart event.
/// </summary>
public delegate void OnStartEventHandler();

/// <summary>
/// Delegate for the Android onStop event.
/// </summary>
public delegate void OnStopEventHandler();

/// <summary>
/// Delegate for the Android onPause event.
/// </summary>
public delegate void OnPauseEventHandler();

/// <summary>
/// Delegate for the Android onResume event.
/// </summary>
public delegate void OnResumeEventHandler();

/// <summary>
/// Delegate for the Android onActivityResult event.
/// </summary>
/// <param name="requestCode">Request code.</param>
/// <param name="resultCode">Result code.</param>
/// <param name="data">Intent data.</param>
public delegate void OnActivityResultEventHandler(int requestCode, int resultCode, AndroidJavaObject data);

/// <summary>
/// Delegate for the Android onRequestPermissionsResult event.
/// </summary>
/// <param name="requestCode">Request code.</param>
/// <param name="permissions">Permissions requested.</param>
/// <param name="grantResults">Grant result for each corresponding permission.</param>
public delegate void OnRequestPermissionsResultHandler(int requestCode, string[] permissions, AndroidPermissionGrantResult[] grantResults);

/// <summary>
/// Delegate for the Android DisplayListener interface's onDisplayChanged event.
/// </summary>
public delegate void OnDisplayChangedEventHandler();

/// <summary>
/// Enum for native Android screen rotations.
/// </summary>
public enum AndroidScreenRotation
{
    ROTATION_0 = 0,
    ROTATION_90 = 1,
    ROTATION_180 = 2,
    ROTATION_270 = 3
}

/// <summary>
/// Enum for the native Android permission grant result.
/// </summary>
public enum AndroidPermissionGrantResult
{
    GRANTED = 0,
    DENIED = -1,
}

/// <summary>
/// Binds callbacks directly to Android lifecycle.
/// </summary>
public class AndroidLifecycleCallbacks : AndroidJavaProxy
{
    /// <summary>
    /// Occurs when the Android onPause event is fired.
    /// </summary>
    private static OnStartEventHandler m_onStartEvent;

    /// <summary>
    /// Occurs when the Android onPause event is fired.
    /// </summary>
    private static OnStopEventHandler m_onStopEvent;

    /// <summary>
    /// Occurs when the Android onPause event is fired.
    /// </summary>
    private static OnPauseEventHandler m_onPauseEvent;

    /// <summary>
    /// Occurs when the Android onResume event is fired.
    /// </summary>
    private static OnResumeEventHandler m_onResumeEvent;

    /// <summary>
    /// Occurs when the Android onActivityResult event is fired.
    /// </summary>
    private static OnActivityResultEventHandler m_onActivityResultEvent;

    /// <summary>
    /// Occurs when the Android screen orientation changed.
    /// </summary>
    private static OnDisplayChangedEventHandler m_onDisplayChangedEvent;

    /// <summary>
    /// Occurs when the Android onRequestPermissionsResult event is fired.
    /// </summary>
    private static OnRequestPermissionsResultHandler m_onRequestPermissionsResultEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidLifecycleCallbacks"/> class.
    /// </summary>
    public AndroidLifecycleCallbacks() : base("com.google.unity.GoogleUnityActivity$AndroidLifecycleListener")
    {
    }

    /// <summary>
    /// Registers the on start callback to Android.
    /// </summary>
    /// <param name="onStart">On start.</param>
    public void RegisterOnStart(OnStartEventHandler onStart)
    {
        if (onStart != null)
        {
            m_onStartEvent += onStart;
        }
    }

    /// <summary>
    /// Registers the on stop callback to Android.
    /// </summary>
    /// <param name="onStop">On stop.</param>
    public void RegisterOnStop(OnStopEventHandler onStop)
    {
        if (onStop != null)
        {
            m_onStopEvent += onStop;
        }
    }

    /// <summary>
    /// Registers the on pause callback to Android.
    /// </summary>
    /// <param name="onPause">On pause.</param>
    public void RegisterOnPause(OnPauseEventHandler onPause)
    {
        if (onPause != null)
        {
            m_onPauseEvent += onPause;
        }
    }

    /// <summary>
    /// Registers the on resume callback to Android.
    /// </summary>
    /// <param name="onResume">On resume.</param>
    public void RegisterOnResume(OnResumeEventHandler onResume)
    {
        if (onResume != null)
        {
            m_onResumeEvent += onResume;
        }
    }

    /// <summary>
    /// Registers the onActivityResult callback to Android.
    /// </summary>
    /// <param name="onActivityResult">On activity result.</param>
    public void RegisterOnActivityResult(OnActivityResultEventHandler onActivityResult)
    {
        if (onActivityResult != null)
        {
            m_onActivityResultEvent += onActivityResult;
        }
    }

    /// <summary>
    /// Registers the callback to listen to display change.
    /// </summary>
    /// <param name="onDisplayChanged">On display changed event.</param>
    public void RegisterOnDisplayChanged(OnDisplayChangedEventHandler onDisplayChanged)
    {
        if (onDisplayChanged != null)
        {
            m_onDisplayChangedEvent += onDisplayChanged;
        }
    }

    /// <summary>
    /// Registers the onRequestPermissionResult callback to Android.
    /// </summary>
    /// <param name="onRequestPermissionResult">On request permissions result.</param>
    public void RegisterOnActivityResult(OnRequestPermissionsResultHandler onRequestPermissionResult)
    {
        if (onRequestPermissionResult != null)
        {
            m_onRequestPermissionsResultEvent += onRequestPermissionResult;
        }
    }

    /// <summary>
    /// Unregisters the on start callback to Android.
    /// </summary>
    /// <param name="onStart">On start.</param>
    public void UnregisterOnStart(OnStartEventHandler onStart)
    {
        if (onStart != null)
        {
            m_onStartEvent -= onStart;
        }
    }

    /// <summary>
    /// Unregisters the on stop callback to Android.
    /// </summary>
    /// <param name="onStop">On stop.</param>
    public void UnregisterOnStop(OnStopEventHandler onStop)
    {
        if (onStop != null)
        {
            m_onStopEvent -= onStop;
        }
    }

    /// <summary>
    /// Unregisters the on pause callback to Android.
    /// </summary>
    /// <param name="onPause">On pause.</param>
    public void UnregisterOnPause(OnPauseEventHandler onPause)
    {
        if (onPause != null)
        {
            m_onPauseEvent -= onPause;
        }
    }

    /// <summary>
    /// Unregisters the on resume callback to Android.
    /// </summary>
    /// <param name="onResume">On resume.</param>
    public void UnregisterOnResume(OnResumeEventHandler onResume)
    {
        if (onResume != null)
        {
            m_onResumeEvent -= onResume;
        }
    }

    /// <summary>
    /// Unregisters the onActivityResult callback to Android.
    /// </summary>
    /// <param name="onActivityResult">On activity result.</param>
    public void UnregisterOnActivityResult(OnActivityResultEventHandler onActivityResult)
    {
        if (onActivityResult != null)
        {
            m_onActivityResultEvent -= onActivityResult;
        }
    }

    /// <summary>
    /// Unregisters the OnDisplayChanged callback to Android.
    /// </summary>
    /// <param name="onDisplayChanged">On screen display changed.</param>
    public void UnregisterOnDisplayChanged(OnDisplayChangedEventHandler onDisplayChanged)
    {
        if (onDisplayChanged != null)
        {
            m_onDisplayChangedEvent -= onDisplayChanged;
        }
    }

    /// <summary>
    /// Unregisters the onRequestPermissionResult callback to Android.
    /// </summary>
    /// <param name="onRequestPermissionsResult">On request permissions result.</param>
    public void UnregisterOnRequestPermissionsResult(OnRequestPermissionsResultHandler onRequestPermissionsResult)
    {
        if (onRequestPermissionsResult != null)
        {
            m_onRequestPermissionsResultEvent -= onRequestPermissionsResult;
        }
    }

    /// <summary>
    /// Invoke the specified methodName and javaArgs.
    /// </summary>
    /// <returns>Return value to pass bath to Java.</returns>
    /// <param name="methodName">Method name.</param>
    /// <param name="javaArgs">Java arguments.</param>
    public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs)
    {
        if (methodName == "onRequestPermissionsResult")
        {
            // As of this writing, Unity versions 5.2 and up do not properly marshal Arrays from
            // Java to Unity when using the AndroidJavaProxy. Bypass that code to properly get the
            // array from Java.
            onRequestPermissionsResult(
                javaArgs[0].Call<int>("intValue", new object[0]),
                AndroidJNIHelper.ConvertFromJNIArray<string[]>(javaArgs[1].GetRawObject()),
                AndroidJNIHelper.ConvertFromJNIArray<int[]>(javaArgs[2].GetRawObject()));
            return null;
        }
        else
        {
            return base.Invoke(methodName, javaArgs);
        }
    }

    /// <summary>
    /// Implements the Android onStart.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onStart()
    {
        if (m_onStartEvent != null)
        {
            Debug.Log("Unity got the Java onStart");
            m_onStartEvent();
        }
    }

    /// <summary>
    /// Implements the Android onStop.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onStop()
    {
        if (m_onStopEvent != null)
        {
            Debug.Log("Unity got the Java onStop");
            m_onStopEvent();
        }
    }

    /// <summary>
    /// Implements the Android onPause.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onPause()
    {
        if (m_onPauseEvent != null)
        {
            Debug.Log("Unity got the Java onPause");
            m_onPauseEvent();
        }
    }

    /// <summary>
    /// Implements the Android onResume.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onResume()
    {
        if (m_onResumeEvent != null)
        {
            Debug.Log("Unity got the Java onResume");
            m_onResumeEvent();
        }
    }

    /// <summary>
    /// Implements the Android onActivityResult.
    /// </summary>
    /// <param name="requestCode">Request code.</param>
    /// <param name="resultCode">Result code.</param>
    /// <param name="data">Intent data.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
    {
        if (m_onActivityResultEvent != null)
        {
            Debug.Log("Unity got the Java onActivityResult, requestCode=" + requestCode);
            m_onActivityResultEvent(requestCode, resultCode, data);
        }
    }

    /// <summary>
    /// Implements the onDisplayChanged.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onDisplayChanged()
    {
        if (m_onDisplayChangedEvent != null)
        {
            m_onDisplayChangedEvent();
        }
    }

    /// <summary>
    /// Implements the Android onRequestPermissionsResult.
    /// </summary>
    /// <param name="requestCode">Request code.</param>
    /// <param name="permissions">Permissions requested.</param>
    /// <param name="intResults">Grant result for each corresponding permission.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onRequestPermissionsResult(
        int requestCode, string[] permissions, int[] intResults)
    {
        AndroidPermissionGrantResult[] grantResults
            = new AndroidPermissionGrantResult[intResults.Length];
        for (int it = 0; it < grantResults.Length; ++it)
        {
            grantResults[it] = (AndroidPermissionGrantResult)intResults[it];
        }

        if (m_onRequestPermissionsResultEvent != null)
        {
            Debug.Log("Unity got the Java onRequestPermissionsResult, requestCode=" + requestCode);
            m_onRequestPermissionsResultEvent(requestCode, permissions, grantResults);
        }
    }
}
