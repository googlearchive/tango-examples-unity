//-----------------------------------------------------------------------
// <copyright file="TangoAndroidHelper.cs" company="Google">
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
/// Misc Android related utilities provided by the Tango CoreSDK.
/// </summary>
public partial class AndroidHelper
{
    internal const int TANGO_MINIMUM_VERSION_CODE = 6804;

    private const string PERMISSION_REQUEST_ACTIVITY = "com.google.atap.tango.RequestPermissionActivity";
    private const string TANGO_APPLICATION_ID = "com.projecttango.tango";
    private const string LAUNCH_INTENT_SIGNATURE = "launchIntent";
    private const string ADF_IMPORT_EXPORT_ACTIVITY = "com.google.atap.tango.RequestImportExportActivity";

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject m_tangoHelper = null;
#endif

    private static TangoServiceLifecycleListener m_tangoServiceLifecycle;

    /// <summary>
    /// Callback for when the Tango service gets connected.
    /// </summary>
    /// <param name="binder">Binder for the service.</param>
    public delegate void OnTangoServiceConnected(AndroidJavaObject binder);

    /// <summary>
    /// Callback for when the Tango service gets disconnected.
    /// </summary>
    public delegate void OnTangoServiceDisconnected();

    /// <summary>
    /// Load the Tango library.
    /// </summary>
    /// <returns><c>true</c>, if the Tango library was loaded, <c>false</c> otherwise.</returns>
    public static bool LoadTangoLibrary()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass tangoInitialization = new AndroidJavaClass("com.projecttango.unity.TangoInitialization");
        if (tangoInitialization != null)
        {
            return tangoInitialization.CallStatic<bool>("loadLibrary");
        }

        return false;
#else
        return true;
#endif
    }

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
    /// Get native android orientation index.
    /// </summary>
    /// <returns>Current native andorid orientation.</returns>
    public static int GetScreenOrientation()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();
        
        if (tangoObject != null)
        {
            return tangoObject.Call<int>("getScreenOrientation");
        }
        
        return -1;
    }

    /// <summary>
    /// Check if the Tango Core package is installed.
    /// </summary>
    /// <returns><c>true</c> if the package is installed; otherwise, <c>false</c>.</returns>
    public static bool IsTangoCorePresent()
    {
#if UNITY_EDITOR
        return true;
#else
        AndroidJavaObject unityActivity = GetUnityActivity();
        
        if (unityActivity != null)
        {
            if (GetPackageInfo(TANGO_APPLICATION_ID) != null)
            {
                return true;
            }
        }
        
        return false;
#endif
    }

    /// <summary>
    /// Check if the Tango Core package is up to date.
    /// </summary>
    /// <returns><c>true</c> if the Tango Core is up to date; otherwise, <c>false</c>.</returns>
    public static bool IsTangoCoreUpToDate()
    {
        return GetVersionCode(TANGO_APPLICATION_ID) >= TANGO_MINIMUM_VERSION_CODE;
    }

    /// <summary>
    /// Register a delegate to be called when connected to the Tango Android service.
    /// </summary>
    /// <param name="onConnected">Delegate to get called.</param>
    internal static void RegisterOnTangoServiceConnected(OnTangoServiceConnected onConnected)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (m_tangoServiceLifecycle == null)
        {
            _RegisterTangoServiceLifecycle();
        }

        m_tangoServiceLifecycle.m_onTangoServiceConnected += onConnected;
#endif
    }

    /// <summary>
    /// Register a delegate to be called when disconnected from the Tango Android service.
    /// </summary>
    /// <param name="onDisconnected">Delegate to get called.</param>
    internal static void RegisterOnTangoServiceDisconnected(OnTangoServiceDisconnected onDisconnected)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (m_tangoServiceLifecycle == null)
        {
            _RegisterTangoServiceLifecycle();
        }

        m_tangoServiceLifecycle.m_onTangoServiceDisconnected += onDisconnected;
#endif
    }

    /// <summary>
    /// Set the Tango binder.  Necessary to do before calling any Tango functions.
    /// </summary>
    /// <returns>TANGO_SUCCESS if binder was set correctly, otherwise TANGO_ERROR.</returns>
    /// <param name="binder">Android service binder.</param>
    internal static int TangoSetBinder(AndroidJavaObject binder)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<int>("setBinder", binder);
        }

        return Tango.Common.ErrorType.TANGO_ERROR;
    }

    /// <summary>
    /// Binds to the Tango Android Service.
    /// </summary>
    /// <returns><c>true</c>, if tango service connection was initiated, <c>false</c> otherwise.</returns>
    internal static bool BindTangoService()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<bool>("bindTangoService");
        }

        return false;
    }

    /// <summary>
    /// Unbinds from the Tango Android Service.
    /// </summary>
    internal static void UnbindTangoService()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            tangoObject.Call("unbindTangoService");
        }
    }

    /// <summary>
    /// Binds to the Tango Cloud Android Service.
    /// </summary>
    /// <returns><c>true</c> if we successfully connect; otherwise, <c>false</c>.</returns>
    internal static bool BindTangoCloudService()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<bool>("bindTangoCloudService");
        }

        return false;
    }

    /// <summary>
    /// Unbinds from the Tango Cloud Android Service.
    /// </summary>
    /// <returns><c>true</c> if we successfully disconnect; otherwise, <c>false</c>.</returns>
    internal static bool UnbindTangoCloudService()
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<bool>("unbindTangoCloudService");
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
            string[] args = new string[2];
            args[0] = "SOURCE_UUID:" + srcAdfUuid;
            args[1] = "DESTINATION_FILE:" + exportLocation;
            unityActivity.Call(LAUNCH_INTENT_SIGNATURE,
                               TANGO_APPLICATION_ID,
                               ADF_IMPORT_EXPORT_ACTIVITY,
                               args,
                               Tango.Common.TANGO_ADF_EXPORT_REQUEST_CODE);
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
            string[] args = new string[1];
            args[0] = "SOURCE_FILE:" + adfPath;
            unityActivity.Call(LAUNCH_INTENT_SIGNATURE,
                               TANGO_APPLICATION_ID,
                               ADF_IMPORT_EXPORT_ACTIVITY,
                               args,
                               Tango.Common.TANGO_ADF_IMPORT_REQUEST_CODE);
        }
    }

    /// <summary>
    /// Registers Java callbacks to get Android events.
    /// </summary>
    private static void _RegisterTangoServiceLifecycle()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        m_tangoServiceLifecycle = new TangoServiceLifecycleListener();
        tangoObject.Call("attachTangoServiceLifecycleListener", m_tangoServiceLifecycle);
#endif
    }

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

    /// <summary>
    /// Listener class for Tango service lifecycle.  Maintains C# callbacks to get called when interesting
    /// lifecycle events happen.
    /// </summary>
    private class TangoServiceLifecycleListener : AndroidJavaProxy
    {
        public OnTangoServiceConnected m_onTangoServiceConnected;
        public OnTangoServiceDisconnected m_onTangoServiceDisconnected;

        /// <summary>
        /// Initializes a new instance of the <see cref="AndroidHelper+TangoServiceLifecycleListener"/> class.
        /// </summary>
        public TangoServiceLifecycleListener() : base("com.projecttango.unity.TangoUnityHelper$TangoServiceLifecycleListener")
        {
        }

        /// <summary>
        /// Method called from Java side when connected to the Tango service.
        /// </summary>
        /// <param name="binder">Android service binder.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                         "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                         Justification = "Android API.")]
        public void onTangoServiceConnected(AndroidJavaObject binder)
        {
            Debug.Log("onTangoServiceConnected");

            if (m_onTangoServiceConnected != null)
            {
                m_onTangoServiceConnected(binder);
            }
        }

        /// <summary>
        /// Method called from Java side when disconnected from the Tango service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                         "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                         Justification = "Android API.")]
        public void onTangoServiceDisconnected()
        {
            Debug.Log("onTangoServiceDisconnected");
            if (m_onTangoServiceDisconnected != null)
            {
                m_onTangoServiceDisconnected();
            }
        }
    }
}
