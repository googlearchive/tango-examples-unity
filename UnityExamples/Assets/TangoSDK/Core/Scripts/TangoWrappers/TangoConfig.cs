//-----------------------------------------------------------------------
// <copyright file="TangoConfig.cs" company="Google">
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
using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using Tango;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// C API wrapper for Tango Configuration Parameters.
    /// </summary>
    internal class TangoConfig
    {
        #region Attributes
        /// <summary>
        /// Key/Value pairs supported by the Tango Service.
        /// </summary>
        internal struct Keys
        {
            // Motion Tracking
            public static readonly string ENABLE_MOTION_TRACKING_BOOL = "config_enable_motion_tracking";
            public static readonly string ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL = "config_enable_auto_recovery";
            public static readonly string ENABLE_LOW_LATENCY_IMU_INTEGRATION = "config_enable_low_latency_imu_integration";
            
            // Area Learning
            public static readonly string ENABLE_AREA_LEARNING_BOOL = "config_enable_learning_mode";
            public static readonly string LOAD_AREA_DESCRIPTION_UUID_STRING = "config_load_area_description_UUID";
            
            // Depth Perception
            public static readonly string ENABLE_DEPTH_PERCEPTION_BOOL = "config_enable_depth";
            
            // Video overlay
            public static readonly string EXPERIMENTAL_Y_TEXTURE_HEIGHT = "experimental_color_y_tex_data_height";
            public static readonly string EXPERIMENTAL_Y_TEXTURE_WIDTH = "experimental_color_y_tex_data_width";
            public static readonly string EXPERIMENTAL_UV_TEXTURE_HEIGHT = "experimental_color_uv_tex_data_height";
            public static readonly string EXPERIMENTAL_UV_TEXTURE_WIDTH = "experimental_color_uv_tex_data_width";
            
            // Utility
            public static readonly string ENABLE_DATASET_RECORDING = "config_enable_dataset_recording";
            public static readonly string GET_TANGO_SERVICE_VERSION_STRING = "tango_service_library_version";
        }

        private const string m_FailedConversionFormat = "Failed to convert object to generic type : {0}. Reverting to default.";
        private const string m_ErrorLogFormat = "{0}.{1}() Was unable to set key: {2} with value: {3}";
        private const string m_ConfigErrorFormat = "{0}.{1}() Invalid TangoConfig, make sure Tango Config is initialized properly.";
        private static readonly string CLASS_NAME = "TangoConfig.";
        private static readonly string NO_CONFIG_FOUND = "No config file found.";
        private static IntPtr m_tangoConfig = IntPtr.Zero;

        /// <summary>
        /// Delegate for internal API call that sets a config option.
        /// 
        /// This matches the signature of TangoConfig_setBool, TangoConfig_Double, etc. 
        /// </summary>
        /// <param name="obj1">C pointer to a TangoConfig.</param>
        /// <param name="obj2">Key we want to modify.</param>
        /// <param name="obj3">Value to set, of the correct type.</param>
        /// <returns>
        /// Returns TANGO_SUCCESS on success or TANGO_INVALID if config or key is NULL, or key is not found or could
        /// not be set.
        /// </returns>
        private delegate int ConfigAPIDelegate<T>(IntPtr obj1, string obj2, T obj3);
        #endregion

        /// <summary>
        /// Gets the cached C pointer to a TangoConfig.
        /// 
        /// This pointer is updated by calling InitConfig.
        /// </summary>
        /// <returns>C pointer to a Tango config.</returns>
        internal static IntPtr GetConfig()
        {
            return m_tangoConfig;
        }

        /// <summary>
        /// Update the cached C pointer to a TangoConfig.
        /// 
        /// This should be used to initialize a Config object for setting the configuration that TangoService should
        /// be run in. The Config handle is passed to TangoService_connect() which starts the service running with
        /// the parameters set at that time in that TangoConfig handle.  This function can be used to find the current
        /// configuration of the service (i.e. what would be run if no config is specified on TangoService_connect()),
        /// or to create one of a few "template" TangoConfigs.  The returned TangoConfig can be further modified by 
        /// TangoConfig_set function calls.  The handle should be freed with Free().  The handle is needed 
        /// only at the time of TangoService_connect() where it is used to configure the service, and can safely be
        /// freed after it has been used in TangoService_connect().
        /// </summary>
        /// <param name="configType">The requested configuration type.</param>
        internal static void InitConfig(TangoEnums.TangoConfigType configType)
        {
            m_tangoConfig = TangoConfigAPI.TangoService_getConfig(configType);

            // TODO : error check this!
        }

        /// <summary>
        /// Free a TangoConfig object.
        /// 
        /// Frees the TangoConfig object for the cached handle.
        /// </summary>
        internal static void Free()
        {
            if (m_tangoConfig != IntPtr.Zero)
            {
                TangoConfigAPI.TangoConfig_free(m_tangoConfig);
            } 
            else
            {
                Debug.Log(CLASS_NAME + ".Free() No allocated Tango Config found!");
            }
        }

        /// <summary>
        /// Gets a string of key-value pairs of all the configuration values of TangoService.
        /// 
        /// The string is separated into lines such that each line is one key-value pair, with format "key=value\n".  
        /// Note that many of these config values are read-only, unless otherwise documented.
        /// </summary>
        /// <returns>String representation of the cached configuration.</returns>
        internal static string GetSettings()
        {
            if (m_tangoConfig != IntPtr.Zero)
            {
                return TangoConfigAPI.TangoConfig_toString(m_tangoConfig);
            } 
            else
            {
                return NO_CONFIG_FOUND;
            }
        }

        /// <summary>
        /// Set a boolean configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal static bool SetBool(string key, bool value)
        {
            return _ConfigHelperSet(new ConfigAPIDelegate<bool>(TangoConfigAPI.TangoConfig_setBool), m_tangoConfig, key, value, "SetBool");
        }

        /// <summary>
        /// Set an Int32 configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal static bool SetInt32(string key, Int32 value)
        {
            return _ConfigHelperSet(new ConfigAPIDelegate<Int32>(TangoConfigAPI.TangoConfig_setInt32), m_tangoConfig, key, value, "SetInt32");
        }

        /// <summary>
        /// Set an Int64 configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal static bool SetInt64(string key, Int64 value)
        {
            return _ConfigHelperSet(new ConfigAPIDelegate<Int64>(TangoConfigAPI.TangoConfig_setInt64), m_tangoConfig, key, value, "SetInt64");
        }

        /// <summary>
        /// Set a double configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal static bool SetDouble(string key, double value)
        {
            return _ConfigHelperSet(new ConfigAPIDelegate<double>(TangoConfigAPI.TangoConfig_setDouble), m_tangoConfig, key, value, "SetDouble");
        }

        /// <summary>
        /// Set a string configuration parameter.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to set.</param>
        /// <param name="value">The value to set the configuration key to.</param>
        internal static bool SetString(string key, string value)
        {
            return _ConfigHelperSet(new ConfigAPIDelegate<string>(TangoConfigAPI.TangoConfig_setString), m_tangoConfig, key, value, "SetString");
        }

        /// <summary>
        /// Get a boolean configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal static bool GetBool(string key, ref bool value)
        {
            bool wasSuccess = false;
            if (m_tangoConfig != IntPtr.Zero)
            {
                wasSuccess = TangoConfigAPI.TangoConfig_getBool(m_tangoConfig, key, ref value) == Common.ErrorType.TANGO_SUCCESS;
            }
            if (!wasSuccess)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Debug.LogWarning(string.Format(m_ErrorLogFormat, "GetBool", key, false));
#endif
            }
            return wasSuccess;
        }

        /// <summary>
        /// Get an Int32 configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal static bool GetInt32(string key, ref Int32 value)
        {
            bool wasSuccess = false;
            if (m_tangoConfig != IntPtr.Zero)
            {
                wasSuccess = TangoConfigAPI.TangoConfig_getInt32(m_tangoConfig, key, ref value) == Common.ErrorType.TANGO_SUCCESS;
            }
            if (!wasSuccess)
            {
                Debug.Log(string.Format(m_ErrorLogFormat, "GetInt32", key, value));
            }
            return wasSuccess;
        }

        /// <summary>
        /// Get an Int64 configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal static bool GetInt64(string key, ref Int64 value)
        {
            bool wasSuccess = false;
            if (m_tangoConfig != IntPtr.Zero)
            {
                wasSuccess = TangoConfigAPI.TangoConfig_getInt64(m_tangoConfig, key, ref value) == Common.ErrorType.TANGO_SUCCESS;
            }
            if (!wasSuccess)
            {
                Debug.Log(string.Format(m_ErrorLogFormat, "GetInt64", key, value));
            }
            return wasSuccess;
        }

        /// <summary>
        /// Get a double configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal static bool GetDouble(string key, ref double value)
        {
            bool wasSuccess = false;
            if (m_tangoConfig != IntPtr.Zero)
            {
                wasSuccess = TangoConfigAPI.TangoConfig_getDouble(m_tangoConfig, key, ref value) == 0;
            }
            if (!wasSuccess)
            {
                Debug.Log(string.Format(m_ErrorLogFormat, "GetDouble", key, value));
            }
            return wasSuccess;
        }

        /// <summary>
        /// Get a string configuration parameter.
        /// </summary>
        /// <returns><c>true</c>, if the value was retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">The string key value of the configuration parameter to get.</param>
        /// <param name="value">On successful return, the value of the configuration key.</param>
        internal static bool GetString(string key, ref string value)
        {
            bool wasSuccess = false;
            if (m_tangoConfig != IntPtr.Zero)
            {
                UInt32 stringLength = 512;

                StringBuilder tempString = new StringBuilder(512); 
                wasSuccess = TangoConfigAPI.TangoConfig_getString(m_tangoConfig, key, tempString, stringLength) == Common.ErrorType.TANGO_SUCCESS;
                if (wasSuccess)
                {
                    value = tempString.ToString();
                } 
                else
                {
                    Debug.Log(string.Format(m_ErrorLogFormat, "GetString", key, value));
                }
            }
            return wasSuccess;
        }

        /// <summary>
        /// Helper method for setting a configuration parameter.
        /// </summary>
        /// <returns><c>true</c> if the API call returned success, <c>false</c> otherwise.</returns>
        /// <param name="apiCall">The API call we want to perform.</param>
        /// <param name="tangoConfig">Handle to a Tango Config.</param>
        /// <param name="configKey">The string key value of the configuration parameter to set.</param>
        /// <param name="configValue">The value to set the configuration key to.</param>
        /// <param name="tangoMethodName">Name of the method we are calling. Used for logging purposes.</param>
        /// <typeparam name="T">The type of object we are trying to set.</typeparam>
        private static bool _ConfigHelperSet<T>(ConfigAPIDelegate<T> apiCall, IntPtr tangoConfig, string configKey, object configValue, 
                                                 string tangoMethodName)
        {
            if (tangoConfig == IntPtr.Zero)
            {
                Debug.Log(string.Format(m_ConfigErrorFormat, CLASS_NAME, tangoMethodName));
                return false;
            }
            bool wasSuccess = false;
            T genericObj;
            try
            {
                genericObj = (T)configValue;
            } 
            catch
            {
                Debug.Log(string.Format(m_FailedConversionFormat, typeof(T)));
                genericObj = default(T);
            }
            wasSuccess = apiCall(tangoConfig, configKey, genericObj) == Common.ErrorType.TANGO_SUCCESS;
            if (!wasSuccess)
            {
                Debug.Log(string.Format(m_ErrorLogFormat, CLASS_NAME, tangoMethodName, configKey, configValue));
            }
            return wasSuccess;
        }

        /// <summary>
        /// DEPRECATED: Internal API, should be private.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        internal struct TangoConfigAPI
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern void TangoConfig_free(IntPtr tangoConfig);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern string TangoConfig_toString(IntPtr tangoConfig);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_setBool(IntPtr tangoConfig,
                                                         [MarshalAs(UnmanagedType.LPStr)] string key,
                                                         bool value);
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern IntPtr TangoService_getConfig(TangoEnums.TangoConfigType config_type);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_setInt32(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          Int32 value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_setInt64(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          Int64 value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_setDouble(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           double value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_setString(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           [MarshalAs(UnmanagedType.LPStr)] string value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_getBool(IntPtr tangoConfig,
                                                         [MarshalAs(UnmanagedType.LPStr)] string key,
                                                         ref bool value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_getInt32(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          ref Int32 value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_getInt64(IntPtr tangoConfig,
                                                          [MarshalAs(UnmanagedType.LPStr)] string key,
                                                          ref Int64 value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_getDouble(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           ref double value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoConfig_getString(IntPtr tangoConfig,
                                                           [MarshalAs(UnmanagedType.LPStr)] string key,
                                                           [In, Out] StringBuilder value,
                                                           UInt32 size);
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
#endif
        }
    }
}