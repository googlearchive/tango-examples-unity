//-----------------------------------------------------------------------
// <copyright file="TangoApplication.cs" company="Google">
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
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Delegate for permission callbacks.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public delegate void PermissionsEvent(bool permissionsGranted);

    /// <summary>
    /// Delegate for service connection.
    /// </summary>
    public delegate void OnTangoConnectEventHandler();

    /// <summary>
    /// Delegate for service disconnection.
    /// </summary>
    public delegate void OnTangoDisconnectEventHandler();

    /// <summary>
    /// Main entry point for the Tango Service.
    /// 
    /// This component handles nearly all communication with the underlying TangoService.  You must have one of these
    /// in your scene for Tango to work.  Customization of the Tango connection can be done in the Unity editor or by
    /// programatically setting the member flags.
    /// 
    /// This sends out events to Components that derive from the ITangoPose, ITangoDepth, etc. interfaces and
    /// register themselves via Register.  This also sends out events to callbacks passed in through 
    /// RegisterOnTangoConnect, RegisterOnTangoDisconnect, and RegisterPermissionsCallback.
    /// 
    /// Note: To connect to the Tango Service, you should call InitApplication after properly registering everything.
    /// </summary>
    public class TangoApplication : MonoBehaviour
    {   
        /// <summary>
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
        public bool m_useExperimentalVideoOverlay = true;
        public bool m_useExperimentalADF = false;
#if UNITY_EDITOR
        public static bool m_mouseEmulationViaPoseUpdates = false;
#endif
        private const string CLASS_NAME = "TangoApplication";
        private const string ANDROID_PRO_LABEL_TEXT = "<size=30>Tango plugin requires Unity Android Pro!</size>";
        private const float ANDROID_PRO_LABEL_PERCENT_X = 0.5f;
        private const float ANDROID_PRO_LABEL_PERCENT_Y = 0.5f;
        private const float ANDROID_PRO_LABEL_WIDTH = 200.0f;
        private const float ANDROID_PRO_LABEL_HEIGHT = 200.0f;
        private const string DEFAULT_AREA_DESCRIPTION = "/sdcard/defaultArea";
        private const string MOTION_TRACKING_LOG_PREFIX = "Motion tracking mode : ";
        private const int MINIMUM_API_VERSION = 1978;
        private static string m_tangoServiceVersion = string.Empty;

        /// <summary>
        /// Occurs when permission event.
        /// </summary>
        private event PermissionsEvent PermissionEvent;

        /// <summary>
        /// Occurs when on tango connect.
        /// </summary>
        private event OnTangoConnectEventHandler OnTangoConnect;

        /// <summary>
        /// Occurs when on tango disconnect.
        /// </summary>
        private event OnTangoDisconnectEventHandler OnTangoDisconnect;

        private PermissionsTypes m_requiredPermissions = 0;
        private static bool m_isValidTangoAPIVersion = false;
        private static bool m_hasVersionBeenChecked = false;
        private DepthProvider m_depthProvider;
        private IntPtr m_callbackContext = IntPtr.Zero;
        private bool m_isServiceConnected = false;
        private bool m_shouldReconnectService = false;
        private bool m_sendPermissions = false;
        private bool m_permissionsSuccessful = false;
        private PoseListener m_poseListener;
        private DepthListener m_depthListener;
        private VideoOverlayListener m_videoOverlayListener;
        private TangoEventListener m_tangoEventListener;
        private YUVTexture m_yuvTexture;

        /// <summary>
        /// Get the Tango service version name.
        /// </summary>
        /// <returns>String for the version name.</returns>
        public static string GetTangoServiceVersion()
        {
            if (m_tangoServiceVersion == string.Empty)
            {
                m_tangoServiceVersion = AndroidHelper.GetVersionName("com.projecttango.tango");
            }
            
            return m_tangoServiceVersion;
        }

        /// <summary>
        /// Get the video overlay texture.
        /// </summary>
        /// <returns>The video overlay texture.</returns>
        public YUVTexture GetVideoOverlayTextureYUV()
        {
            return m_yuvTexture;
        }

        /// <summary>
        /// Register to get Tango callbacks.
        /// 
        /// The object should derive from one of ITangoDepth, ITangoEvent, ITangoPos, ITangoVideoOverlay, or
        /// ITangoExperimentalTangoVideoOverlay.  You will get callback during Update until you unregister.
        /// </summary>
        /// <param name="tangoObject">Object to get Tango callbacks from.</param>
        public void Register(System.Object tangoObject)
        {
            ITangoEvent tangoEvent = tangoObject as ITangoEvent;
            
            if (tangoEvent != null)
            {
                RegisterOnTangoEvent(tangoEvent.OnTangoEventAvailableEventHandler);
            }

            if (m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;

                if (poseHandler != null)
                {
                    RegisterOnTangoPoseEvent(poseHandler.OnTangoPoseAvailable);
                }
            }

            if (m_enableDepth)
            {
                ITangoDepth depthHandler = tangoObject as ITangoDepth;

                if (depthHandler != null)
                {
                    RegisterOnTangoDepthEvent(depthHandler.OnTangoDepthAvailable);
                }
            }
            
            if (m_enableVideoOverlay)
            {
                if (m_useExperimentalVideoOverlay)
                {
                    IExperimentalTangoVideoOverlay videoOverlayHandler = tangoObject as IExperimentalTangoVideoOverlay;

                    if (videoOverlayHandler != null)
                    {
                        RegisterOnExperimentalTangoVideoOverlay(videoOverlayHandler.OnExperimentalTangoImageAvailable);
                    }
                } 
                else
                {
                    ITangoVideoOverlay videoOverlayHandler = tangoObject as ITangoVideoOverlay;
                    
                    if (videoOverlayHandler != null)
                    {
                        RegisterOnTangoVideoOverlay(videoOverlayHandler.OnTangoImageAvailableEventHandler);
                    }
                }
            }
        }

        /// <summary>
        /// Unregister from Tango callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="tangoObject">Object to stop getting Tango callbacks from.</param>
        public void Unregister(System.Object tangoObject)
        {
            ITangoEvent tangoEvent = tangoObject as ITangoEvent;
            
            if (tangoEvent != null)
            {
                UnregisterOnTangoEvent(tangoEvent.OnTangoEventAvailableEventHandler);
            }

            if (m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;
                
                if (poseHandler != null)
                {
                    UnregisterOnTangoPoseEvent(poseHandler.OnTangoPoseAvailable);
                }
            }
            
            if (m_enableDepth)
            {
                ITangoDepth depthHandler = tangoObject as ITangoDepth;
                
                if (depthHandler != null)
                {
                    UnregisterOnTangoDepthEvent(depthHandler.OnTangoDepthAvailable);
                }
            }

            if (m_enableVideoOverlay)
            {
                if (m_useExperimentalVideoOverlay)
                {
                    IExperimentalTangoVideoOverlay videoOverlayHandler = tangoObject as IExperimentalTangoVideoOverlay;
                    
                    if (videoOverlayHandler != null)
                    {
                        UnregisterOnExperimentalTangoVideoOverlay(videoOverlayHandler.OnExperimentalTangoImageAvailable);
                    }
                }
                else
                {
                    ITangoVideoOverlay videoOverlayHandler = tangoObject as ITangoVideoOverlay;
                    
                    if (videoOverlayHandler != null)
                    {
                        UnregisterOnTangoVideoOverlay(videoOverlayHandler.OnTangoImageAvailableEventHandler);
                    }
                }
            }
        }

        /// <summary>
        /// Check if all requested permissions have been granted.
        /// </summary>
        /// <returns><c>true</c> if all requested permissions were granted; otherwise, <c>false</c>.</returns>
        public bool HasRequestedPermissions()
        {
            return m_requiredPermissions == PermissionsTypes.NONE;
        }

        /// <summary>
        /// Register to get an event callback when all permissions are granted.
        /// 
        /// The passed event will get called once all Tango permissions have been granted.  Registering 
        /// after all permissions have already been granted will cause the event to never fire.
        /// </summary>
        /// <param name="permissionsEventHandler">Event to call.</param>
        public void RegisterPermissionsCallback(PermissionsEvent permissionsEventHandler)
        {
            if (permissionsEventHandler != null)
            {
                PermissionEvent += permissionsEventHandler;
            }
        }

        /// <summary>
        /// Unregister from the permission callbacks.
        /// 
        /// See TangoApplication.RegisterPermissionsCallback for more details.
        /// </summary>
        /// <param name="permissionsEventHandler">Event to remove.</param>
        public void UnregisterPermissionsCallback(PermissionsEvent permissionsEventHandler)
        {
            if (permissionsEventHandler != null)
            {
                PermissionEvent -= permissionsEventHandler;
            }
        }

        /// <summary>
        /// DEPRECATED: Unregister from the permission callbacks.
        /// 
        /// See TangoApplication.RegisterPermissionsCallback for more details.
        /// </summary>
        /// <param name="permissionsEventHandler">Event to remove.</param>
        public void RemovePermissionsCallback(PermissionsEvent permissionsEventHandler)
        {
            if (permissionsEventHandler != null)
            {
                PermissionEvent -= permissionsEventHandler;
            }
        }

        /// <summary>
        /// Register to get an event callback when connected to the Tango service.
        /// 
        /// The passed event will get called once connected to the Tango service.  Registering 
        /// after already connected will cause the event to not fire until disconnected and then
        /// connecting again.
        /// </summary>
        /// <param name="handler">Event to call.</param>
        public void RegisterOnTangoConnect(OnTangoConnectEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoConnect += handler;
            }
        }

        /// <summary>
        /// Unregister from the callback when connected to the Tango service.
        /// 
        /// See TangoApplication.RegisterOnTangoConnect for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        public void UnregisterOnTangoConnect(OnTangoConnectEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoConnect -= handler;
            }
        }

        /// <summary>
        /// Register to get an event callback when disconnected from the Tango service.
        /// 
        /// The passed event will get called when disconnected from the Tango service.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        public void RegisterOnTangoDisconnect(OnTangoDisconnectEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoDisconnect += handler;
            }
        }

        /// <summary>
        /// Unregister from the callback when disconnected from the Tango service.
        /// 
        /// See TangoApplication.RegisterOnTangoDisconnect for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        public void UnregisterOnTangoDisconnect(OnTangoDisconnectEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoDisconnect -= handler;
            }
        }

        /// <summary>
        /// Init step 1.  Call this to request Tango permissions.
        /// 
        /// After setting up the necessary permissions and callbacks, call this to request each of
        /// the permissions in order.  Once all the permissions are granted, the permission callback
        /// will get called to do the next step.
        /// 
        /// Also see TangoApplication.InitApplication, TangoApplication.InitProviders, and 
        /// TangoApplication.ConnectToService.
        /// </summary>
        public void RequestNecessaryPermissionsAndConnect()
        {
            _ResetPermissionsFlags();
            _RequestNextPermission();
        }
        
        /// <summary>
        /// Init step 2.  Call this to initialize interal state on TangoApplication.
        /// 
        /// Call this in the permissions callback if all permissions have been granted.
        /// 
        /// Also see TangoApplication.RequestNecessaryPermissionsandConnect, TangoApplication.InitProviders, and
        /// TangoApplication.ConnectToService.
        /// </summary>
        public void InitApplication()
        {
            Debug.Log("-----------------------------------Initializing Tango");
            _TangoInitialize();
            TangoConfig.InitConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_DEFAULT);

            if (m_enableVideoOverlay && m_useExperimentalVideoOverlay)
            {
                int yTextureWidth = 0;
                int yTextureHeight = 0;
                int uvTextureWidth = 0;
                int uvTextureHeight = 0;
                
                TangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_Y_TEXTURE_WIDTH, ref yTextureWidth);
                TangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_Y_TEXTURE_HEIGHT, ref yTextureHeight);
                TangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_UV_TEXTURE_WIDTH, ref uvTextureWidth);
                TangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_UV_TEXTURE_HEIGHT, ref uvTextureHeight);
                
                if (yTextureWidth == 0 || yTextureHeight == 0 || uvTextureWidth == 0 || uvTextureHeight == 0)
                {
                    Debug.Log("Video overlay texture sizes were not set properly");
                }

                m_yuvTexture.ResizeAll(yTextureWidth, yTextureHeight, uvTextureWidth, uvTextureHeight);
            }
        }
        
        /// <summary>
        /// Init step 3.  Call this to choose what area description ID to use, if any.
        /// 
        /// Call this in the permissions callback after calling TangoApplication.InitApplication.
        /// 
        /// Also see TangoApplication.RequestNecessaryPermissionsAndConnect, TangoApplication.InitApplication, and
        /// TangoApplication.ConnectToService.
        /// </summary>
        /// <param name="uuid">Area description ID to load, or <c>string.Empty</c> to not use any.</param>
        public void InitProviders(string uuid)
        {
            _InitializeMotionTracking(uuid);
            _InitializeDepth();
            _InitializeOverlay();
            _SetEventCallbacks();
        }
        
        /// <summary>
        /// Init step 4.  Call this to connect to the Tango service.
        /// 
        /// Also see TangoApplication.RequestNecessaryPermissionsAndConnect, TangoApplication.InitApplication, 
        /// and TangoApplication.InitProviders.
        /// </summary>
        public void ConnectToService()
        {
            Debug.Log("TangoApplication.ConnectToService()");
            _TangoConnect();
        }

        /// <summary>
        /// Disconnect from the Tango service.
        /// 
        /// This is called automatically when the TangoApplication goes away.  You only need 
        /// to call this to disconnect from the Tango service before the TangoApplication goes
        /// away.
        /// </summary>
        public void Shutdown()
        {
            Debug.Log("Tango Shutdown");
            TangoConfig.Free();
            _TangoDisconnect();
        }

        /// <summary>
        /// Register to get Tango pose callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Handler.</param>
        internal void RegisterOnTangoPoseEvent(OnTangoPoseAvailableEventHandler handler)
        {
            if (m_poseListener != null)
            {
                m_poseListener.RegisterTangoPoseAvailable(handler);
            }
        }

        /// <summary>
        /// Unregister from the Tango pose callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        internal void UnregisterOnTangoPoseEvent(OnTangoPoseAvailableEventHandler handler)
        {
            if (m_poseListener != null)
            {
                m_poseListener.UnregisterTangoPoseAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango depth callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Object to stop getting Tango callbacks from.</param>
        internal void RegisterOnTangoDepthEvent(OnTangoDepthAvailableEventHandler handler)
        {
            if (m_depthListener != null)
            {
                m_depthListener.RegisterOnTangoDepthAvailable(handler);
            }
        }

        /// <summary>
        /// Unregister from the Tango depth callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        internal void UnregisterOnTangoDepthEvent(OnTangoDepthAvailableEventHandler handler)
        {
            if (m_depthListener != null)
            {
                m_depthListener.UnregisterOnTangoDepthAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="handler">Object to stop getting Tango callbacks from.</param>
        internal void RegisterOnTangoEvent(OnTangoEventAvailableEventHandler handler)
        {
            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.RegisterOnTangoEventAvailable(handler);
            }
        }

        /// <summary>
        /// Unregister from the Tango event callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        internal void UnregisterOnTangoEvent(OnTangoEventAvailableEventHandler handler)
        {
            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.UnregisterOnTangoEventAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango video overlay callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="handler">Object to stop getting Tango callbacks from.</param>
        internal void RegisterOnTangoVideoOverlay(OnTangoImageAvailableEventHandler handler)
        {
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.RegisterOnTangoImageAvailable(handler);
            }
        }

        /// <summary>
        /// Unregister from the Tango video overlay callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        internal void UnregisterOnTangoVideoOverlay(OnTangoImageAvailableEventHandler handler)
        {
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.UnregisterOnTangoImageAvailable(handler);
            }
        }

        /// <summary>
        /// Experimental API only, subject to change.  Register to get Tango video overlay callbacks.
        /// </summary>
        /// <param name="handler">Object to stop getting Tango callbacks from.</param>
        internal void RegisterOnExperimentalTangoVideoOverlay(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.RegisterOnExperimentalTangoImageAvailable(handler);
            }
        }

        /// <summary>
        /// Experimental API only, subject to change.  Unregister from the Tango video overlay callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        internal void UnregisterOnExperimentalTangoVideoOverlay(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.UnregisterOnExperimentalTangoImageAvailable(handler);
            }
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
            if (m_poseListener != null)
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
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.SetCallback(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, m_useExperimentalVideoOverlay, m_yuvTexture);
            }
        }
        
        /// <summary>
        /// Initialize motion tracking.
        /// </summary>
        /// <param name="uuid">ADF UUID to load.</param>
        private void _InitializeMotionTracking(string uuid)
        {
            System.Collections.Generic.List<TangoCoordinateFramePair> framePairs = new System.Collections.Generic.List<TangoCoordinateFramePair>();
            
            if (TangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_BOOL, m_enableMotionTracking) && m_enableMotionTracking)
            {
                TangoCoordinateFramePair motionTracking;
                motionTracking.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                motionTracking.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                framePairs.Add(motionTracking);
                
                if (TangoConfig.SetBool(TangoConfig.Keys.ENABLE_AREA_LEARNING_BOOL, m_enableAreaLearning) && m_enableAreaLearning)
                {
                    Debug.Log("Area Learning is enabled.");
                    if (!string.IsNullOrEmpty(uuid))
                    {
                        TangoConfig.SetBool("config_experimental_high_accuracy_small_scale_adf", m_useExperimentalADF);
                        TangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, uuid);
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
            }

            if (framePairs.Count > 0)
            {
                _SetMotionTrackingCallbacks(framePairs.ToArray());
            }

            // The C API does not default this to on, but it is locked down.
            TangoConfig.SetBool(TangoConfig.Keys.ENABLE_LOW_LATENCY_IMU_INTEGRATION, true);

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
            if (_IsValidTangoAPIVersion())
            {
                int status = TangoServiceAPI.TangoService_initialize(IntPtr.Zero, IntPtr.Zero);
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
            if (!m_isServiceConnected)
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
                    
                    if (OnTangoConnect != null)
                    {
                        OnTangoConnect();
                    }
                }
            }
        }
        
        /// <summary>
        /// Disconnect from the Tango Service.
        /// </summary>
        private void _TangoDisconnect()
        {
            Debug.Log(CLASS_NAME + ".Disconnect() Disconnecting from the Tango Service");
            m_isServiceConnected = false;
            if (TangoServiceAPI.TangoService_disconnect() != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".Disconnect() Could not disconnect from the Tango Service!");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".Disconnect() Tango client disconnected from service!");

                if (OnTangoDisconnect != null)
                {
                    OnTangoDisconnect();
                }
            }
        }

        /// <summary>
        /// Checks to see if the current Tango Service is supported.
        /// </summary>
        /// <returns><c>true</c>, if is valid tango API version is greater
        /// than or equal to the minimum supported version, <c>false</c> otherwise.</returns>
        private bool _IsValidTangoAPIVersion()
        {
            if (!m_hasVersionBeenChecked)
            {
                int versionCode = _GetTangoAPIVersion();
                if (versionCode < 0)
                {
                    m_isValidTangoAPIVersion = false;
                }
                else
                {
                    m_isValidTangoAPIVersion = versionCode >= MINIMUM_API_VERSION;
                }
                
                m_hasVersionBeenChecked = true;
            }
            
            return m_isValidTangoAPIVersion;
        }

        /// <summary>
        /// Android on pause.
        /// </summary>
        private void _androidOnPause()
        {
            if (m_isServiceConnected && m_requiredPermissions == PermissionsTypes.NONE)
            {
                Debug.Log("Pausing services");
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
            if (m_shouldReconnectService)
            {
                Debug.Log("Resuming services");
                m_shouldReconnectService = false;
                _ResumeTangoServices();
            }
            Debug.Log("androidOnResume done");
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
                    if (resultCode == (int)Common.AndroidResult.SUCCESS)
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
                    if (resultCode == (int)Common.AndroidResult.SUCCESS)
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
        /// Awake this instance.
        /// </summary>
        private void Awake()
        {
            AndroidHelper.RegisterPauseEvent(_androidOnPause);
            AndroidHelper.RegisterResumeEvent(_androidOnResume);
            AndroidHelper.RegisterOnActivityResultEvent(_androidOnActivityResult);

            m_tangoEventListener = new TangoEventListener();

            if (m_enableMotionTracking)
            {
                m_poseListener = new PoseListener();
            }

            if (m_enableDepth)
            {
                m_depthListener = new DepthListener();
            }

            if (m_enableVideoOverlay)
            {
                int yTextureWidth = 0;
                int yTextureHeight = 0;
                int uvTextureWidth = 0;
                int uvTextureHeight = 0;

                m_yuvTexture = new YUVTexture(yTextureWidth, yTextureHeight, uvTextureWidth, uvTextureHeight, TextureFormat.RGBA32, false);
                m_videoOverlayListener = new VideoOverlayListener();
            }
        }

        /// <summary>
        /// Reset permissions flags.
        /// </summary>
        private void _ResetPermissionsFlags()
        {
            if (m_requiredPermissions == PermissionsTypes.NONE)
            {
                m_requiredPermissions |= m_enableMotionTracking ? PermissionsTypes.MOTION_TRACKING : PermissionsTypes.NONE;
                m_requiredPermissions |= m_enableAreaLearning ? PermissionsTypes.AREA_LEARNING : PermissionsTypes.NONE;
            }
        }

        /// <summary>
        /// Flip a permission bit and check to see if all permissions were accepted.
        /// </summary>
        /// <param name="permission">Permission.</param>
        private void _FlipBitAndCheckPermissions(PermissionsTypes permission)
        {
            m_requiredPermissions ^= permission;
            
            if (m_requiredPermissions == 0)
            {
                // all permissions are good!
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
            if (PermissionEvent != null)
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
            if (m_requiredPermissions == PermissionsTypes.NONE)
            {
                _SendPermissionEvent(true);
            }

            if ((m_requiredPermissions & PermissionsTypes.MOTION_TRACKING) == PermissionsTypes.MOTION_TRACKING)
            {
                if (AndroidHelper.ApplicationHasTangoPermissions(Common.TANGO_MOTION_TRACKING_PERMISSIONS))
                {
                    _androidOnActivityResult(Common.TANGO_MOTION_TRACKING_PERMISSIONS_REQUEST_CODE, -1, null);
                }
                else
                {
                    AndroidHelper.StartTangoPermissionsActivity(Common.TANGO_MOTION_TRACKING_PERMISSIONS);
                }
            }
            else if ((m_requiredPermissions & PermissionsTypes.AREA_LEARNING) == PermissionsTypes.AREA_LEARNING)
            {
                if (AndroidHelper.ApplicationHasTangoPermissions(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS))
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
            m_sendPermissions = true;
            m_permissionsSuccessful = permissions;
        }

        /// <summary>
        /// Disperse any events related to Tango functionality.
        /// </summary>
        private void Update()
        {
            if (m_sendPermissions)
            {
                if (PermissionEvent != null)
                {
                    PermissionEvent(m_permissionsSuccessful);
                }
                m_sendPermissions = false;
            }

            if (m_poseListener != null)
            {
                m_poseListener.SendPoseIfAvailable();
            }

            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.SendIfTangoEventAvailable();
            }

            if (m_depthListener != null)
            {
                m_depthListener.SendDepthIfAvailable();
            }

            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.SendIfVideoOverlayAvailable();
            }
        }

        /// <summary>
        /// Unity callback when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Shutdown();
        }

        #region NATIVE_FUNCTIONS
        /// <summary>
        /// Interface for native function calls to Tango Service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct TangoServiceAPI
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_initialize(IntPtr jniEnv, IntPtr appContext);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connect(IntPtr callbackContext, IntPtr config);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_disconnect();
            #else
            public static int TangoService_initialize(IntPtr jniEnv, IntPtr appContext)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_connect(IntPtr callbackContext, IntPtr config)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_disconnect()
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            #endif
        }
        #endregion // NATIVE_FUNCTIONS
    }
}
