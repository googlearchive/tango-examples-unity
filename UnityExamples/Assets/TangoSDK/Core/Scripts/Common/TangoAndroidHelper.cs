//-----------------------------------------------------------------------
// <copyright file="TangoAndroidHelper.cs" company="Google">
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

/// <summary>
/// Misc Android related utilities provided by the Tango CoreSDK.
/// </summary>
public partial class AndroidHelper
{
    /// <summary>
    /// Holds the current and default orientation of the device.
    /// </summary>
    public struct TangoDeviceOrientation
    {
        /// <summary>
        /// The default orientation of the device.  This is the "natural" way to hold this device.
        /// </summary>
        public DeviceOrientation defaultRotation;

        /// <summary>
        /// The current orientation of the device.
        /// </summary>
        public DeviceOrientation currentRotation;
    }

    private const string PERMISSION_REQUEST_ACTIVITY = "com.google.atap.tango.RequestPermissionActivity";

    private const string TANGO_APPLICATION_ID = "com.projecttango.tango";
    private const string LAUNCH_INTENT_SIGNATURE = "launchIntent";
    private const string ADF_IMPORT_EXPORT_ACTIVITY = "com.google.atap.tango.RequestImportExportActivity";

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject m_tangoHelper = null;
#endif

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
    /// Start the Tango permissions activity, requesting that permission.
    /// </summary>
    /// <param name="permissionsType">String for the permission to request.</param>
    public static void StartTangoPermissionsActivity(string permissionsType)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();
        
        if (unityActivity != null)
        {
            int requestCode = 0;
            string[] args = new string[1];

            if (permissionsType == Tango.Common.TANGO_MOTION_TRACKING_PERMISSIONS)
            {
                requestCode = Tango.Common.TANGO_MOTION_TRACKING_PERMISSIONS_REQUEST_CODE;
                args[0] = "PERMISSIONTYPE:" + Tango.Common.TANGO_MOTION_TRACKING_PERMISSIONS;
            }
            else if (permissionsType == Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS)
            {
                requestCode = Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE;
                args[0] = "PERMISSIONTYPE:" + Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS;
            }
            
            if (requestCode != 0)
            {
                unityActivity.Call(LAUNCH_INTENT_SIGNATURE,
                                   TANGO_APPLICATION_ID,
                                   PERMISSION_REQUEST_ACTIVITY,
                                   args,
                                   requestCode);
            }
            else
            {
                Debug.Log("Invalid permission request");
            }
        }
    }

    /// <summary>
    /// Check if the application has a Tango permission.
    /// </summary>
    /// <param name="permissionType">String for the permission.</param>
    /// <returns><c>true</c> if application has the permission; otherwise, <c>false</c>.</returns>
    public static bool ApplicationHasTangoPermissions(string permissionType)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        if (tangoObject != null)
        {
            return tangoObject.Call<bool>("hasPermission", permissionType);
        }
        
        return false;
    }

    /// <summary>
    /// Get the devices current and default orientations.
    /// </summary>
    /// <returns>The current and default orientations.</returns>
    public static TangoDeviceOrientation GetTangoDeviceOrientation()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        TangoDeviceOrientation deviceOrientation;
        deviceOrientation.defaultRotation = DeviceOrientation.Unknown;
        deviceOrientation.currentRotation = DeviceOrientation.Unknown;

        if (tangoObject != null)
        {
            AndroidJavaObject rotationInfo = tangoObject.Call<AndroidJavaObject>("showTranslatedOrientation");

            deviceOrientation.defaultRotation = (DeviceOrientation)rotationInfo.Get<int>("defaultRotation");
            deviceOrientation.currentRotation = (DeviceOrientation)rotationInfo.Get<int>("currentRotation");
        }
        
        return deviceOrientation;
    }

    /// <summary>
    /// Check if the Tango Core package is installed.
    /// </summary>
    /// <returns><c>true</c> if the package is installed; otherwise, <c>false</c>.</returns>
    public static bool IsTangoCorePresent()
    {
        AndroidJavaObject unityActivity = GetUnityActivity();
        
        if (unityActivity != null)
        {
            if (GetPackageInfo(TANGO_APPLICATION_ID) != null)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Call export ADF permission activity.
    /// </summary>
    /// <param name="srcAdfUuid">ADF that is going to be exported.</param>
    /// <param name="exportLocation">Path to the export location.</param>
    internal static void StartExportADFActivity(string srcAdfUuid, string exportLocation)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            int requestCode = 1;
            string[] args = new string[2];
            args[0] = "SOURCE_UUID:" + srcAdfUuid;
            args[1] = "DESTINATION_FILE:" + exportLocation;
            unityActivity.Call(LAUNCH_INTENT_SIGNATURE,
                               TANGO_APPLICATION_ID,
                               ADF_IMPORT_EXPORT_ACTIVITY,
                               args,
                               1);
        }
    }

    /// <summary>
    /// Call import ADF permission activity.
    /// </summary>
    /// <param name="adfPath">Path to the ADF that is going to be imported.</param>
    internal static void StartImportADFActivity(string adfPath)
    {
        AndroidJavaObject unityActivity = GetUnityActivity();

        if (unityActivity != null)
        {
            int requestCode = 1;
            string[] args = new string[1];
            args[0] = "SOURCE_FILE:" + adfPath;
            unityActivity.Call(LAUNCH_INTENT_SIGNATURE,
                               TANGO_APPLICATION_ID,
                               ADF_IMPORT_EXPORT_ACTIVITY,
                               args,
                               1);
        }
    }
}
