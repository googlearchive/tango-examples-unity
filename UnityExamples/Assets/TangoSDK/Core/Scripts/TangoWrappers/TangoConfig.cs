//-----------------------------------------------------------------------
// <copyright file="TangoConfig.cs" company="Google">
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
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
    Justification = "Files can start with an interface.")]

namespace Tango
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Text;
    using Tango;
    using UnityEngine;

    /// <summary>
    /// API wrapper interface for Tango Configuration Parameters.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
    internal interface ITangoConfig
    {
        bool SetBool(string key, bool value);

        bool GetBool(string key, ref bool value);

        bool SetInt32(string key, Int32 value);

        bool GetInt32(string key, ref Int32 value);

        bool SetString(string key, string value);

        IntPtr GetHandle();

        void SetRuntimeConfig();

        void Dispose();
    }

    /// <summary>
    /// C API wrapper for Tango Configuration Parameters.
    /// </summary>
    internal sealed class TangoConfig : IDisposable, ITangoConfig
    {
        private const string m_ConfigErrorFormat = "{0}.{1}() Invalid TangoConfig, make sure Tango Config is initialized properly.";
        private static readonly string CLASS_NAME = "TangoConfig";
        private static readonly string NO_CONFIG_FOUND = "No config file found.";

        /// <summary>
        /// Pointer to the TangoConfig.
        /// </summary>
        private IntPtr m_configHandle;

        /// <summary>
        /// Create a new TangoConfig.
        ///
        /// A TangoConfig is passed to TangoService_connect() which starts the service running with
        /// the parameters set at that time in that TangoConfig.  This function can be used to find the current
        /// configuration of the service (i.e. what would be run if no config is specified on TangoService_connect()),
        /// or to create one of a few "template" TangoConfig objects.
        ///
        /// The class is needed only at the time of TangoService_connect() where it is used to configure the service
        /// and can safely be disposed after it has been used in TangoService_connect().
        /// </summary>
        /// <param name="configType">The requested configuration type.</param>
        public TangoConfig(TangoEnums.TangoConfigType configType)
        {
            m_configHandle = TangoConfigAPI.TangoService_getConfig(configType);
        }

        /// <summary>
        /// Delegate for internal API call that sets a config option.
        ///
        /// This matches the signature of <c>TangoConfig_setBool</c>, <c>TangoConfig_setDouble</c>, etc.
        /// </summary>
        /// <typeparam name="T">Type of the value being set.</typeparam>
        /// <param name="configHandle">TangoConfig handle.</param>
        /// <param name="key">Key we want to modify.</param>
        /// <param name="val">Value to set, of the correct type.</param>
        /// <returns>
        /// Returns TANGO_SUCCESS on success or TANGO_INVALID if config or key is NULL, or key is not found or could
        /// not be set.
        /// </returns>
        private delegate int ConfigAPISetter<T>(IntPtr configHandle, string key, T val);

        /// <summary>
        /// Delegate for internal API call that gets a config option.
        ///
        /// This matches the signature of <c>TangoConfig_getBool</c>, <c>TangoConfig_getDouble</c>, etc.
        /// </summary>
        /// <typeparam name="T">Type of the value being retrieved.</typeparam>
        /// <param name="configHandle">TangoConfig handle.</param>
        /// <param name="key">Key we want to get.</param>
        /// <param name="val">Upon success, the value of for key.</param>
        /// <returns>
        /// Returns TANGO_SUCCESS on success or TANGO_INVALID if config or key is NULL, or key is not found or could
        /// not be set.
        /// </returns>
        private delegate int ConfigAPIGetter<T>(IntPtr configHandle, string key, ref T val);

        /// <summary>
        /// Values for the DEPTH_MODE config option.
        /// </summary>
        internal enum DepthMode
        {
            /// <summary>
            /// PointCloud mode.
            /// </summary>
            XYZC = 0
        }

        /// <summary>
        /// Releases all resource used by the <see cref="Tango.TangoConfig"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Tango.TangoConfig"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Tango.TangoConfig"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="Tango.TangoConfig"/> so the garbage
        /// collector can reclaim the memory that the <see cref="Tango.TangoConfig"/> was occupying.</remarks>
        public void Dispose()
        {
            if (m_configHandle != IntPtr.Zero)
            {
                TangoConfigAPI.TangoConfig_free(m_configHandle);
                m_configHandle = IntPtr.Zero;
            }
            else
            {
                Debug.Log(CLASS_NAME + ".Free() No allocated Tango Config found!");
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get the internal handle for this TangoConfig.
        /// </summary>
        /// <returns>The handle.</returns>
        [PublicForTesting]
        public IntPtr GetHandle()
        {
            return m_configHandle;
        }

        /// <summary>
        /// Set a string configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        [PublicForTesting]
        public bool SetString(string key, string value)
        {
            return _ConfigHelperSet(new ConfigAPISetter<string>(TangoConfigAPI.TangoConfig_setString), key, value, "SetString");
        }

        /// <summary>
        /// Get a boolean configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        [PublicForTesting]
        public bool GetBool(string key, ref bool value)
        {
            return _ConfigHelperGet(new ConfigAPIGetter<bool>(TangoConfigAPI.TangoConfig_getBool), key, ref value, "GetBool");
        }

        /// <summary>
        /// Get an <c>Int32</c> configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        [PublicForTesting]
        public bool GetInt32(string key, ref Int32 value)
        {
            return _ConfigHelperGet(new ConfigAPIGetter<Int32>(TangoConfigAPI.TangoConfig_getInt32), key, ref value, "GetInt32");
        }

        /// <summary>
        /// Set this config as the current runtime config.
        /// </summary>
        [PublicForTesting]
        public void SetRuntimeConfig()
        {
            bool wasSuccess = TangoConfigAPI.TangoService_setRuntimeConfig(m_configHandle) == Common.ErrorType.TANGO_SUCCESS;
            if (!wasSuccess)
            {
                Debug.Log(string.Format("{0}.SetRuntimeConfig() Unable to set runtime config.", CLASS_NAME));
            }
        }

        /// <summary>
        /// Set a boolean configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        [PublicForTesting]
        public bool SetBool(string key, bool value)
        {
            return _ConfigHelperSet(new ConfigAPISetter<bool>(TangoConfigAPI.TangoConfig_setBool), key, value, "SetBool");
        }

        /// <summary>
        /// Set an <c>Int32</c> configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        [PublicForTesting]
        public bool SetInt32(string key, Int32 value)
        {
            return _ConfigHelperSet(new ConfigAPISetter<Int32>(TangoConfigAPI.TangoConfig_setInt32), key, value, "SetInt32");
        }

        /// <summary>
        /// Gets a string of key-value pairs of all the configuration values of TangoService.
        ///
        /// The string is separated into lines such that each line is one key-value pair, with format "key=value\n".
        /// Note that many of these config values are read-only, unless otherwise documented.
        /// </summary>
        /// <returns>String representation of the cached configuration.</returns>
        internal string GetSettings()
        {
            if (m_configHandle != IntPtr.Zero)
            {
                return TangoConfigAPI.TangoConfig_toString(m_configHandle);
            }
            else
            {
                return NO_CONFIG_FOUND;
            }
        }

        /// <summary>
        /// Set an <c>Int64</c> configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal bool SetInt64(string key, Int64 value)
        {
            return _ConfigHelperSet(new ConfigAPISetter<Int64>(TangoConfigAPI.TangoConfig_setInt64), key, value, "SetInt64");
        }

        /// <summary>
        /// Set a double configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal bool SetDouble(string key, double value)
        {
            return _ConfigHelperSet(new ConfigAPISetter<double>(TangoConfigAPI.TangoConfig_setDouble), key, value, "SetDouble");
        }

        /// <summary>
        /// Get an <c>Int64</c> configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal bool GetInt64(string key, ref Int64 value)
        {
            return _ConfigHelperGet(new ConfigAPIGetter<Int64>(TangoConfigAPI.TangoConfig_getInt64), key, ref value, "GetInt64");
        }

        /// <summary>
        /// Get a double configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal bool GetDouble(string key, ref double value)
        {
            return _ConfigHelperGet(new ConfigAPIGetter<double>(TangoConfigAPI.TangoConfig_getDouble), key, ref value, "GetDouble");
        }

        /// <summary>
        /// Get a string configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal bool GetString(string key, ref string value)
        {
            // Can't use _ConfigHelperGet because the API takes a size parameter.
            string tangoMethodName = "GetString";

            if (m_configHandle == IntPtr.Zero)
            {
                Debug.Log(string.Format(m_ConfigErrorFormat, CLASS_NAME, tangoMethodName));
                return false;
            }

            bool wasSuccess = false;
            StringBuilder stringBuilder = new StringBuilder(512);
            wasSuccess = TangoConfigAPI.TangoConfig_getString(m_configHandle, key, stringBuilder, (uint)stringBuilder.Capacity) == Common.ErrorType.TANGO_SUCCESS;
            value = stringBuilder.ToString();
            if (!wasSuccess)
            {
                Debug.Log(string.Format("{0}.{1}() Was unable to get key: {2}", CLASS_NAME, tangoMethodName, key));
            }

            return wasSuccess;
        }

        /// <summary>
        /// Helper method for setting a configuration parameter.
        /// </summary>
        /// <returns><c>true</c> if the API call returned success, <c>false</c> otherwise.</returns>
        /// <param name="apiCall">The API call to perform.</param>
        /// <param name="key">The key of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        /// <param name="tangoMethodName">Name of the calling method. Used for logging purposes.</param>
        /// <typeparam name="T">The type of object to set.</typeparam>
        private bool _ConfigHelperSet<T>(ConfigAPISetter<T> apiCall, string key, T value, string tangoMethodName)
        {
#if UNITY_EDITOR
            return true;
#else
            if (m_configHandle == IntPtr.Zero)
            {
                Debug.Log(string.Format(m_ConfigErrorFormat, CLASS_NAME, tangoMethodName));
                return false;
            }

            bool wasSuccess = false;
            wasSuccess = apiCall(m_configHandle, key, value) == Common.ErrorType.TANGO_SUCCESS;
            if (!wasSuccess)
            {
                Debug.Log(string.Format("{0}.{1}() Was unable to set key: {2} with value: {3}",
                                        CLASS_NAME, tangoMethodName, key, value));
            }

            return wasSuccess;
#endif
        }

        /// <summary>
        /// Helper method for getting a configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the API call returned success, <c>false</c> otherwise.</returns>
        /// <param name="apiCall">The API call to perform.</param>
        /// <param name="key">The key of the configuration parameter to get.</param>
        /// <param name="value">On success, this is filled with the value of the configuration parameter.</param>
        /// <param name="tangoMethodName">Name of the calling method. Used for logging purposes.</param>
        /// <typeparam name="T">The 1type of object to get.</typeparam>
        private bool _ConfigHelperGet<T>(ConfigAPIGetter<T> apiCall, string key, ref T value, string tangoMethodName)
        {
            if (m_configHandle == IntPtr.Zero)
            {
                Debug.Log(string.Format(m_ConfigErrorFormat, CLASS_NAME, tangoMethodName));
                return false;
            }

            bool wasSuccess = false;
            wasSuccess = apiCall(m_configHandle, key, ref value) == Common.ErrorType.TANGO_SUCCESS;
            if (!wasSuccess)
            {
                Debug.Log(string.Format("{0}.{1}() Was unable to get key: {2}", CLASS_NAME, tangoMethodName, key));
            }

            return wasSuccess;
        }

        /// <summary>
        /// Key/Value pairs supported by the Tango Service.
        /// </summary>
        internal struct Keys
        {
            // Motion Tracking
            public const string ENABLE_MOTION_TRACKING_BOOL = "config_enable_motion_tracking";
            public const string ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL = "config_enable_auto_recovery";
            public const string ENABLE_LOW_LATENCY_IMU_INTEGRATION = "config_enable_low_latency_imu_integration";

            // Area Learning
            public const string ENABLE_AREA_LEARNING_BOOL = "config_enable_learning_mode";
            public const string LOAD_AREA_DESCRIPTION_UUID_STRING = "config_load_area_description_UUID";
            public const string ENABLE_CLOUD_ADF_BOOL = "config_experimental_use_cloud_adf";

            // Experimental COM
            public const string EXPERIMENTAL_ENABLE_DRIFT_CORRECTION_BOOL = "config_enable_drift_correction";

            // Depth Perception
            public const string ENABLE_DEPTH_PERCEPTION_BOOL = "config_enable_depth";
            public const string DEPTH_MODE = "config_depth_mode";

            // Video overlay
            public const string ENABLE_COLOR_CAMERA_BOOL = "config_enable_color_camera";
            public const string EXPERIMENTAL_Y_TEXTURE_HEIGHT = "experimental_color_y_tex_data_height";
            public const string EXPERIMENTAL_Y_TEXTURE_WIDTH = "experimental_color_y_tex_data_width";
            public const string EXPERIMENTAL_UV_TEXTURE_HEIGHT = "experimental_color_uv_tex_data_height";
            public const string EXPERIMENTAL_UV_TEXTURE_WIDTH = "experimental_color_uv_tex_data_width";

            // Utility
            public static readonly string ENABLE_DATASET_RECORDING = "config_enable_dataset_recording";
            public static readonly string GET_TANGO_SERVICE_VERSION_STRING = "tango_service_library_version";

            // Runtime configs
            public static readonly string RUNTIME_DEPTH_FRAMERATE = "config_runtime_depth_framerate";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct TangoConfigAPI
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern void TangoConfig_free(IntPtr tangoConfig);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern string TangoConfig_toString(IntPtr tangoConfig);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_setBool(IntPtr tangoConfig,
                                                         [MarshalAs(UnmanagedType.LPStr)] string key,
                                                         bool value);
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern IntPtr TangoService_getConfig(TangoEnums.TangoConfigType config_type);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_setInt32(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          Int32 value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_setInt64(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          Int64 value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_setDouble(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           double value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_setString(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           [MarshalAs(UnmanagedType.LPStr)] string value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_getBool(IntPtr tangoConfig,
                                                         [MarshalAs(UnmanagedType.LPStr)] string key,
                                                         ref bool value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_getInt32(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          ref Int32 value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_getInt64(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          ref Int64 value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_getDouble(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           ref double value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoConfig_getString(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           [In, Out] StringBuilder value,
                                                           UInt32 size);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_setRuntimeConfig(IntPtr tangoConfig);
#else
            public static void TangoConfig_free(IntPtr tangoConfig)
            {
            }

            public static IntPtr TangoService_getConfig(TangoEnums.TangoConfigType config_type)
            {
                return IntPtr.Zero;
            }

            public static string TangoConfig_toString(IntPtr tangoConfig)
            {
                return "Editor Mode";
            }

            public static int TangoConfig_setBool(IntPtr tangoConfig,
                                                  [MarshalAs(UnmanagedType.LPStr)] string key,
                                                  bool value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_setInt32(IntPtr tangoConfig,
                                                   [MarshalAs(UnmanagedType.LPStr)] string key,
                                                   Int32 value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_setInt64(IntPtr tangoConfig,
                                                   [MarshalAs(UnmanagedType.LPStr)] string key,
                                                   Int64 value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_setDouble(IntPtr tangoConfig,
                                                    [MarshalAs(UnmanagedType.LPStr)] string key,
                                                    double value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_setString(IntPtr tangoConfig,
                                                    [MarshalAs(UnmanagedType.LPStr)] string key,
                                                    string value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_getBool(IntPtr tangoConfig,
                                                  [MarshalAs(UnmanagedType.LPStr)] string key,
                                                  ref bool value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_getInt32(IntPtr tangoConfig,
                                                   [MarshalAs(UnmanagedType.LPStr)] string key,
                                                   ref Int32 value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_getInt64(IntPtr tangoConfig,
                                                   [MarshalAs(UnmanagedType.LPStr)] string key,
                                                   ref Int64 value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_getDouble(IntPtr tangoConfig,
                                                    [MarshalAs(UnmanagedType.LPStr)] string key,
                                                    ref double value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoConfig_getString(IntPtr tangoConfig,
                                                    [MarshalAs(UnmanagedType.LPStr)] string key,
                                                    [In, Out] StringBuilder value,
                                                    UInt32 size)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_setRuntimeConfig(IntPtr tangoConfig)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
    }
}
