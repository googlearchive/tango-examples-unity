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
using System.Collections;
using UnityEngine;

/// <summary>
/// Miscellaneous Android related utilities provided by the Tango UX SDK.
/// </summary>
public partial class AndroidHelper
{
    #pragma warning disable 414
    private static AndroidJavaObject m_tangoUxHelper = null;
    #pragma warning restore 414
    private static System.IntPtr m_tangoUxHelper_class;
    private static System.IntPtr m_tangoUxHelper_obj;
    private static System.IntPtr m_tangoUxHelper_processPoseDataStatus;
    [System.ThreadStatic] private static jvalue[] val;
    [System.ThreadStatic] private static int jniAttached; 
    
    /// <summary>
    /// Gets the Java tango helper object.
    /// </summary>
    /// <returns>The tango helper object.</returns>
    public static AndroidJavaObject GetTangoUxHelperObject()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if(m_tangoUxHelper == null)
        {
            m_tangoUxHelper = new AndroidJavaObject("com.projecttango.unityuxhelper.TangoUnityUxHelper", GetUnityActivity());
            m_tangoUxHelper_class = m_tangoUxHelper.GetRawClass();
            m_tangoUxHelper_obj = m_tangoUxHelper.GetRawObject();
        }
        return m_tangoUxHelper;
        #else
        return null;
        #endif
    }

    /// <summary>
    /// Parses the tango event.
    /// </summary>
    /// <param name="timestamp">Timestamp of the event.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="key">Event key.</param>
    /// <param name="value">Event value.</param>
    public static void ParseTangoEvent(double timestamp, int eventType, string key, string value)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("processTangoEvent", timestamp, eventType, key, value);
        }
    }

    /// <summary>
    /// Parses the tango pose status.
    /// </summary>
    /// <param name="poseStatus">Pose status.</param>
    public static void ParseTangoPoseStatus(int poseStatus)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            // Replaced with the call to JNI to reduce memory allocations
            // tangoUxObject.Call("processPoseDataStatus", poseStatus);
            if (jniAttached != 1)
            {
               if (AndroidJNI.AttachCurrentThread() == 0)
               {
                 jniAttached = 1;
               }
            }
            
            if (jniAttached == 1)
            {
                if (val == null)
                {
                    val = new jvalue[1];
                }      
                
                m_tangoUxHelper_processPoseDataStatus = AndroidJNI.GetMethodID(m_tangoUxHelper_class, "processPoseDataStatus", "(I)V");
                val[0].i = poseStatus;
                AndroidJNI.CallVoidMethod(m_tangoUxHelper_obj, m_tangoUxHelper_processPoseDataStatus, val);
            }
        }
    }

    /// <summary>
    /// Parses the tango depth point count.
    /// </summary>
    /// <param name="pointCount">Point count.</param>
    public static void ParseTangoDepthPointCount(int pointCount)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("processXyzIjPointCount", pointCount);
        }
    }

    /// <summary>
    /// Initialize Tango UX library.
    /// </summary>
    public static void InitTangoUx()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("initTangoUx");
        }
    }

    /// <summary>
    /// Shows the standard Tango exception UI.
    /// </summary>
    /// <param name="shouldUseDefaultUi">A flag to indicate if default <c>TangoUx</c> UI is enabled.</param>
    public static void ShowStandardTangoExceptionsUI(bool shouldUseDefaultUi)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("showDefaultExceptionsUi", shouldUseDefaultUi);
        }
    }

    /// <summary>
    /// Start exception handler.
    /// 
    /// This is interface is to handle the case of displaying notification when Tango is not connect, i.e.
    /// Tango Core out of date.
    /// </summary>
    public static void StartExceptionHandler()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("startExceptionHandler");
        }
    }

    /// <summary>
    /// Starts the tango UX library.
    /// Should be called after connecting to Tango service.
    /// </summary>
    /// <param name="showConnectionScreen">Specifies whether the Connection layout should be shown.
    /// Set this to false if motion tracking is disabled.</param>
    public static void StartTangoUX(bool showConnectionScreen)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("start", showConnectionScreen);
        }
    }

    /// <summary>
    /// Stops the tango UX library.
    /// Should be called before disconnect.
    /// </summary>
    public static void StopTangoUX()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("stop");
        }
    }

    /// <summary>
    /// Sets the Tango UX exception event listener.
    /// </summary>
    public static void SetUxExceptionEventListener()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("setUxExceptionEventListener", UxExceptionEventListener.GetInstance);
        }
    }

    /// <summary>
    /// Sets the recommended way to hold the device.
    /// </summary>
    /// <param name="holdPostureType">Recommended way to hold the device.</param>
    public static void SetHoldPosture(int holdPostureType)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("setHoldPosture", holdPostureType);
        }
    }

    /// <summary>
    /// Display notification for Tango Core out of date.
    /// </summary>
    public static void ShowTangoOutOfDate()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if (tangoUxObject != null)
        {
            tangoUxObject.Call("showTangoOutOfDate");
        }
    }
}
