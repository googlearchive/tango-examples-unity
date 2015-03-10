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
using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using Tango;
using UnityEngine;

namespace Tango
{
	/// <summary>
	/// Functionality for interacting with the Tango Service
	/// configuration.
	/// </summary>
	public class TangoConfig
	{
		#region Attributes
		/// <summary>
		/// Key/Value pairs supported by the Tango Service.
		/// </summary>	
	    public struct Keys
	    {
	        // Motion Tracking
	        public static readonly string ENABLE_MOTION_TRACKING_BOOL = "config_enable_motion_tracking";
	        public static readonly string ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL = "config_enable_auto_recovery";

	        // Area Learning
	        public static readonly string ENABLE_AREA_LEARNING_BOOL = "config_enable_learning_mode";
	        public static readonly string LOAD_AREA_DESCRIPTION_UUID_STRING = "config_load_area_description_UUID";

	        // Depth Perception
	        public static readonly string ENABLE_DEPTH_PERCEPTION_BOOL = "config_enable_depth";

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
		/// Delegate definition for setting API callbacks when setting values in the Tango Config.
		/// </summary>
		/// <param name="obj1"> The IntPtr object, usually the tango config reference.</param>
		/// <param name="obj2"> Usually the key that we want to modify in the tango config file.</param>
		/// <param name="obj3"> Usually the value that we want to modify for the key in the tango config file.</param>
		/// <returns> <c> Common.ErrorType.TANGO_SUCCESS </c> if API call was successfull, 
		/// <c> Common.ErrorType.TANGO_ERROR </c> otherwise.</returns>
		public delegate int ConfigAPIDelegate<T>(IntPtr obj1, string obj2, T obj3);
		#endregion

		/// <summary>
		/// Gets the handle to the current Tango configuration.
		/// </summary>
		/// <returns> Handle to the Tango configuration.</returns>
	    public static IntPtr GetConfig()
	    {
	        return m_tangoConfig;
	    }

		/// <summary>
		/// Fills out a given Tango configuration with the currently set configuration settings.
		/// </summary>
		public static void InitConfig(TangoEnums.TangoConfigType configType)
	    {
			m_tangoConfig = TangoConfigAPI.TangoService_getConfig(configType);

			// TODO : error check this!
	    }

		/// <summary>
		/// Deallocate a Tango configuration object.
		/// </summary>
	    public static void Free()
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
		/// Gets a string representing the current settings
		/// of the Tango configuration.
		/// </summary>
		/// <returns> String representing the current settings.
		/// Null if no configuration is found.</returns>
	    public static string GetSettings()
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
		/// Sets the value of a boolean key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if bool was set, <c>false</c> otherwise.</returns>
		/// <param name="key"> Key.</param>
		/// <param name="value"> If set to <c>true</c> value.</param>
	    public static bool SetBool(string key, bool value)
	    {
			return _ConfigHelperSet(new ConfigAPIDelegate<bool>(TangoConfigAPI.TangoConfig_setBool), m_tangoConfig, key, value, "SetBool");
	    }

		/// <summary>
		/// Sets the value of an int32 key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if int32 was set, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool SetInt32(string key, Int32 value)
	    {
			return _ConfigHelperSet(new ConfigAPIDelegate<Int32>(TangoConfigAPI.TangoConfig_setInt32), m_tangoConfig, key, value, "SetInt32");
	    }

		/// <summary>
		/// Sets the value of an int64 key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if int64 was set, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool SetInt64(string key, Int64 value)
	    {
			return _ConfigHelperSet(new ConfigAPIDelegate<Int64>(TangoConfigAPI.TangoConfig_setInt64), m_tangoConfig, key, value, "SetInt64");
	    }
	    
		/// <summary>
		/// Sets the value of a double key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if double was set, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool SetDouble(string key, double value)
	    {
			return _ConfigHelperSet(new ConfigAPIDelegate<double>(TangoConfigAPI.TangoConfig_setDouble), m_tangoConfig, key, value, "SetDouble");
	    }
	    
		/// <summary>
		/// Sets the value of a string key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if string was set, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool SetString(string key, string value)
	    {
			return _ConfigHelperSet(new ConfigAPIDelegate<string>(TangoConfigAPI.TangoConfig_setString), m_tangoConfig, key, value, "SetString");
	    }

		/// <summary>
		/// Gets the value of a bool key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool GetBool(string key, ref bool value)
	    {
			bool wasSuccess = false;
			if (m_tangoConfig != IntPtr.Zero)
	        {
				wasSuccess = TangoConfigAPI.TangoConfig_getBool(m_tangoConfig, key, ref value) == Common.ErrorType.TANGO_SUCCESS;
	        }
			if (!wasSuccess)
			{
#if UNITY_ANDROID && !UNITY_EDITOR
				Debug.Log(string.Format(m_ErrorLogFormat, "GetBool", key, false));
#endif
			}
			return wasSuccess;
	    }
	    
		/// <summary>
		/// Gets the value of an int32 kay/value pair.
		/// </summary>
		/// <returns><c>true</c>, if int32 was gotten, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool GetInt32(string key, ref Int32 value)
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
		/// Gets the value of an int64 key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if int64 was gotten, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool GetInt64(string key, ref Int64 value)
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
		/// Gets the value of a double key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if double was gotten, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool GetDouble(string key, ref double value)
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
		/// Gets the value of a string key/value pair.
		/// </summary>
		/// <returns><c>true</c>, if string was gotten, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
	    public static bool GetString(string key, ref string value)
	    {
			bool wasSuccess = false;
			if (m_tangoConfig != IntPtr.Zero)
	        {
	            UInt32 stringLength = 512;

	            // char[] tempString = new char[512];
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
		/// Helper method for setting values in the Tango Config file.
		/// </summary>
		/// <returns><c>true</c> if the API call returned success, <c>false</c> otherwise.</returns>
		/// <param name="apiCall">The API call we want to perform.</param>
		/// <param name="tangoConfig">Ptr to the current active Tango Config.</param>
		/// <param name="configKey">The key of the config file we want to modify the value of.</param>
		/// <param name="configValue">The new value we want to set.</param>
		/// <param name="tangoMethodName">String representing the name of the method we are trying to call. Used for logging purposes.</param>
		/// <typeparam name="T">The type of object we are trying to set.</typeparam>
		internal static bool _ConfigHelperSet<T>(ConfigAPIDelegate<T> apiCall, IntPtr tangoConfig, string configKey, object configValue, 
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
		/// Interface for the Tango Service API.
		/// </summary>
	    internal struct TangoConfigAPI
	    {
#if UNITY_ANDROID && !UNITY_EDITOR
	        [DllImport(Common.TANGO_UNITY_DLL)]
	        public static extern void TangoConfig_free(IntPtr tangoConfig);
	        
	        [DllImport(Common.TANGO_UNITY_DLL)]
	        public static extern string TangoConfig_toString(IntPtr TangoConfig);

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
	        public static string TangoConfig_toString(IntPtr TangoConfig)
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