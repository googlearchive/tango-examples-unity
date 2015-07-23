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
