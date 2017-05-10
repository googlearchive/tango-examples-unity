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
using UnityEngine;

/// <summary>
/// Miscellaneous Android related utilities provided by the Tango CoreSDK.
/// </summary>
public partial class AndroidHelper
{
    internal const int TANGO_MINIMUM_VERSION_CODE = 14694;

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
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            int requestCode = 0;
            if (permissionsType == Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS)
            {
                requestCode = Tango.Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE;
            }

            if (requestCode != 0)
            {
                tangoObject.Call("startPermissionActivity", requestCode, permissionsType);
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
    /// Get current Android rotation of the device.
    /// </summary>
    /// <returns>Current native android rotation.</returns>
    public static Tango.OrientationManager.Rotation GetDisplayRotation()
    {
#if UNITY_EDITOR
        return Tango.OrientationManager.Rotation.ROTATION_0;
#else
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return (Tango.OrientationManager.Rotation)tangoObject.Call<int>("getDisplayRotation");
        }

        return Tango.OrientationManager.Rotation.INVALID;
#endif
    }

    /// <summary>
    /// Get color camera sensor rotation from Android. This will never change.
    /// </summary>
    /// <returns>Current color camera rotation.</returns>
    public static Tango.OrientationManager.Rotation GetColorCameraRotation()
    {
#if UNITY_EDITOR
        return Tango.OrientationManager.Rotation.ROTATION_0;
#else
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            int rot = tangoObject.Call<int>("getColorCameraRotation");
            switch (rot)
            {
            case 0:
                return Tango.OrientationManager.Rotation.ROTATION_0;
            case 90:
                return Tango.OrientationManager.Rotation.ROTATION_90;
            case 180:
                return Tango.OrientationManager.Rotation.ROTATION_180;
            case 270:
                return Tango.OrientationManager.Rotation.ROTATION_270;
            default:
                return Tango.OrientationManager.Rotation.INVALID;
            }
        }

        return Tango.OrientationManager.Rotation.INVALID;
#endif
    }

    /// <summary>
    /// Get the default orientation of the device. For example, most phones will return portrait and most tablets will
    /// return landscape. This will never change.
    /// </summary>
    /// <returns>Default orientation, odd number represents portrait, even number represents landscape.</returns>
    public static int GetDefaultOrientation()
    {
#if UNITY_EDITOR
        return 0;
#else
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<int>("getDeviceDefaultOrientation");
        }

        return -1;
#endif
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
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<int>("getTangoCoreVersionCode") != 0;
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
#if UNITY_EDITOR
        return true;
#else
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            int rawCode = tangoObject.Call<int>("getTangoCoreVersionCode");

            // The first two digits of the version code are actually the Platform version.  The real
            // code is actually the integer following those.
            string stringCode = rawCode.ToString().Remove(0, 2);
            int realCode;
            if (int.TryParse(stringCode, out realCode))
            {
                return realCode >= TANGO_MINIMUM_VERSION_CODE;
            }
        }

        return false;
#endif
    }

    /// <summary>
    /// Get the Tango Core's version name field.
    /// </summary>
    /// <returns>The tango core version name.</returns>
    public static string GetTangoCoreVersionName()
    {
#if UNITY_EDITOR
        return "UnityEditor";
#else
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            return tangoObject.Call<string>("getTangoCoreVersionName");
        }

        return string.Empty;
#endif
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
    /// Call export ADF permission activity.
    /// </summary>
    /// <param name="srcAdfUuid">ADF that is going to be exported.</param>
    /// <param name="exportLocation">Path to the export location.</param>
    internal static void StartExportADFActivity(string srcAdfUuid, string exportLocation)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            tangoObject.Call("startExportAreaDescriptionActivity",
                             Tango.Common.TANGO_ADF_EXPORT_REQUEST_CODE,
                             srcAdfUuid,
                             exportLocation);
        }
    }

    /// <summary>
    /// Call import ADF permission activity.
    /// </summary>
    /// <param name="adfPath">Path to the ADF that is going to be imported.</param>
    internal static void StartImportADFActivity(string adfPath)
    {
        AndroidJavaObject tangoObject = GetTangoHelperObject();

        if (tangoObject != null)
        {
            tangoObject.Call("startImportAreaDescriptionActivity",
                             Tango.Common.TANGO_ADF_IMPORT_REQUEST_CODE,
                             adfPath);
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
