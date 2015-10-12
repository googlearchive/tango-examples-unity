//-----------------------------------------------------------------------
// <copyright file="AndroidLifecycle.cs" company="Google">
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
/// <param name="data">Data.</param>
public delegate void OnActivityResultEventHandler(int requestCode, int resultCode, AndroidJavaObject data);

/// <summary>
/// Binds callbacks directly to Android lifecycle.
/// </summary>
public class AndroidLifecycleCallbacks : AndroidJavaProxy 
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidLifecycleCallbacks"/> class.
    /// </summary>
    public AndroidLifecycleCallbacks() : base("com.google.unity.GoogleUnityActivity$AndroidLifecycleListener")
    {
    }

    /// <summary>
    /// Occurs when the Android onPause event is fired.
    /// </summary>
    private static event OnPauseEventHandler OnPauseEvent;

    /// <summary>
    /// Occurs when the Android onResume event is fired.
    /// </summary>
    private static event OnResumeEventHandler OnResumeEvent;

    /// <summary>
    /// Occurs when the Android onActivityResult event is fired.
    /// </summary>
    private static event OnActivityResultEventHandler OnActivityResultEvent;

    /// <summary>
    /// Registers the on pause callback to Android.
    /// </summary>
    /// <param name="onPause">On pause.</param>
    public void RegisterOnPause(OnPauseEventHandler onPause)
    {
        if (onPause != null)
        {
            OnPauseEvent += onPause;
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
            OnResumeEvent += onResume;
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
            OnActivityResultEvent += onActivityResult;
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
            OnPauseEvent -= onPause;
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
            OnResumeEvent -= onResume;
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
            OnActivityResultEvent -= onActivityResult;
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
        if (OnPauseEvent != null)
        {
            Debug.Log("Unity got the Java onPause");
            OnPauseEvent();
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
        if (OnResumeEvent != null)
        {
            Debug.Log("Unity got the Java onResume");
            OnResumeEvent();
        }
    }

    /// <summary>
    /// Implements the Android onActivityResult.
    /// </summary>
    /// <param name="requestCode">Request code.</param>
    /// <param name="resultCode">Result code.</param>
    /// <param name="data">Data.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Android API.")]
    protected void onActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
    {
        if (OnActivityResultEvent != null)
        {
            Debug.Log("Unity got the Java onActivityResult");
            OnActivityResultEvent(requestCode, resultCode, data);
        }
    }
}
