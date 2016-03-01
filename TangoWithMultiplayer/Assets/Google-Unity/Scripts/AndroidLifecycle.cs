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
/// Delegate for the Android screen orientation changed.
/// </summary>
/// <param name="newOrientation">The index of new orientation.</param>
public delegate void OnScreenOrientationChangedEventHandler(AndroidScreenRotation newOrientation);

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
/// Binds callbacks directly to Android lifecycle.
/// </summary>
public class AndroidLifecycleCallbacks : AndroidJavaProxy 
{
    /// <summary>
    /// Occurs when the Android onPause event is fired.
    /// </summary>
    private static OnPauseEventHandler m_onPuaseEvent;

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
    private static OnScreenOrientationChangedEventHandler m_onScreenOrientationChangedEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidLifecycleCallbacks"/> class.
    /// </summary>
    public AndroidLifecycleCallbacks() : base("com.google.unity.GoogleUnityActivity$AndroidLifecycleListener")
    {
    }

    /// <summary>
    /// Registers the on pause callback to Android.
    /// </summary>
    /// <param name="onPause">On pause.</param>
    public void RegisterOnPause(OnPauseEventHandler onPause)
    {
        if (onPause != null)
        {
            m_onPuaseEvent += onPause;
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
    /// Registers the on onActivityResult callback to Android.
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
    /// Registers the callback to listen to screen orientation change.
    /// </summary>
    /// <param name="onOrientationChanged">On screen orientation changed.</param>
    public void RegisterOnScreenOrientationChanged(OnScreenOrientationChangedEventHandler onOrientationChanged)
    {
        if (onOrientationChanged != null)
        {
            m_onScreenOrientationChangedEvent += onOrientationChanged;
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
            m_onPuaseEvent -= onPause;
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
    /// Unregisters the on onActivityResult callback to Android.
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
    /// Unregisters the on OnScreenOrientationChanged callback to Android.
    /// </summary>
    /// <param name="onOrientationChanged">On screen orientation changed.</param>
    public void UnregisterOnScreenOrientationChanged(OnScreenOrientationChangedEventHandler onOrientationChanged)
    {
        if (onOrientationChanged != null)
        {
            m_onScreenOrientationChangedEvent -= onOrientationChanged;
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
        if (m_onPuaseEvent != null)
        {
            Debug.Log("Unity got the Java onPause");
            m_onPuaseEvent();
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
            Debug.Log("Unity got the Java onActivityResult");
            m_onActivityResultEvent(requestCode, resultCode, data);
        }
    }

    /// <summary>
    /// Implements the onScreenRotationChanged.
    /// </summary>
    /// <param name="newOrientation">New screen orientation.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onScreenRotationChanged(AndroidScreenRotation newOrientation)
    {
        if (m_onScreenOrientationChangedEvent != null)
        {
            m_onScreenOrientationChangedEvent(newOrientation);
        }
    }
}
