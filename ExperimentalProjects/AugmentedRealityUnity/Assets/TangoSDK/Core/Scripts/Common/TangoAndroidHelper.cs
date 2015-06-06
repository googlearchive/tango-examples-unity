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

/// <summary>
/// Helper functions for common android functionality.
/// </summary>
public partial class AndroidHelper
{
    private const string PERMISSION_REQUESTER = "com.projecttango.permissionrequester.RequestManagerActivity";

    #pragma warning disable 414
    private static AndroidJavaObject m_tangoHelper = null;
    #pragma warning restore 414
    
    /// <summary>
    /// Gets the Java tango helper object.
    /// </summary>
    /// <returns>The tango helper object.</returns>
    public static AndroidJavaObject GetTangoHelperObject()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if(m_tangoHelper == null)
        {
            m_tangoHelper = new AndroidJavaObject("com.projecttango.unity.TangoUnityHelper", GetUnityActivity());
        }
        return m_tangoHelper;
        #else
        return null;
        #endif
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
    
    /// <summary>
    /// Parses the tango event.
    /// </summary>
    /// <param name="timestamp">Timestamp.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="key">Key.</param>
    /// <param name="value">Value.</param>
    public static void ParseTangoEvent(double timestamp, int eventType, string key, string value)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            tangoObject.Call("processTangoEvent", timestamp, eventType, key, value);
        }
    }
    
    /// <summary>
    /// Parses the tango pose status.
    /// </summary>
    /// <param name="poseStatus">Pose status.</param>
    public static void ParseTangoPoseStatus(int poseStatus)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            tangoObject.Call("processPoseDataStatus", poseStatus);
        }
    }
    
    /// <summary>
    /// Parses the tango depth point count.
    /// </summary>
    /// <param name="pointCount">Point count.</param>
    public static void ParseTangoDepthPointCount(int pointCount)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            tangoObject.Call("processXyzIjPointCount", pointCount);
        }
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
    /// Shows the standard tango exceptions UI.
    /// </summary>
    public static void ShowStandardTangoExceptionsUI(bool shouldUseDefaultUi)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            Debug.Log("Show UX exceptions");
            tangoObject.Call("showDefaultExceptionsUi", shouldUseDefaultUi);
        }
    }
    
    /// <summary>
    /// Starts the tango UX library.
    /// Should be called after connecting to Tango service.
    /// </summary>
    public static void StartTangoUX()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            tangoObject.Call("start");
        }
    }
    
    /// <summary>
    /// Stops the tango UX library.
    /// Should be called before disconnect.
    /// </summary>
    public static void StopTangoUX()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if(tangoObject != null)
        {
            tangoObject.Call("stop");
        }
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
}
