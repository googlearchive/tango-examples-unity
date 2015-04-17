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
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace Tango
{
	public delegate void PermissionsEvent(bool permissionsGranted);

    /// <summary>
    /// Entry point of Tango applications, maintain the application handler.
    /// </summary>
    public class TangoApplication : MonoBehaviour 
	{	
		///<summary>
		/// Permission types used by Tango applications.
		/// </summary>
		[Flags]
		private enum PermissionsTypes
		{
			// All entries must be a power of two for
			// use in a bit field as flags.
			NONE = 0,
			MOTION_TRACKING = 0x1,
			AREA_LEARNING = 0x2,
		}

        public bool m_enableMotionTracking = true;
        public bool m_enableDepth = true;
        public bool m_enableVideoOverlay = false;
        public bool m_motionTrackingAutoReset = true;
		public bool m_enableAreaLearning = false;
		public bool m_enableADFSaveLoad = false;
        public bool m_enableUXLibrary = true;
        public bool m_useExperimentalVideoOverlay = true;
        public bool m_useExperimentalADF = false;

		private static string m_tangoServiceVersion = string.Empty;

		private const string CLASS_NAME = "TangoApplication";
        private const string ANDROID_PRO_LABEL_TEXT = "<size=30>Tango plugin requires Unity Android Pro!</size>";
        private const float ANDROID_PRO_LABEL_PERCENT_X = 0.5f;
        private const float ANDROID_PRO_LABEL_PERCENT_Y = 0.5f;
        private const float ANDROID_PRO_LABEL_WIDTH = 200.0f;
        private const float ANDROID_PRO_LABEL_HEIGHT = 200.0f;
        private const string DEFAULT_AREA_DESCRIPTION = "/sdcard/defaultArea";
		private const string MOTION_TRACKING_LOG_PREFIX = "Motion tracking mode : ";
		private const int MINIMUM_API_VERSION = 1978;

		private event PermissionsEvent m_permissionEvent;

		private PermissionsTypes m_requiredPermissions = 0;

		private static bool m_isValidTangoAPIVersion = false;
		private static bool m_hasVersionBeenChecked = false;
		private DepthProvider m_depthProvider;
		private IntPtr m_callbackContext = IntPtr.Zero;
		private bool m_isServiceConnected = false;
		private bool m_shouldReconnectService = false;
		private bool m_isDisconnecting = false;

        private bool m_sendPermissions = false;
        private bool m_permissionsSuccessful = false;

        private PoseListener m_poseListener;
        private DepthListener m_depthListener;
        private VideoOverlayListener m_videoOverlayListener;
        private TangoEventListener m_tangoEventListener;

        private Texture2D m_videoOverlayTexture;

        /// <summary>
        /// Gets the video overlay texture.
        /// </summary>
        /// <returns>The video overlay texture.</returns>
        public Texture2D GetVideoOverlayTexture()
        {
            return m_videoOverlayTexture;
        }

		/// <summary>
		/// Gets the tango service version.
		/// </summary>
		/// <returns>The tango service version.</returns>
		public static string GetTangoServiceVersion()
		{
			if(m_tangoServiceVersion == string.Empty)
			{
				m_tangoServiceVersion = AndroidHelper.GetVersionName("com.projecttango.tango");
			}

			return m_tangoServiceVersion;
		}

        /// <summary>
        /// Register the specified tangoObject.
        /// </summary>
        /// <param name="tangoObject">Tango object.</param>
		public void Register(System.Object tangoObject)
		{
			if(m_enableMotionTracking)
			{
				ITangoPose poseHandler = tangoObject as ITangoPose;

				if(poseHandler != null)
				{
                    RegisterOnTangoPoseEvent(poseHandler.OnTangoPoseAvailable);
				}
			}

			if(m_enableDepth)
			{
                ITangoDepth depthHandler = tangoObject as ITangoDepth;

                if(depthHandler != null)
                {
                    RegisterOnTangoDepthEvent(depthHandler.OnTangoDepthAvailable);
                }
			}
            
            if(m_enableVideoOverlay)
            {
                if(m_useExperimentalVideoOverlay)
                {
                    IExperimentalTangoVideoOverlay videoOverlayHandler = tangoObject as IExperimentalTangoVideoOverlay;

                    if(videoOverlayHandler != null)
                    {
                        RegisterOnExperimentalTangoVideoOverlay(videoOverlayHandler.OnExperimentalTangoImageAvailable);
                    }
                }
                else
                {
                    ITangoVideoOverlay videoOverlayHandler = tangoObject as ITangoVideoOverlay;
                    
                    if(videoOverlayHandler != null)
                    {
                        UnregisterOnTangoVideoOverlay(videoOverlayHandler.OnTangoImageAvailableEventHandler);
                    }
                }
            }
		}

        /// <summary>
        /// Unregister the specified tangoObject.
        /// </summary>
        /// <param name="tangoObject">Tango object.</param>
        public void Unregister(System.Object tangoObject)
        {
            if(m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;
                
                if(poseHandler != null)
                {
                    UnregisterOnTangoPoseEvent(poseHandler.OnTangoPoseAvailable);
                }
            }
            
            if(m_enableDepth)
            {
                ITangoDepth depthHandler = tangoObject as ITangoDepth;
                
                if(depthHandler != null)
                {
                    UnregisterOnTangoDepthEvent(depthHandler.OnTangoDepthAvailable);
                }
            }

            if(m_enableVideoOverlay)
            {
                if(m_useExperimentalVideoOverlay)
                {
                    IExperimentalTangoVideoOverlay videoOverlayHandler = tangoObject as IExperimentalTangoVideoOverlay;
                    
                    if(videoOverlayHandler != null)
                    {
                        UnregisterOnExperimentalTangoVideoOverlay(videoOverlayHandler.OnExperimentalTangoImageAvailable);
                    }
                }
                else
                {
                    ITangoVideoOverlay videoOverlayHandler = tangoObject as ITangoVideoOverlay;
                    
                    if(videoOverlayHandler != null)
                    {
                        UnregisterOnTangoVideoOverlay(videoOverlayHandler.OnTangoImageAvailableEventHandler);
                    }
                }
            }
        }
        
        /// <summary>
        /// Registers the on tango pose event.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoPoseEvent(OnTangoPoseAvailableEventHandler handler)
        {
            if(m_poseListener != null)
            {
                m_poseListener.RegisterTangoPoseAvailable(handler);
            }
        }

        /// <summary>
        /// Registers the on tango depth event.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoDepthEvent(OnTangoDepthAvailableEventHandler handler)
        {
            if(m_depthListener != null)
            {
                m_depthListener.RegisterOnTangoDepthAvailable(handler);
            }
        }

        /// <summary>
        /// Registers the on tango event.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoEvent(OnTangoEventAvailableEventHandler handler)
        {
            if(m_tangoEventListener != null)
            {
                m_tangoEventListener.RegisterOnTangoEventAvailable(handler);
            }
        }

        /// <summary>
        /// Registers the on tango video overlay.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoVideoOverlay(OnTangoImageAvailableEventHandler handler)
        {
            if(m_videoOverlayListener != null)
            {
                m_videoOverlayListener.RegisterOnTangoImageAvailable(handler);
            }
        }

        /// <summary>
        /// Registers the on experimental tango video overlay.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnExperimentalTangoVideoOverlay(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if(m_videoOverlayListener != null)
            {
                m_videoOverlayListener.RegisterOnExperimentalTangoImageAvailable(handler);
            }
        }

		/// <summary>
		/// Determines if has requested permissions.
		/// </summary>
		/// <returns><c>true</c> if has requested permissions; otherwise, <c>false</c>.</returns>
		public bool HasRequestedPermissions()
		{
			return (m_requiredPermissions == PermissionsTypes.NONE);
		}

		/// <summary>
		/// Registers the permissions callback.
		/// </summary>
		/// <param name="permissionsEventHandler">Permissions event handler.</param>
		public void RegisterPermissionsCallback(PermissionsEvent permissionsEventHandler)
		{
			if(permissionsEventHandler != null)
			{
				m_permissionEvent += permissionsEventHandler;
			}
		}

        /// <summary>
        /// Unregisters the on tango pose event.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoPoseEvent(OnTangoPoseAvailableEventHandler handler)
        {
            if(m_poseListener != null)
            {
                m_poseListener.UnregisterTangoPoseAvailable(handler);
            }
        }

        /// <summary>
        /// Unregisters the on tango depth event.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoDepthEvent(OnTangoDepthAvailableEventHandler handler)
        {
            if(m_depthListener != null)
            {
                m_depthListener.UnregisterOnTangoDepthAvailable(handler);
            }
        }
        
        /// <summary>
        /// Unregisters the on tango event.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoEvent(OnTangoEventAvailableEventHandler handler)
        {
            if(m_tangoEventListener != null)
            {
                m_tangoEventListener.UnregisterOnTangoEventAvailable(handler);
            }
        }

        /// <summary>
        /// Unregisters the on tango video overlay.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoVideoOverlay(OnTangoImageAvailableEventHandler handler)
        {
            if(m_videoOverlayListener != null)
            {
                m_videoOverlayListener.UnregisterOnTangoImageAvailable(handler);
            }
        }
        
        /// <summary>
        /// Unregisters the on experimental tango video overlay.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnExperimentalTangoVideoOverlay(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if(m_videoOverlayListener != null)
            {
                m_videoOverlayListener.UnregisterOnExperimentalTangoImageAvailable(handler);
            }
        }

		/// <summary>
		/// Removes the permissions callback.
		/// </summary>
		/// <param name="permissionsEventHandler">Permissions event handler.</param>
		public void RemovePermissionsCallback(PermissionsEvent permissionsEventHandler)
		{
			if(permissionsEventHandler != null)
			{
				m_permissionEvent -= permissionsEventHandler;
			}
		}

		
		/// <summary>
		/// Requests the necessary permissions for Tango functionality.
		/// </summary>
		public void RequestNecessaryPermissionsAndConnect()
		{
			_ResetPermissionsFlags();
			_RequestNextPermission();
		}
		
		/// <summary>
		/// Initialize Tango Service and Config.
		/// </summary>
		public void InitApplication()
		{
			Debug.Log("-----------------------------------Initializing Tango");
			_TangoInitialize();
			TangoConfig.InitConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_DEFAULT);
		}
		
		/// <summary>
		/// Initialize the providers.
		/// </summary>
		/// <param name="UUID"> UUID to be loaded, if any.</param>
		public void InitProviders(string UUID)
		{
			_InitializeMotionTracking(UUID);
			_InitializeDepth();
			//_InitializeOverlay();
			_SetEventCallbacks();
		}
		
		/// <summary>
		/// Connects to Tango Service.
		/// </summary>
		public void ConnectToService()
		{
			Debug.Log("TangoApplication.ConnectToService()");
			_TangoConnect();
		}

		/// <summary>
		/// Shutdown this instance.
		/// </summary>
		public void Shutdown()
		{
			Debug.Log("Tango Shutdown");
			TangoConfig.Free();
			_TangoDisconnect();
		}

		/// <summary>
		/// Helper method that will resume the tango services on App Resume.
		/// Locks the config again and connects the service.
		/// </summary>
		private void _ResumeTangoServices()
		{
			RequestNecessaryPermissionsAndConnect();
		}
		
		/// <summary>
		/// Helper method that will suspend the tango services on App Suspend.
		/// Unlocks the tango config and disconnects the service.
		/// </summary>
		private void _SuspendTangoServices()
		{
			Debug.Log("Suspending Tango Service");
			_TangoDisconnect();
		}

		/// <summary>
		/// Set callbacks on all PoseListener objects.
		/// </summary>
		/// <param name="framePairs">Frame pairs.</param>
        private void _SetMotionTrackingCallbacks(TangoCoordinateFramePair[] framePairs)
        {
            if(m_poseListener != null)
            {
                m_poseListener.AutoReset = m_motionTrackingAutoReset;
                m_poseListener.SetCallback(framePairs);
            }
        }

		/// <summary>
		/// Set callbacks for all DepthListener objects.
		/// </summary>
        private void _SetDepthCallbacks()
        {
            if (m_depthListener != null)
            {
                m_depthListener.SetCallback();
            }
        }

        /// <summary>
        /// Set callbacks for all TangoEventListener objects.
        /// </summary>
        private void _SetEventCallbacks()
        {
            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.SetCallback();
            }
        }

		/// <summary>
		/// Set callbacks for all VideoOverlayListener objects.
		/// </summary>
		private void _SetVideoOverlayCallbacks()
		{
            if(m_videoOverlayListener != null)
            {
                m_videoOverlayListener.SetCallback(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, true, m_videoOverlayTexture);
            }
		}
		
		/// <summary>
		/// Initialize motion tracking.
		/// </summary>
		private void _InitializeMotionTracking(string UUID)
        {
			System.Collections.Generic.List<TangoCoordinateFramePair> framePairs = new System.Collections.Generic.List<TangoCoordinateFramePair>();
			if (TangoConfig.SetBool(TangoConfig.Keys.ENABLE_AREA_LEARNING_BOOL, m_enableAreaLearning) && m_enableAreaLearning)
			{
				Debug.Log("Area Learning is enabled.");
                if(!string.IsNullOrEmpty(UUID))
                {
                    TangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, UUID);
                    TangoConfig.SetBool("config_experimental_high_accuracy_small_scale_adf", m_useExperimentalADF);
                }
                
                TangoCoordinateFramePair areaDescription;
                areaDescription.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                areaDescription.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                
                TangoCoordinateFramePair startToADF;
                startToADF.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                startToADF.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                
                framePairs.Add(areaDescription);
                framePairs.Add(startToADF);
			}

            
            if (TangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_BOOL, m_enableMotionTracking) && m_enableMotionTracking)
            {
				TangoCoordinateFramePair motionTracking;
				motionTracking.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
				motionTracking.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
				framePairs.Add(motionTracking);
            }

			if(framePairs.Count > 0)
			{
				_SetMotionTrackingCallbacks(framePairs.ToArray());
			}
            
			TangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL, m_motionTrackingAutoReset);
        }

        /// <summary>
        /// Initialize depth perception.
        /// </summary>
        private void _InitializeDepth()
        {
            if (TangoConfig.SetBool(TangoConfig.Keys.ENABLE_DEPTH_PERCEPTION_BOOL, m_enableDepth) && m_enableDepth)
            {
                _SetDepthCallbacks();
            }
			bool depthConfigValue = false;
			TangoConfig.GetBool(TangoConfig.Keys.ENABLE_DEPTH_PERCEPTION_BOOL, ref depthConfigValue);
			Debug.Log("TangoConfig bool for key: " + TangoConfig.Keys.ENABLE_DEPTH_PERCEPTION_BOOL
			          + " has value set of: " + depthConfigValue);
        }

        /// <summary>
        /// Initialize the RGB overlay.
        /// </summary>
        private void _InitializeOverlay()
        {
			_SetVideoOverlayCallbacks();
        }
        
		/// <summary>
		/// Initialize the Tango Service.
		/// </summary>
        private void _TangoInitialize()
        {
			if(_IsValidTangoAPIVersion())
			{
				int status = TangoServiceAPI.TangoService_initialize( IntPtr.Zero, IntPtr.Zero);
	            if (status != Common.ErrorType.TANGO_SUCCESS)
	            {
					Debug.Log("-------------------Tango initialize status : " + status);
					Debug.Log(CLASS_NAME + ".Initialize() The service has not been initialized!");
	            }
	            else
	            {
					Debug.Log(CLASS_NAME + ".Initialize() Tango was initialized!");
	            }
			}
			else
			{
				Debug.Log(CLASS_NAME + ".Initialize() Invalid API version. please update to minimul API version.");
			}
        }
        
		/// <summary>
		/// Connect to the Tango Service.
		/// </summary>
        private void _TangoConnect()
        {
			if(!m_isServiceConnected)
			{
				m_isServiceConnected = true;
				AndroidHelper.PerformanceLog("Unity _TangoConnect start");
				if (TangoServiceAPI.TangoService_connect(m_callbackContext, TangoConfig.GetConfig()) != Common.ErrorType.TANGO_SUCCESS)
	            {
					Debug.Log(CLASS_NAME + ".Connect() Could not connect to the Tango Service!");
	            }
	            else
				{
					AndroidHelper.PerformanceLog("Unity _TangoConnect end");
					Debug.Log(CLASS_NAME + ".Connect() Tango client connected to service!");
	            }
			}
        }
        
		/// <summary>
		/// Disconnect from the Tango Service.
		/// </summary>
        private void _TangoDisconnect()
		{
			Debug.Log(CLASS_NAME + ".Disconnect() Disconnecting from the Tango Service");
			m_isDisconnecting = true;
			m_isServiceConnected = false;
            if (TangoServiceAPI.TangoService_disconnect() != Common.ErrorType.TANGO_SUCCESS)
            {
				Debug.Log(CLASS_NAME + ".Disconnect() Could not disconnect from the Tango Service!");
				m_isDisconnecting = false;
            }
            else
            {
				Debug.Log(CLASS_NAME + ".Disconnect() Tango client disconnected from service!");
				m_isDisconnecting = false;
            }
        }

		/// <summary>
		/// Checks to see if the current Tango Service is supported.
		/// </summary>
		/// <returns><c>true</c>, if is valid tango API version is greater
		/// than or equal to the minimum supported version, <c>false</c> otherwise.</returns>
		private bool _IsValidTangoAPIVersion()
		{
			if(!m_hasVersionBeenChecked)
			{
				int versionCode = _GetTangoAPIVersion();
				if(versionCode < 0)
				{
					m_isValidTangoAPIVersion = false;
				}
				else
				{
					m_isValidTangoAPIVersion = (versionCode >= MINIMUM_API_VERSION);
				}
				
				m_hasVersionBeenChecked = true;
			}
			
			return m_isValidTangoAPIVersion;
		}

		/// <summary>
		/// Gets the get tango API version code.
		/// </summary>
		/// <returns>The get tango API version code.</returns>
		private static int _GetTangoAPIVersion()
		{
			return AndroidHelper.GetVersionCode("com.projecttango.tango");
		}

		/// <summary>
		/// Android on pause.
		/// </summary>
		private void _androidOnPause()
		{
			if(m_isServiceConnected && m_requiredPermissions == PermissionsTypes.NONE)
			{
				Debug.Log ("Pausing services");
				m_shouldReconnectService = true;
				_SuspendTangoServices();
			}
			Debug.Log("androidOnPause done");
		}

		/// <summary>
		/// Android on resume.
		/// </summary>
		private void _androidOnResume()
		{
			if(m_shouldReconnectService)
			{
				Debug.Log ("Resuming services");
				m_shouldReconnectService = false;
				_ResumeTangoServices();
			}
			Debug.Log ("androidOnResume done");
		}

		/// <summary>
		/// EventHandler for Android's on activity result.
		/// </summary>
		/// <param name="requestCode">Request code.</param>
		/// <param name="resultCode">Result code.</param>
		/// <param name="data">Data.</param>
		private void _androidOnActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
		{
			Debug.Log("Activity returned result code : " + resultCode);

            switch (requestCode)
            {
                case Common.TANGO_MOTION_TRACKING_PERMISSIONS_REQUEST_CODE:
                {
                    if(resultCode == (int)Common.AndroidResult.SUCCESS)
                    {
                        _FlipBitAndCheckPermissions(PermissionsTypes.MOTION_TRACKING);
                    }
                    else
                    {
                        _PermissionWasDenied();
                    }
                    break;
                }
                case Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE:
                {
                    if(resultCode == (int)Common.AndroidResult.SUCCESS)
                    {
                        _FlipBitAndCheckPermissions(PermissionsTypes.AREA_LEARNING);
                    }
                    else
                    {
                        _PermissionWasDenied();
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
            Debug.Log("Activity returned result end");
		}

        /// <summary>
        /// Start exceptions listener.
        /// </summary>
        /// <returns>The start exceptions listener.</returns>
        private IEnumerator _StartExceptionsListener()
        {
            AndroidHelper.ShowStandardTangoExceptionsUI();
            
            while (!AndroidHelper.FindTangoExceptionsUILayout())
            {
                yield return 0;
            }
            
            AndroidHelper.SetTangoExceptionsListener();
        }

		/// <summary>
		/// Awake this instance.
		/// </summary>
		private void Awake()
        {
			AndroidHelper.RegisterPauseEvent(_androidOnPause);
			AndroidHelper.RegisterResumeEvent(_androidOnResume);
			AndroidHelper.RegisterOnActivityResultEvent(_androidOnActivityResult);

            if(m_enableMotionTracking)
            {
                m_poseListener = new PoseListener();
            }

            if(m_enableDepth)
            {
                m_depthListener = new DepthListener();
            }

            if(m_enableUXLibrary)
            {
                m_tangoEventListener = new TangoEventListener();
            }

            if(m_enableVideoOverlay)
            {
                m_videoOverlayTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
                m_videoOverlayTexture.Apply();

                m_videoOverlayListener = new VideoOverlayListener();
            }
		}

		/// <summary>
		/// Reset permissions flags.
		/// </summary>
		private void _ResetPermissionsFlags()
		{
			if(m_requiredPermissions == PermissionsTypes.NONE)
			{
				m_requiredPermissions |= m_enableMotionTracking ? PermissionsTypes.MOTION_TRACKING : PermissionsTypes.NONE;
				m_requiredPermissions |= (m_enableAreaLearning | m_enableADFSaveLoad) ? PermissionsTypes.AREA_LEARNING : PermissionsTypes.NONE;
			}
		}

        /// <summary>
        /// Flip a permission bit and check to see if all permissions were accepted.
        /// </summary>
        /// <param name="permission">Permission.</param>
        private void _FlipBitAndCheckPermissions(PermissionsTypes permission)
        {
            m_requiredPermissions ^= permission;
            
            if(m_requiredPermissions == 0) // all permissions are good!
            {
                Debug.Log("All permissions have been accepted!");
                _SendPermissionEvent(true);
            }
            else
            {
                _RequestNextPermission();
            }
        }

        /// <summary>
        /// A Tango permission was denied.
        /// </summary>
        private void _PermissionWasDenied()
        {
            m_requiredPermissions = PermissionsTypes.NONE;
            if(m_permissionEvent != null)
            {
                _SendPermissionEvent(false);
            }
        }
        
        /// <summary>
        /// Request next permission.
        /// </summary>
        private void _RequestNextPermission()
        {
            Debug.Log("TangoApplication._RequestNextPermission()");

			// if no permissions are needed let's kick-off the Tango connect
			if(m_requiredPermissions == PermissionsTypes.NONE)
            {
                _SendPermissionEvent(true);
			}

            if((m_requiredPermissions & PermissionsTypes.MOTION_TRACKING) == PermissionsTypes.MOTION_TRACKING)
			{
				if(AndroidHelper.ApplicationHasTangoPermissions(Common.TANGO_MOTION_TRACKING_PERMISSIONS))
				{
					_androidOnActivityResult(Common.TANGO_MOTION_TRACKING_PERMISSIONS_REQUEST_CODE, -1, null);
				}
				else
				{
					AndroidHelper.StartTangoPermissionsActivity(Common.TANGO_MOTION_TRACKING_PERMISSIONS);
				}
			}
			else if((m_requiredPermissions & PermissionsTypes.AREA_LEARNING) == PermissionsTypes.AREA_LEARNING)
			{
				if(AndroidHelper.ApplicationHasTangoPermissions(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS))
				{
					_androidOnActivityResult(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE, -1, null);
                }
				else
				{
					AndroidHelper.StartTangoPermissionsActivity(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS);
				}
			}
		}

        /// <summary>
        /// Sends the permission event.
        /// </summary>
        /// <param name="permissions">If set to <c>true</c> permissions.</param>
        private void _SendPermissionEvent(bool permissions)
        {
            if (m_enableUXLibrary && permissions)
            {
                StartCoroutine(_StartExceptionsListener());
            }

            m_sendPermissions = true;
            m_permissionsSuccessful = permissions;
        }

        /// <summary>
        /// Disperse any events related to Tango functionality.
        /// </summary>
        private void Update()
        {
            if(m_sendPermissions)
            {
                _InitializeOverlay();
                if(m_permissionEvent != null)
                {
                    m_permissionEvent(m_permissionsSuccessful);
                }
                m_sendPermissions = false;
            }

            if(m_poseListener != null)
            {
                m_poseListener.SendPoseIfAvailable(m_enableUXLibrary);
            }

            if(m_tangoEventListener != null)
            {
                m_tangoEventListener.SendIfTangoEventAvailable(m_enableUXLibrary);
            }

            if(m_depthListener != null)
            {
                m_depthListener.SendDepthIfAvailable();
            }

            if(m_videoOverlayListener != null)
            {
                m_videoOverlayListener.SendIfVideoOverlayAvailable();
            }
        }

        #region NATIVE_FUNCTIONS
        /// <summary>
        /// Interface for native function calls to Tango Service.
        /// </summary>
        private struct TangoServiceAPI
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
			public static extern int TangoService_initialize (IntPtr JNIEnv, IntPtr appContext);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connect (IntPtr callbackContext, IntPtr config);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_disconnect ();
            #else
            public static int TangoService_initialize(IntPtr JNIEnv, IntPtr appContext)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
			public static  int TangoService_connect (IntPtr callbackContext, IntPtr config)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            public static int TangoService_disconnect ()
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            #endif
        }
        #endregion // NATIVE_FUNCTIONS
    }
}
