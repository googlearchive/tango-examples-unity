//-----------------------------------------------------------------------
// <copyright file="TangoApplication.cs" company="Google">
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

namespace Tango
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Delegate for handling device display orientation change event.
    /// </summary>
    /// <param name="displayRotation">Rotation of current display. Index enum is same same as Android screen
    /// rotation standard.</param>
    /// <param name="colorCameraRotation">Rotation of current color camera sensor. Index enum is same as Android
    /// camera rotation standard.</param>
    public delegate void OnDisplayChangedEventHandler(OrientationManager.Rotation displayRotation,
                                                      OrientationManager.Rotation colorCameraRotation);

    /// <summary>
    /// Main entry point for the Tango Service.
    ///
    /// This component handles nearly all communication with the underlying TangoService.  You must have one of these
    /// in your scene for Tango to work.  Customization of the Tango connection can be done in the Unity editor or by
    /// programmatically setting the member flags.
    ///
    /// This sends out events to Components that implement ITangoPose, ITangoDepth, etc. interfaces and
    /// register themselves via Register.
    ///
    /// Note: To connect to the Tango Service, you should call <c>InitApplication</c> after properly registering
    /// everything.
    /// </summary>
    public class TangoApplication : MonoBehaviour, ITangoUX
    {
        public bool m_autoConnectToService = false;

        public bool m_allowOutOfDateTangoAPI = false;
#if UNITY_EDITOR
        /// <summary>
        /// Whether to show performance-related options for
        /// TangoApplication via the TangoInspector custom editor GUI.
        /// </summary>
        public bool m_showPerformanceOptionsInInspector = true;

        /// <summary>
        /// Whether to show emulation options for TangoApplication
        /// via the TangoInspector custom editor GUI.
        /// </summary>
        public bool m_showEmulationOptionsInInspector = false;

        /// <summary>
        /// Mesh used in emulating the physical world
        /// (e.g. depth and color camera data).
        /// </summary>
        public Mesh m_emulationEnvironment;

        /// <summary>
        /// Optional texture to be applied to emulation environment mesh.
        /// </summary>
        public Texture m_emulationEnvironmentTexture;

        /// <summary>
        /// If true, use the more performance-demanding aspects of tango emulation
        /// (Currently, depth and color camera).
        /// </summary>
        public bool m_doSlowEmulation;

        /// <summary>
        /// Whether the emulation environment mesh should be lit
        /// (For example, a scanned/generated mesh of a room with color data
        /// should not be virtually lit, because it already represents real
        /// lighting).
        /// </summary>
        public bool m_emulationVideoOverlaySimpleLighting;

        public Vector3 m_emulatedAreaDescriptionStartOffset;
#endif

        public bool m_enableMotionTracking = true;
        public bool m_motionTrackingAutoReset = true;

        [FormerlySerializedAs("m_enableADFLoading")]
        public bool m_enableAreaDescriptions = false;
        [FormerlySerializedAs("m_enableAreaLearning")]
        public bool m_areaDescriptionLearningMode = false;

        /// <summary>
        /// Toggle for experimental drift correction.
        ///
        /// Drift-corrected frames come through the Area Description reference
        /// frame and currently cannot be used when an Area Description is loaded
        /// or when Area Learning is enabled.
        ///
        /// There will be a period after Startup during which drift-corrected frames
        /// are not available.
        ///
        /// Behavior is likely to change in the future.
        /// </summary>
        public bool m_enableDriftCorrection = false;

        public bool m_enableDepth = true;

        public bool m_enableAreaLearning = false;
        public bool m_enableADFLoading = false;

        public bool m_enable3DReconstruction = false;
        public float m_3drResolutionMeters = 0.1f;
        public bool m_3drGenerateColor = false;
        public bool m_3drGenerateNormal = false;
        public bool m_3drGenerateTexCoord = false;
        public bool m_3drSpaceClearing = false;
        public bool m_3drUseAreaDescriptionPose = false;
        public int m_3drMinNumVertices = 20;
        public Tango3DReconstruction.UpdateMethod m_3drUpdateMethod = Tango3DReconstruction.UpdateMethod.PROJECTIVE;

        public bool m_enableVideoOverlay = false;
        public bool m_videoOverlayUseTextureMethod = true;

        /// <summary>
        /// DEPRECATED. Will be removed in a future SDK.
        /// </summary>
        public bool m_videoOverlayUseYUVTextureIdMethod = false;

        public bool m_videoOverlayUseByteBufferMethod = false;

        /// <summary>
        /// Whether to keep screen always awake throughout the whole application session.
        /// </summary>
        public bool m_keepScreenAwake = false;

        /// <summary>
        /// Whether to adjust the size of the application's main render buffer
        /// for performance reasons (Some Tango devices have very high-resolution
        /// displays, so this option exists as a hint that this may be necessary).
        /// </summary>
        public bool m_adjustScreenResolution = false;

        /// <summary>
        /// Target resolution to reduce resolution to when m_adjustScreenResolution is
        /// enabled. Specifies the lesser of the two dimensions (i.e. height in landscape
        /// or width in portrait mode).
        /// </summary>
        public int m_targetResolution = 1080;

        /// <summary>
        /// If true, resolution adjustment will allow adjusting to a resolution
        /// larger than the display of the current device.
        ///
        /// This is generally discouraged.
        /// </summary>
        public bool m_allowOversizedScreenResolutions = false;

        /// <summary>
        /// Inspector value for initial max point cloud size.
        /// To set point cloud max size at runtime, use SetMaxDepthPoints().
        ///
        /// A value of 0 disables the feature.
        /// </summary>
        public int m_initialPointCloudMaxPoints = 0;

        /// <summary>
        /// Event triggered when display is rotated.
        /// </summary>
        public OnDisplayChangedEventHandler OnDisplayChanged;

        internal bool m_enableCloudADF = false;

        private const string CLASS_NAME = "TangoApplication";
        private static readonly HashSet<TangoUxEnums.UxExceptionEventType> SCREEN_SLEEPABLE_UX_EXCEPTIONS = 
            new HashSet<TangoUxEnums.UxExceptionEventType> 
            {
                TangoUxEnums.UxExceptionEventType.TYPE_UNDER_EXPOSED,
                TangoUxEnums.UxExceptionEventType.TYPE_LYING_ON_SURFACE,
            };
            
        private static string m_tangoServiceVersion = string.Empty;

        /// <summary>
        /// Ratio of the screen's larger dimension (e.g. Screen.Width in landscape)
        /// to the screen's smaller dimension. Measured once, on the first time
        /// TangoApplication awakes.
        /// </summary>
        private static float m_screenLandscapeAspectRatio = -1;

        /// <summary>
        /// If RequestPermissions() has been called automatically.
        ///
        /// This only matters if m_autoConnectToService is set.
        /// </summary>
        private bool m_autoConnectRequestedPermissions = false;

        /// <summary>
        /// The connection to the Tango3DReconstruction library.
        /// </summary>
        private Tango3DReconstruction m_tango3DReconstruction;

        private PermissionsTypes m_requiredPermissions = 0;
        private PermissionRequestState m_permissionRequestState = PermissionRequestState.NONE;
        private IntPtr m_callbackContext = IntPtr.Zero;

        /// <summary>
        /// If true, the device has a compatible version of Tango Core. Otherwise false.
        /// </summary>
        private bool m_isTangoUpToDate = false;

        /// <summary>
        /// If true, the TangoService is running and providing data to client, e.g motion tracking
        /// and point cloud data. Otherwise false.
        /// </summary>
        private bool m_isTangoStarted = false;

        /// <summary>
        /// If true, the TangoApplication instance will request permission when application is resumed.
        /// Otherwise false. This is used to handle Tango Service connection when application resumed from
        /// pause.
        /// </summary>
        private bool m_shouldReconnectService = false;

        /// <summary>
        /// The last-known depth camera rate.
        /// </summary>
        private int m_appDepthCameraRate = 5;

        private YUVTexture m_yuvTexture;
        private TangoConfig m_tangoConfig;
        private TangoConfig m_tangoRuntimeConfig;

        /// <summary>
        /// Boolean value to check if application is currently paused in background.
        /// </summary>
        private bool m_isApplicationPaused = false;

        /// <summary>
        /// Lock for m_androidMessageQueue.
        /// </summary>
        private object m_messageQueueLock = new object();

        /// <summary>
        /// A message queue to pipe Android native callbacks (e.g OnResume, OnPause) to Unity main thread.
        /// </summary>
        private Queue<AndroidMessage> m_androidMessageQueue = new Queue<AndroidMessage>();

        /// <summary>
        /// Lock to protect Tango connect and disconnect.
        /// </summary>
        private object m_tangoLifecycleLock = new object();

        /// <summary>
        /// A set of unresolved ux exceptions.
        /// </summary>
        private HashSet<TangoUxEnums.UxExceptionEventType> m_unresolvedUxExceptions = new HashSet<TangoUxEnums.UxExceptionEventType>();

        /// <summary>
        /// Occurs when required android permissions have been resolved.  NOTE: this event currently only
        /// fires for the TangoUx class.
        /// </summary>
        private OnAndroidPermissionsDelegate m_androidPermissionsEvent;

        /// <summary>
        /// Occurs when all required tango permissions have been resolved.
        /// </summary>
        private OnTangoBindDelegate m_tangoBindEvent;

        /// <summary>
        /// Occurs when tango service connection is complete.
        /// </summary>
        private OnTangoConnectDelegate m_tangoConnectEvent;

        /// <summary>
        /// Occurs when on tango service disconnection is complete.
        /// </summary>
        private OnTangoDisconnectDelegate m_tangoDisconnectEvent;

        /// <summary>
        /// Delegate for handling resolution of all required android permissions.
        /// </summary>
        /// <param name="permissionsGranted"><c>true</c> if all required android permissions were granted, otherwise <c>false</c>.</param>
        private delegate void OnAndroidPermissionsDelegate(bool permissionsGranted);

        /// <summary>
        /// Delegate for handling the resolution of tango service binding.
        /// </summary>
        /// <param name="success"><c>true</c> if the tango service was bound, otherwise <c>false</c>.</param>
        private delegate void OnTangoBindDelegate(bool success);

        /// <summary>
        /// Delegate for handling service connection event.
        /// </summary>
        private delegate void OnTangoConnectDelegate();

        /// <summary>
        /// Delegate for handling service disconnection event.
        /// </summary>
        private delegate void OnTangoDisconnectDelegate();

        /// <summary>
        /// Type of Android messages. This mainly mirrors the callback from Android activity.
        /// </summary>
        private enum AndroidMessageType
        {
            NONE,
            ON_PAUSE,
            ON_RESUME,
            ON_ACTIVITY_RESULT,
            ON_TANGO_SERVICE_CONNECTED,
            ON_TANGO_SERVICE_DISCONNECTED,
            ON_REQUEST_PERMISSION_RESULT,
            ON_DISPLAY_CHANGED,
            ON_START,
            ON_STOP
        }

        /// <summary>
        /// Permission types used by Tango applications.
        /// </summary>
        [Flags]
        private enum PermissionsTypes
        {
            // All entries must be a power of two for
            // use in a bit field as flags.
            NONE = 0,
            AREA_LEARNING = 0x1,
            ANDROID_CAMERA = 0x2,
            SERVICE_BOUND = 0x4,
        }

        /// <summary>
        /// State of the permission request process.
        /// </summary>
        private enum PermissionRequestState
        {
            NONE = 0,
            PERMISSION_REQUEST_INIT = 1,
            REQUEST_ANDROID_PERMISSIONS = 2,
            BIND_TO_SERVICE = 3,
            ALL_PERMISSIONS_GRANTED = 4,
            SOME_PERMISSIONS_DENIED = 5,
        }

        /// <summary>
        /// Gets a value indicating whether this is connected to a Tango service.
        /// </summary>
        /// <value><c>true</c> if connected to a Tango service; otherwise, <c>false</c>.</value>
        public bool IsServiceConnected
        {
            get { return m_isTangoStarted; }
        }

        /// <summary>
        /// Gets the Tango config.  Useful for debugging.
        /// </summary>
        /// <value>The config.</value>
        internal TangoConfig Config
        {
            get { return m_tangoConfig; }
        }

        /// <summary>
        /// Gets the current Tango runtime config.  Useful for debugging.
        /// </summary>
        /// <value>The current runtime config.</value>
        internal TangoConfig RuntimeConfig
        {
            get { return m_tangoRuntimeConfig; }
        }

        /// <summary>
        /// Get the Tango service version name.
        /// </summary>
        /// <returns>String for the version name.</returns>
        public static string GetTangoServiceVersion()
        {
            if (m_tangoServiceVersion == string.Empty)
            {
                m_tangoServiceVersion = AndroidHelper.GetTangoCoreVersionName();
            }

            return m_tangoServiceVersion;
        }

        /// <summary>
        /// DEPRECATED: Get the video overlay texture.
        /// </summary>
        /// <returns>The video overlay texture.</returns>
        public YUVTexture GetVideoOverlayTextureYUV()
        {
            return m_yuvTexture;
        }

        /// <summary>
        /// Register to get Tango callbacks.
        ///
        /// The object should derive from one of ITangoDepth, ITangoEvent, ITangoPose, ITangoVideoOverlay, or
        /// ITangoExperimentalTangoVideoOverlay.  You will get callback during Update until you unregister.
        /// </summary>
        /// <param name="tangoObject">Object to get Tango callbacks from.</param>
        public void Register(object tangoObject)
        {
            ITangoAreaDescriptionEvent areaDescriptionEvent = tangoObject as ITangoAreaDescriptionEvent;
            if (areaDescriptionEvent != null)
            {
                AreaDescriptionEventListener.Register(areaDescriptionEvent.OnAreaDescriptionImported,
                                                      areaDescriptionEvent.OnAreaDescriptionExported);
            }

            ITangoEvent tangoEvent = tangoObject as ITangoEvent;
            if (tangoEvent != null)
            {
                TangoEventListener.RegisterOnTangoEventAvailable(tangoEvent.OnTangoEventAvailableEventHandler);
            }

            ITangoEventMultithreaded tangoEventMultithreaded = tangoObject as ITangoEventMultithreaded;
            if (tangoEventMultithreaded != null)
            {
                TangoEventListener.RegisterOnTangoEventMultithreadedAvailable(
                    tangoEventMultithreaded.OnTangoEventMultithreadedAvailableEventHandler);
            }

            TangoUx tangoUx = tangoObject as TangoUx;
            if (tangoUx != null)
            {
                m_androidPermissionsEvent += tangoUx.OnAndroidPermissions;
            }

            ITangoLifecycle tangoLifecycle = tangoObject as ITangoLifecycle;
            if (tangoLifecycle != null)
            {
                // We attach OnTangoBindEvent to OnTangoPermissions lifecycle for backwards compatability.
                m_tangoBindEvent += tangoLifecycle.OnTangoPermissions;
                m_tangoConnectEvent += tangoLifecycle.OnTangoServiceConnected;
                m_tangoDisconnectEvent += tangoLifecycle.OnTangoServiceDisconnected;
            }

            if (m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;
                if (poseHandler != null)
                {
                    PoseListener.RegisterTangoPoseAvailable(poseHandler.OnTangoPoseAvailable);
                }
            }

            if (m_enableDepth)
            {
                ITangoPointCloud pointCloudHandler = tangoObject as ITangoPointCloud;
                if (pointCloudHandler != null)
                {
                    DepthListener.RegisterOnPointCloudAvailable(pointCloudHandler.OnTangoPointCloudAvailable);
                }

                ITangoPointCloudMultithreaded pointCloudMultithreadedHandler
                    = tangoObject as ITangoPointCloudMultithreaded;
                if (pointCloudMultithreadedHandler != null)
                {
                    DepthListener.RegisterOnPointCloudMultithreadedAvailable(
                        pointCloudMultithreadedHandler.OnTangoPointCloudMultithreadedAvailable);
                }

                ITangoDepth depthHandler = tangoObject as ITangoDepth;
                if (depthHandler != null)
                {
                    DepthListener.RegisterOnTangoDepthAvailable(depthHandler.OnTangoDepthAvailable);
                }

                ITangoDepthMultithreaded depthMultithreadedHandler = tangoObject as ITangoDepthMultithreaded;
                if (depthMultithreadedHandler != null)
                {
                    DepthListener.RegisterOnTangoDepthMultithreadedAvailable(
                        depthMultithreadedHandler.OnTangoDepthMultithreadedAvailable);
                }
            }

            if (m_enableVideoOverlay)
            {
                if (m_videoOverlayUseTextureMethod)
                {
                    ITangoCameraTexture handler = tangoObject as ITangoCameraTexture;
                    if (handler != null)
                    {
                        VideoOverlayListener.RegisterOnTangoCameraTextureAvailable(handler.OnTangoCameraTextureAvailable);
                    }
                }

                if (m_videoOverlayUseYUVTextureIdMethod)
                {
                    IExperimentalTangoVideoOverlay handler = tangoObject as IExperimentalTangoVideoOverlay;
                    if (handler != null)
                    {
                        VideoOverlayListener.RegisterOnTangoYUVTextureAvailable(handler.OnExperimentalTangoImageAvailable);
                    }
                }

                if (m_videoOverlayUseByteBufferMethod)
                {
                    ITangoVideoOverlay handler = tangoObject as ITangoVideoOverlay;
                    if (handler != null)
                    {
                        VideoOverlayListener.RegisterOnTangoImageAvailable(handler.OnTangoImageAvailableEventHandler);
                    }

                    ITangoVideoOverlayMultithreaded multithreadedHandler = tangoObject as ITangoVideoOverlayMultithreaded;
                    if (multithreadedHandler != null)
                    {
                        VideoOverlayListener.RegisterOnTangoImageMultithreadedAvailable(multithreadedHandler.OnTangoImageMultithreadedAvailable);
                    }
                }
            }

            if (m_enable3DReconstruction)
            {
                ITango3DReconstruction t3drHandler = tangoObject as ITango3DReconstruction;
                if (t3drHandler != null && m_tango3DReconstruction != null)
                {
                    m_tango3DReconstruction.RegisterGridIndicesDirty(t3drHandler.OnTango3DReconstructionGridIndicesDirty);
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
            ITangoAreaDescriptionEvent areaDescriptionEvent = tangoObject as ITangoAreaDescriptionEvent;
            if (areaDescriptionEvent != null)
            {
                AreaDescriptionEventListener.Unregister(areaDescriptionEvent.OnAreaDescriptionImported,
                                                        areaDescriptionEvent.OnAreaDescriptionExported);
            }

            ITangoEvent tangoEvent = tangoObject as ITangoEvent;
            if (tangoEvent != null)
            {
                TangoEventListener.UnregisterOnTangoEventAvailable(tangoEvent.OnTangoEventAvailableEventHandler);
            }

            ITangoEventMultithreaded tangoEventMultithreaded = tangoObject as ITangoEventMultithreaded;
            if (tangoEventMultithreaded != null)
            {
                TangoEventListener.UnregisterOnTangoEventMultithreadedAvailable(
                    tangoEventMultithreaded.OnTangoEventMultithreadedAvailableEventHandler);
            }

            TangoUx tangoUx = tangoObject as TangoUx;
            if (tangoUx != null)
            {
                m_androidPermissionsEvent -= tangoUx.OnAndroidPermissions;
            }

            ITangoLifecycle tangoLifecycle = tangoObject as ITangoLifecycle;
            if (tangoLifecycle != null)
            {
                // We detach OnTangoBindEvent from OnTangoPermissions lifecycle for backwards compatability.
                m_tangoBindEvent -= tangoLifecycle.OnTangoPermissions;
                m_tangoConnectEvent -= tangoLifecycle.OnTangoServiceConnected;
                m_tangoDisconnectEvent -= tangoLifecycle.OnTangoServiceDisconnected;
            }

            if (m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;
                if (poseHandler != null)
                {
                    PoseListener.UnregisterTangoPoseAvailable(poseHandler.OnTangoPoseAvailable);
                }
            }

            if (m_enableDepth)
            {
                ITangoPointCloud pointCloudHandler = tangoObject as ITangoPointCloud;
                if (pointCloudHandler != null)
                {
                    DepthListener.UnregisterOnPointCloudAvailable(pointCloudHandler.OnTangoPointCloudAvailable);
                }

                ITangoPointCloudMultithreaded pointCloudMultithreaded = tangoObject as ITangoPointCloudMultithreaded;
                if (pointCloudMultithreaded != null)
                {
                    DepthListener.UnregisterOnPointCloudMultithreadedAvailable(
                        pointCloudMultithreaded.OnTangoPointCloudMultithreadedAvailable);
                }

                ITangoDepth depthHandler = tangoObject as ITangoDepth;
                if (depthHandler != null)
                {
                    DepthListener.UnregisterOnTangoDepthAvailable(depthHandler.OnTangoDepthAvailable);
                }

                ITangoDepthMultithreaded depthMultithreadedHandler = tangoObject as ITangoDepthMultithreaded;
                if (depthMultithreadedHandler != null)
                {
                    DepthListener.UnregisterOnTangoDepthMultithreadedAvailable(
                        depthMultithreadedHandler.OnTangoDepthMultithreadedAvailable);
                }
            }

            if (m_enableVideoOverlay)
            {
                if (m_videoOverlayUseTextureMethod)
                {
                    ITangoCameraTexture handler = tangoObject as ITangoCameraTexture;
                    if (handler != null)
                    {
                        VideoOverlayListener.UnregisterOnTangoCameraTextureAvailable(handler.OnTangoCameraTextureAvailable);
                    }
                }

                if (m_videoOverlayUseYUVTextureIdMethod)
                {
                    IExperimentalTangoVideoOverlay handler = tangoObject as IExperimentalTangoVideoOverlay;
                    if (handler != null)
                    {
                        VideoOverlayListener.UnregisterOnTangoYUVTextureAvailable(handler.OnExperimentalTangoImageAvailable);
                    }
                }

                if (m_videoOverlayUseByteBufferMethod)
                {
                    ITangoVideoOverlay handler = tangoObject as ITangoVideoOverlay;
                    if (handler != null)
                    {
                        VideoOverlayListener.UnregisterOnTangoImageAvailable(handler.OnTangoImageAvailableEventHandler);
                    }

                    ITangoVideoOverlayMultithreaded multithreadedHandler = tangoObject as ITangoVideoOverlayMultithreaded;
                    if (multithreadedHandler != null)
                    {
                        VideoOverlayListener.UnregisterOnTangoImageMultithreadedAvailable(multithreadedHandler.OnTangoImageMultithreadedAvailable);
                    }
                }
            }

            if (m_enable3DReconstruction)
            {
                ITango3DReconstruction t3drHandler = tangoObject as ITango3DReconstruction;
                if (t3drHandler != null && m_tango3DReconstruction != null)
                {
                    m_tango3DReconstruction.UnregisterGridIndicesDirty(t3drHandler.OnTango3DReconstructionGridIndicesDirty);
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
        /// Manual initialization step 1: Call this to request Tango permissions.
        ///
        /// To know the result of the permissions request, implement the interface ITangoLifecycle and register
        /// yourself before calling this.
        ///
        /// Once all permissions have been granted, you can call TangoApplication.Startup, optionally passing in the
        /// AreaDescription to load.  You can get the list of AreaDescriptions once the appropriate permission is
        /// granted.
        /// </summary>
        public void RequestPermissions()
        {
#if UNITY_EDITOR
            m_requiredPermissions = PermissionsTypes.NONE;
#else
            if (m_requiredPermissions == PermissionsTypes.NONE)
            {
                if (m_enableVideoOverlay || m_enableDepth)
                {
                    if (!AndroidHelper.CheckPermission(Common.ANDROID_CAMERA_PERMISSION))
                    {
                        m_requiredPermissions |= PermissionsTypes.ANDROID_CAMERA;
                    }
                }

                if (m_enableAreaDescriptions && !m_enableDriftCorrection)
                {
                    if (!AndroidHelper.ApplicationHasTangoPermissions(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS))
                    {
                        m_requiredPermissions |= PermissionsTypes.AREA_LEARNING;
                    }
                }
            }

            // It is always required to rebind to the service.
            m_requiredPermissions |= PermissionsTypes.SERVICE_BOUND;
#endif
            m_permissionRequestState = PermissionRequestState.PERMISSION_REQUEST_INIT;
            _RequestNextPermission();
        }

        /// <summary>
        /// Manual initialization step 2: Call this to connect to the Tango service.
        ///
        /// After connecting to the Tango service, you will get updates for Motion Tracking, Depth Sensing, and Area
        /// Learning.  If you have a specific Area Description you want to localize too, pass that Area Description in
        /// here.
        /// </summary>
        /// <param name="areaDescription">If not null, the Area Description to localize to.</param>
        public void Startup(AreaDescription areaDescription)
        {
            // Make sure all required permissions have been granted.
            if (m_requiredPermissions != PermissionsTypes.NONE)
            {
                Debug.Log(CLASS_NAME + ".Startup() -- ERROR: Not all required permissions were accepted yet. Needed " +
                          "permission: " + m_requiredPermissions);
                return;
            }

            if (!_CheckTangoVersion())
            {
                // Error logged in _CheckTangoVersion function.
                return;
            }

            // Setup configs.
            m_tangoConfig = new TangoConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_DEFAULT);
            m_tangoRuntimeConfig = new TangoConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_RUNTIME);

            if (m_enableVideoOverlay && m_videoOverlayUseYUVTextureIdMethod)
            {
                int yTextureWidth = 0;
                int yTextureHeight = 0;
                int uvTextureWidth = 0;
                int uvTextureHeight = 0;

                m_tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_Y_TEXTURE_WIDTH, ref yTextureWidth);
                m_tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_Y_TEXTURE_HEIGHT, ref yTextureHeight);
                m_tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_UV_TEXTURE_WIDTH, ref uvTextureWidth);
                m_tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_UV_TEXTURE_HEIGHT, ref uvTextureHeight);

                if (yTextureWidth == 0 || yTextureHeight == 0 || uvTextureWidth == 0 || uvTextureHeight == 0)
                {
                    Debug.Log("Video overlay texture sizes were not set properly");
                }

                m_yuvTexture.ResizeAll(yTextureWidth, yTextureHeight, uvTextureWidth, uvTextureHeight);
            }

            if (areaDescription != null)
            {
                _InitializeMotionTracking(areaDescription.m_uuid);
            }
            else
            {
                _InitializeMotionTracking(null);
            }

            if (m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_DEPTH_PERCEPTION_BOOL, m_enableDepth)
                && m_tangoConfig.SetInt32(TangoConfig.Keys.DEPTH_MODE, (int)TangoConfig.DepthMode.XYZC)
                && m_enableDepth)
            {
                DepthListener.SetCallback();
            }

            if (m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_COLOR_CAMERA_BOOL, m_enableVideoOverlay) &&
                m_enableVideoOverlay)
            {
                _SetVideoOverlayCallbacks();
            }

            TangoEventListener.SetCallback();

            if (m_enable3DReconstruction)
            {
                Register(m_tango3DReconstruction);
            }

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
            _TangoDisconnect();
        }

        /// <summary>
        /// Called when a new ux exception event is dispatched.
        /// </summary>
        /// <param name="exceptionEvent">Event containing information about the exception.</param>
        public void OnUxExceptionEventHandler(Tango.UxExceptionEvent exceptionEvent) 
        {
            if (exceptionEvent.status == TangoUxEnums.UxExceptionEventStatus.STATUS_DETECTED)
            {
                m_unresolvedUxExceptions.Add(exceptionEvent.type);
            }
            else if (exceptionEvent.status == TangoUxEnums.UxExceptionEventStatus.STATUS_RESOLVED)
            {
                m_unresolvedUxExceptions.Remove(exceptionEvent.type);
            }
            else
            {
                return;
            }

            _UpdateSleepTimeout();
        }

        /// <summary>
        /// Set the frame rate of the depth camera.
        ///
        /// Disabling or reducing the frame rate of the depth camera when it is running can save a significant amount
        /// of battery.
        /// </summary>
        /// <param name="rate">The rate in frames per second, for the depth camera to run at.</param>
        public void SetDepthCameraRate(int rate)
        {
            _SetDepthCameraRate(rate);
            m_appDepthCameraRate = rate;
        }

        /// <summary>
        /// Set the frame rate of the depth camera.
        ///
        /// Disabling or reducing the frame rate of the depth camera when it is running can save a significant amount
        /// of battery.
        /// </summary>
        /// <param name="rate">A special rate to set the depth camera to.</param>
        public void SetDepthCameraRate(TangoEnums.TangoDepthCameraRate rate)
        {
            switch (rate)
            {
            case TangoEnums.TangoDepthCameraRate.DISABLED:
                SetDepthCameraRate(0);
                break;

            case TangoEnums.TangoDepthCameraRate.MAXIMUM:
                // Set the depth frame rate to a sufficiently high number, it will get rounded down.  There is no
                // way to actually get the maximum value to pass in.
                SetDepthCameraRate(5);
                break;
            }
        }

        /// <summary>
        /// A cap on the number of points allowable in a point cloud; the point cloud will be reduced to this size if
        /// it exceeds it.
        ///
        /// Reducing the point cloud density will not reduce the time it takes for the native Tango process to
        /// produce depth data (and may in fact incur a small penalty). Performance gains come strictly from operations
        /// downstream of point cloud creation (e.g. converting the depth cloud points into Unity coordinates,
        /// point cloud rendering, plane-finding, etc).
        /// </summary>
        /// <param name="maxDepthPoints">Maximum number of depth points. A value of 0 means no limit.</param>
        public void SetMaxDepthPoints(int maxDepthPoints)
        {
            DepthListener.SetPointCloudLimit(maxDepthPoints);
        }

        /// <summary>
        /// Clear the 3D Reconstruction data.  The reconstruction will start fresh.
        /// </summary>
        public void Tango3DRClear()
        {
            if (m_tango3DReconstruction != null)
            {
                m_tango3DReconstruction.Clear();
            }
        }

        /// <summary>
        /// Extract a single grid cell's mesh.
        /// </summary>
        /// <returns>Status of the extraction.</returns>
        /// <param name="gridIndex">Grid index to extract.</param>
        /// <param name="vertices">Filled out with extracted vertices.</param>
        /// <param name="normals">Filled out with extracted normals.</param>
        /// <param name="colors">Filled out with extracted colors.</param>
        /// <param name="triangles">Filled out with extracted triangle indices.</param>
        /// <param name="numVertices">Filled out with the number of valid vertices.</param>
        /// <param name="numTriangles">Filled out with the number of valid triangles.</param>
        public Tango3DReconstruction.Status Tango3DRExtractMeshSegment(
            Tango3DReconstruction.GridIndex gridIndex, Vector3[] vertices, Vector3[] normals, Color32[] colors,
            int[] triangles, out int numVertices, out int numTriangles)
        {
            if (m_tango3DReconstruction != null)
            {
                return m_tango3DReconstruction.ExtractMeshSegment(gridIndex, vertices, normals, colors, triangles,
                                                                  out numVertices, out numTriangles);
            }

            numVertices = 0;
            numTriangles = 0;
            return Tango3DReconstruction.Status.INVALID;
        }

        /// <summary>
        /// Extracts a mesh of the entire 3D reconstruction into a suitable format for a Unity Mesh.
        /// </summary>
        /// <returns>
        /// Returns <c>Status.SUCCESS</c> if the mesh is fully extracted and stored in the lists. Otherwise, Status.ERROR or 
        /// Status.INVALID is returned if some error occurs.</returns>
        /// <param name="vertices">A list to which mesh vertices will be appended, can be null.</param>
        /// <param name="normals">A list to which mesh normals will be appended, can be null.</param>
        /// <param name="colors">A list to which mesh colors will be appended, can be null.</param>
        /// <param name="triangles">A list to which vertex indices will be appended, can be null.</param>
        public Tango3DReconstruction.Status Tango3DRExtractWholeMesh(
            List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles)
        {
            if (m_tango3DReconstruction != null)
            {
                return m_tango3DReconstruction.ExtractWholeMesh(vertices, normals, colors, triangles);
            }

            return Tango3DReconstruction.Status.INVALID;
        }

        /// <summary>
        /// Extract an array of <c>SignedDistanceVoxel</c> objects.
        /// </summary>
        /// <returns>
        /// Returns Status.SUCCESS if the voxels are fully extracted and stared in the array.  In this case, <c>numVoxels</c>
        /// will say how many voxels are used, the rest of the array is untouched.
        ///
        /// Returns Status.INVALID if the array length does not exactly equal the number of voxels in a single grid
        /// index.  By default, the number of voxels in a grid index is 16*16*16.
        ///
        /// Returns Status.INVALID if some other error occurs.
        /// </returns>
        /// <param name="gridIndex">Grid index to extract.</param>
        /// <param name="voxels">
        /// On successful extraction this will get filled out with the signed distance voxels.
        /// </param>
        /// <param name="numVoxels">Number of voxels filled out.</param>
        public Tango3DReconstruction.Status Tango3DRExtractSignedDistanceVoxel(
            Tango3DReconstruction.GridIndex gridIndex, Tango3DReconstruction.SignedDistanceVoxel[] voxels,
            out int numVoxels)
        {
            if (m_tango3DReconstruction != null)
            {
                return m_tango3DReconstruction.ExtractSignedDistanceVoxel(gridIndex, voxels, out numVoxels);
            }

            numVoxels = 0;
            return Tango3DReconstruction.Status.INVALID;
        }

        /// <summary>
        /// Enable or disable the 3D Reconstruction.
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public void Set3DReconstructionEnabled(bool enabled)
        {
            m_tango3DReconstruction.SetEnabled(enabled);
        }
        
        /// <summary>
        /// Clears all current unresolved ux exceptions and updates the sleep timeout.
        /// </summary>
        private void _ResetSleepTimeout()
        {
            m_unresolvedUxExceptions.Clear();
            _UpdateSleepTimeout();
        }

        /// <summary>
        /// Updates the device sleep timeout.
        /// </summary>
        private void _UpdateSleepTimeout()
        {
            if (m_keepScreenAwake && !m_unresolvedUxExceptions.Overlaps(SCREEN_SLEEPABLE_UX_EXCEPTIONS))
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
            else
            {
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
            }
        }

        /// <summary>
        /// Set callbacks for all VideoOverlayListener objects.
        /// </summary>
        private void _SetVideoOverlayCallbacks()
        {
            Debug.Log("TangoApplication._SetVideoOverlayCallbacks()");

            if (m_videoOverlayUseTextureMethod)
            {
                VideoOverlayListener.SetCallbackTextureMethod();
            }

            if (m_videoOverlayUseYUVTextureIdMethod)
            {
                VideoOverlayListener.SetCallbackYUVTextureIdMethod(m_yuvTexture);
            }

            if (m_videoOverlayUseByteBufferMethod)
            {
                VideoOverlayListener.SetCallbackByteBufferMethod();
            }
        }

        /// <summary>
        /// Initialize motion tracking.
        /// </summary>
        /// <param name="uuid">ADF UUID to load.</param>
        private void _InitializeMotionTracking(string uuid)
        {
            Debug.Log("TangoApplication._InitializeMotionTracking(" + uuid + ")");

            System.Collections.Generic.List<TangoCoordinateFramePair> framePairs = new System.Collections.Generic.List<TangoCoordinateFramePair>();

            bool usedUUID = false;
            if (m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_BOOL, m_enableMotionTracking) && m_enableMotionTracking)
            {
                TangoCoordinateFramePair motionTracking;
                motionTracking.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                motionTracking.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                framePairs.Add(motionTracking);

                if (m_enableAreaDescriptions)
                {
                    if (!m_enableDriftCorrection)
                    {
                        m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_AREA_LEARNING_BOOL, m_areaDescriptionLearningMode);

                        if (!string.IsNullOrEmpty(uuid))
                        {
                            if (m_tangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, uuid))
                            {
                                usedUUID = true;
                            }
                        }

                        if (m_enableCloudADF)
                        {
                            m_tangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, string.Empty);
                            m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_CLOUD_ADF_BOOL, true);
                            Debug.Log("Local AreaDescription cannot be loaded when cloud ADF is enabled, Tango is starting" +
                                      "with cloud Area Description only." + Environment.StackTrace);
                        }
                    }
                    else
                    {
                        m_tangoConfig.SetBool(TangoConfig.Keys.EXPERIMENTAL_ENABLE_DRIFT_CORRECTION_BOOL,
                                              m_enableDriftCorrection);
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
                PoseListener.SetCallback(framePairs.ToArray());
            }

            // The C API does not default this to on, but it is locked down.
            m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_LOW_LATENCY_IMU_INTEGRATION, true);

            m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL, m_motionTrackingAutoReset);

            // Check if the UUID passed in was actually used.
            if (!usedUUID && !string.IsNullOrEmpty(uuid))
            {
                Debug.Log("An AreaDescription UUID was passed in, but motion tracking and area descriptions are not "
                          + "both enabled." + Environment.StackTrace);
            }

#if UNITY_EDITOR
            EmulatedAreaDescriptionHelper.InitEmulationForUUID(uuid, m_enableAreaDescriptions, m_areaDescriptionLearningMode,
                                                               m_enableDriftCorrection, m_emulatedAreaDescriptionStartOffset);
#endif
        }

        /// <summary>
        /// Validate the TangoService version is supported.
        /// </summary>
        /// <returns>Returns <c>true</c> if Tango version is compatible, otherwise not.</returns>
        private bool _CheckTangoVersion()
        {
            if (!AndroidHelper.IsTangoCoreUpToDate())
            {
                Debug.Log(string.Format(CLASS_NAME + ".Initialize() Invalid API version. Please update Project Tango Core to at least {0}.", AndroidHelper.TANGO_MINIMUM_VERSION_CODE));
                if (!m_allowOutOfDateTangoAPI)
                {
                    TangoUx ux = GetComponent<TangoUx>();

                    if (ux != null && ux.isActiveAndEnabled)
                    {
                        ux.ShowTangoOutOfDate();
                    }

                    return false;
                }
            }

            m_isTangoUpToDate = true;
            Debug.Log(CLASS_NAME + ".Initialize() Tango was initialized!");
            return true;
        }

        /// <summary>
        /// Set the frame rate of the depth camera.
        /// Unlike public SetDepthCameraRate function. This function doesn't set m_appDepthCameraRate.
        ///
        /// Disabling or reducing the frame rate of the depth camera when it is running can save a significant amount
        /// of battery.
        /// </summary>
        /// <param name="rate">The rate in frames per second, for the depth camera to run at.</param>
        private void _SetDepthCameraRate(int rate)
        {
            if (rate < 0)
            {
                Debug.Log("Invalid rate passed to SetDepthCameraRate");
                return;
            }

            m_tangoRuntimeConfig.SetInt32(TangoConfig.Keys.RUNTIME_DEPTH_FRAMERATE, rate);
            m_tangoRuntimeConfig.SetRuntimeConfig();
        }

        /// <summary>
        /// Connect to the Tango Service.
        /// </summary>
        private void _TangoConnect()
        {
            Debug.Log("TangoApplication._TangoConnect()");

            _ResetSleepTimeout();
            
            if (!m_isTangoUpToDate)
            {
                return;
            }

            lock (m_tangoLifecycleLock)
            {
                if (!m_isTangoStarted && !m_isApplicationPaused)
                {
                    m_isTangoStarted = true;

                    AndroidHelper.PerformanceLog("Unity _TangoConnect start");
                    if (API.TangoService_connect(m_callbackContext, m_tangoConfig.GetHandle()) != Common.ErrorType.TANGO_SUCCESS)
                    {
                        AndroidHelper.ShowAndroidToastMessage("Failed to connect to Tango Service.");
                        Debug.Log(CLASS_NAME + ".Connect() Could not connect to the Tango Service!");
                    }
                    else
                    {
                        AndroidHelper.PerformanceLog("Unity _TangoConnect end");
                        Debug.Log(CLASS_NAME + ".Connect() Tango client connected to service!");
                    }

                    if (m_tangoConnectEvent != null)
                    {
                        m_tangoConnectEvent();
                    }
                }
            }
        }

        /// <summary>
        /// Disconnect from the Tango Service.
        /// </summary>
        private void _TangoDisconnect()
        {
            lock (m_tangoLifecycleLock)
            {
                if (!m_isTangoStarted)
                {
                    AndroidHelper.UnbindTangoService();
                    Debug.Log(CLASS_NAME + ".Disconnect() Not disconnecting from Tango Service "
                              + "as this TangoApplication was not connected");
                    return;
                }

                Debug.Log(CLASS_NAME + ".Disconnect() Disconnecting from the Tango Service");
                m_isTangoStarted = false;

                // This is necessary because tango_client_api clears camera callbacks when
                // TangoService_disconnect() is called, unlike other callbacks.
                VideoOverlayListener.ClearTangoCallbacks();

                _SetDepthCameraRate(5);

                API.TangoService_disconnect();
                Debug.Log(CLASS_NAME + ".Disconnect() Tango client disconnected from service!");

                if (m_tangoDisconnectEvent != null)
                {
                    m_tangoDisconnectEvent();
                }

                AndroidHelper.UnbindTangoService();
                Debug.Log(CLASS_NAME + ".Disconnect() Tango client unbind from service!");
#if UNITY_EDITOR
                PoseProvider.ResetTangoEmulation();
#endif
            }
        }

        /// <summary>
        /// Android on start.
        /// </summary>
        private void _androidOnStart()
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_START));
            }

            Debug.Log(CLASS_NAME + "._androidOnStart() Android OnStart() called from Android main thread.");
        }

        /// <summary>
        /// Android on stop.
        /// </summary>
        private void _androidOnStop()
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_STOP));
            }

            _TangoDisconnect();
            Debug.Log(CLASS_NAME + "._androidOnStop() Android OnStop() called from Android main thread.");
        }

        /// <summary>
        /// Android on pause.
        /// </summary>
        private void _androidOnPause()
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_PAUSE));
            }

            lock (m_tangoLifecycleLock)
            {
                m_isApplicationPaused = true;
            }

            Debug.Log(CLASS_NAME + "._androidOnPause() Android OnPause() called from Android main thread.");
        }

        /// <summary>
        /// Android on resume.
        /// </summary>
        private void _androidOnResume()
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_RESUME));
            }

            lock (m_tangoLifecycleLock)
            {
                m_isApplicationPaused = false;
            }

            Debug.Log(CLASS_NAME + "._androidOnResume() Android OnResume() called from Android main thread.");
        }

        /// <summary>
        /// EventHandler for Android's on activity result.
        /// </summary>
        /// <param name="requestCode">Request code.</param>
        /// <param name="resultCode">Result code.</param>
        /// <param name="data">Intent data.</param>
        private void _androidOnActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_ACTIVITY_RESULT,
                    requestCode, resultCode, data));
            }

            Debug.Log(CLASS_NAME + "._androidOnActivityResult() Android OnActivityResult() called from Android main " +
                "thread.");
        }

        /// <summary>
        /// EventHandler for Android's on request permission result.
        /// </summary>
        /// <param name="requestCode">Request code.</param>
        /// <param name="permissions">Permissions requested.</param>
        /// <param name="grantResults">Grant result for each corresponding permission.</param>
        private void _androidOnRequestPermissionsResult(
            int requestCode, string[] permissions, AndroidPermissionGrantResult[] grantResults)
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_REQUEST_PERMISSION_RESULT,
                    requestCode, permissions, grantResults));
            }

            Debug.Log(CLASS_NAME + "._androidOnRequestPermissionsResult() Android OnPermissionResult() called from " +
                "Android main thread.");
        }

        /// <summary>
        /// Delegate for when connected to the Tango Android service.
        /// </summary>
        /// <param name="binder">Binder for the service.</param>
        private void _androidOnTangoServiceConnected(AndroidJavaObject binder)
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_TANGO_SERVICE_CONNECTED,
                    binder));
            }

            Debug.Log(CLASS_NAME + "._androidOnTangoServiceConnected() Android OnServiceConnected() called from " +
                "Android main thread.");
        }

        /// <summary>
        /// Delegate for when disconnected from the Tango Android service.
        /// </summary>
        private void _androidOnTangoServiceDisconnected()
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_TANGO_SERVICE_DISCONNECTED));
            }

            Debug.Log(CLASS_NAME + "._androidOnTangoServiceDisconnected() Android OnServiceDisconnected() called " +
                "from Android main thread.");
        }

        /// <summary>
        /// Delegate for the Android display rotation changed.
        /// </summary>
        private void _androidOnDisplayChanged()
        {
            lock (m_messageQueueLock)
            {
                m_androidMessageQueue.Enqueue(new AndroidMessage(AndroidMessageType.ON_DISPLAY_CHANGED));
            }

            Debug.Log(CLASS_NAME + "._androidOnDisplayChanged() Android OnDisplayChanged() called " +
                "from Android main thread.");
        }

        /// <summary>
        /// Awake this instance.
        /// </summary>
        private void Awake()
        {
            if (!AndroidHelper.LoadTangoLibrary())
            {
                Debug.Log("Unable to load Tango library.  Things may not work.");
                return;
            }

            AndroidHelper.RegisterStartEvent(_androidOnStart);
            AndroidHelper.RegisterStopEvent(_androidOnStop);
            AndroidHelper.RegisterPauseEvent(_androidOnPause);
            AndroidHelper.RegisterResumeEvent(_androidOnResume);
            AndroidHelper.RegisterOnActivityResultEvent(_androidOnActivityResult);
            AndroidHelper.RegisterOnDisplayChangedEvent(_androidOnDisplayChanged);
            AndroidHelper.RegisterOnTangoServiceConnected(_androidOnTangoServiceConnected);
            AndroidHelper.RegisterOnTangoServiceDisconnected(_androidOnTangoServiceDisconnected);
            AndroidHelper.RegisterOnRequestPermissionsResultEvent(_androidOnRequestPermissionsResult);

            if (m_enableDepth)
            {
                DepthListener.SetPointCloudLimit(m_initialPointCloudMaxPoints);
            }

            if (m_enableVideoOverlay)
            {
                int yTextureWidth = 0;
                int yTextureHeight = 0;
                int uvTextureWidth = 0;
                int uvTextureHeight = 0;

                m_yuvTexture = new YUVTexture(yTextureWidth, yTextureHeight, uvTextureWidth, uvTextureHeight, TextureFormat.RGBA32, false);
            }

            if (m_enable3DReconstruction)
            {
                m_tango3DReconstruction = new Tango3DReconstruction(
                    resolution: m_3drResolutionMeters,
                    generateColor: m_3drGenerateColor,
                    spaceClearing: m_3drSpaceClearing,
                    minNumVertices: m_3drMinNumVertices,
                    updateMethod: m_3drUpdateMethod);
                m_tango3DReconstruction.m_useAreaDescriptionPose = m_3drUseAreaDescriptionPose;
                m_tango3DReconstruction.m_sendColorToUpdate = m_3drGenerateColor;
            }

            TangoSupport.UpdatePoseMatrixFromDeviceRotation(AndroidHelper.GetDisplayRotation(),
                                                            AndroidHelper.GetColorCameraRotation());

            if (m_adjustScreenResolution)
            {
                _ChangeResolutionForPerformance();
            }

            // Importing and exporting Area Descriptions can be done before you connect. We must
            // propogate those events if they happen.
            AreaDescriptionEventListener.SetCallback();

            _ResetSleepTimeout();

            TangoUx tangoUx = GetComponent<TangoUx>();
            if (tangoUx != null)
            {
                tangoUx.Register(this);
            }

#if UNITY_EDITOR
            if (m_doSlowEmulation && (m_enableDepth || m_enableVideoOverlay))
            {
                if (m_emulationEnvironment == null)
                {
                    Debug.LogError("No Mesh for Emulation assigned on the Tango Application (commonly in the Tango Manager prefab)."
                                   + " Expect blank camera and/or depth frames.");
                }

                EmulatedEnvironmentRenderHelper.InitForEnvironment(m_emulationEnvironment, m_emulationEnvironmentTexture, m_emulationVideoOverlaySimpleLighting);
            }
            else
            {
                EmulatedEnvironmentRenderHelper.Clear();
            }
#endif
        }

        /// <summary>
        /// Called when an Android or Tango permission was denied.
        /// </summary>
        private void _PermissionWasDenied()
        {
            if (m_permissionRequestState == PermissionRequestState.REQUEST_ANDROID_PERMISSIONS)
            {
                _FireAndroidPermissionEvent(false);
            }

            _FireTangoBindEvent(false);
            m_requiredPermissions = PermissionsTypes.NONE;
            m_permissionRequestState = PermissionRequestState.SOME_PERMISSIONS_DENIED;
        }

        /// <summary>
        /// Requests next permission and updates permission states.
        /// </summary>
        private void _RequestNextPermission()
        {
            if (m_permissionRequestState == PermissionRequestState.PERMISSION_REQUEST_INIT)
            {
                m_permissionRequestState = PermissionRequestState.REQUEST_ANDROID_PERMISSIONS;
            }

            if (m_permissionRequestState == PermissionRequestState.REQUEST_ANDROID_PERMISSIONS)
            {
                if ((m_requiredPermissions & PermissionsTypes.AREA_LEARNING) == PermissionsTypes.AREA_LEARNING)
                {
                    AndroidHelper.StartTangoPermissionsActivity(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS);
                    return;
                }
                else if ((m_requiredPermissions & PermissionsTypes.ANDROID_CAMERA) == PermissionsTypes.ANDROID_CAMERA)
                {
                    AndroidHelper.RequestPermission(Common.ANDROID_CAMERA_PERMISSION,
                        Common.ANDROID_PERMISSION_REQUEST_CODE);
                    return;
                }
                else
                {
                    _FireAndroidPermissionEvent(true);
                    m_permissionRequestState = PermissionRequestState.BIND_TO_SERVICE;
                }
            }

            if (m_permissionRequestState == PermissionRequestState.BIND_TO_SERVICE)
            {
                if ((m_requiredPermissions & PermissionsTypes.SERVICE_BOUND) == PermissionsTypes.SERVICE_BOUND)
                {
                    if (!AndroidHelper.BindTangoService())
                    {
                        _PermissionWasDenied();
                        Debug.Log(CLASS_NAME + "Update() Permission denied: " + PermissionsTypes.SERVICE_BOUND);
                    }
                }
                else
                {
                    _FireTangoBindEvent(true);
                    m_permissionRequestState = PermissionRequestState.ALL_PERMISSIONS_GRANTED;
                }
            }
        }

        /// <summary>
        /// Fires the AndroidPermissionEvent.
        /// </summary>
        /// <param name="permissionsGranted">Specifies if permissions were granted.</param>
        private void _FireAndroidPermissionEvent(bool permissionsGranted)
        {
            if (m_androidPermissionsEvent != null)
            {
                m_androidPermissionsEvent(permissionsGranted);
            }
        }

        /// <summary>
        /// Fires the TangoBindEvent.
        /// </summary>
        /// <param name="success">Specifies if bind was successful.</param>
        private void _FireTangoBindEvent(bool success)
        {
            if (m_tangoBindEvent != null)
            {
                m_tangoBindEvent(success);
            }
        }

        /// <summary>
        /// Handle resolution-limiting option for performance.
        /// </summary>
        private void _ChangeResolutionForPerformance()
        {
            if (m_targetResolution < Mathf.Min(Screen.width, Screen.height) || m_allowOversizedScreenResolutions)
            {
                // Record aspect only once so that it can't get corrupted through
                // rounding across successive resolution changes.
                if (m_screenLandscapeAspectRatio == -1)
                {
                    float bigDimension = Mathf.Max(Screen.width, Screen.height);
                    float littleDimension = Mathf.Min(Screen.width, Screen.height);

                    m_screenLandscapeAspectRatio = bigDimension / littleDimension;
                }

                int targetWidth, targetHeight;

                if (Screen.width > Screen.height)
                {
                    targetWidth = Mathf.RoundToInt(m_targetResolution * m_screenLandscapeAspectRatio);
                    targetHeight = m_targetResolution;
                }
                else
                {
                    targetWidth = m_targetResolution;
                    targetHeight = Mathf.RoundToInt(m_targetResolution * m_screenLandscapeAspectRatio);
                }

                Screen.SetResolution(targetWidth, targetHeight, Screen.fullScreen);
            }
        }

        /// <summary>
        /// Handle permission result.
        /// </summary>
        /// <param name="permissionType">Type of permission.</param>
        /// <param name="isGranted">If set to <c>true</c> permissions is granted. Otherwise <c>false</c>..</param>
        private void _PermissionResult(PermissionsTypes permissionType, bool isGranted)
        {
            if (isGranted)
            {
                m_requiredPermissions &= ~permissionType;
                _RequestNextPermission();
            }
            else
            {
                _PermissionWasDenied();
                Debug.Log(CLASS_NAME + "_PermissionResult() Permission denied: " + permissionType);
            }
        }

        /// <summary>
        /// Disperse any events related to Tango functionality.
        /// </summary>
        private void Update()
        {
            while (m_androidMessageQueue.Count != 0)
            {
                AndroidMessage msg;
                lock (m_messageQueueLock)
                {
                    msg = m_androidMessageQueue.Dequeue();
                }

                switch (msg.m_type)
                {
                case AndroidMessageType.ON_START:
                    break;
                case AndroidMessageType.ON_STOP:
                    if (m_isTangoStarted && m_requiredPermissions == PermissionsTypes.NONE)
                    {
                        m_shouldReconnectService = true;
                        m_permissionRequestState = PermissionRequestState.NONE;
                        m_autoConnectRequestedPermissions = false;
                    }

                    break;
                case AndroidMessageType.ON_PAUSE:
                    break;
                case AndroidMessageType.ON_RESUME:
                    if (m_shouldReconnectService)
                    {
                        m_shouldReconnectService = false;
                        _SetDepthCameraRate(m_appDepthCameraRate);
                    }

                    break;
                case AndroidMessageType.ON_ACTIVITY_RESULT:
                    int requestCode = (int)msg.m_messages[0];
                    int resultCode = (int)msg.m_messages[1];
                    if (requestCode == Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE)
                    {
                        _PermissionResult(PermissionsTypes.AREA_LEARNING,
                                          resultCode == (int)Common.AndroidResult.SUCCESS);
                    }

                    break;
                case AndroidMessageType.ON_REQUEST_PERMISSION_RESULT:
                    requestCode = (int)msg.m_messages[0];
                    string[] permissions = (string[])msg.m_messages[1];
                    AndroidPermissionGrantResult[] grantResults = (AndroidPermissionGrantResult[])msg.m_messages[2];
                    if (requestCode == Common.ANDROID_PERMISSION_REQUEST_CODE)
                    {
                        for (int it = 0; it < permissions.Length; ++it)
                        {
                            string permission = permissions[it];
                            AndroidPermissionGrantResult grantResult = grantResults[it];

                            if (permission == Common.ANDROID_CAMERA_PERMISSION)
                            {
                                _PermissionResult(PermissionsTypes.ANDROID_CAMERA,
                                                  grantResult == AndroidPermissionGrantResult.GRANTED);
                            }
                        }
                    }

                    break;
                case AndroidMessageType.ON_TANGO_SERVICE_CONNECTED:
                    AndroidJavaObject binder = (AndroidJavaObject)msg.m_messages[0];

                    // By keeping this logic in C#, the client app can respond if this call fails.
                    int result = AndroidHelper.TangoSetBinder(binder);
                    _PermissionResult(PermissionsTypes.SERVICE_BOUND,
                                      result == Common.ErrorType.TANGO_SUCCESS);

                    break;
                case AndroidMessageType.ON_DISPLAY_CHANGED:
                    if (m_isTangoStarted)
                    {
                        OrientationManager.Rotation displayRotation = AndroidHelper.GetDisplayRotation();
                        OrientationManager.Rotation colorCameraRotation = AndroidHelper.GetColorCameraRotation();
                        TangoSupport.UpdatePoseMatrixFromDeviceRotation(displayRotation, colorCameraRotation);
                        if (OnDisplayChanged != null)
                        {
                            OnDisplayChanged(displayRotation, colorCameraRotation);
                        }
                    }

                    break;
                default:
                    break;
                }
            }

            lock (m_tangoLifecycleLock)
            {
                if (m_isApplicationPaused)
                {
                    return;
                }
            }

            // Autoconnect requesting permissions can not be moved earlier into Awake() or Start().  All other scripts
            // must be able to register for the permissions callback before RequestPermissions() is called.  The
            // earliest another script can register is in Start().  Therefore, this logic must be run after Start() has
            // run on all scripts.  That means it must be in FixedUpdate(), Update(), LateUpdate(), or a coroutine.
            if (m_autoConnectToService)
            {
                if (!m_autoConnectRequestedPermissions)
                {
                    RequestPermissions();
                    m_autoConnectRequestedPermissions = true;
                }

                if (m_permissionRequestState == PermissionRequestState.ALL_PERMISSIONS_GRANTED && !m_isTangoStarted)
                {
                    Startup(null);
                }
            }

            // Update any emulation
#if UNITY_EDITOR
            if (m_isTangoStarted)
            {
                PoseProvider.UpdateTangoEmulation();
                if (m_doSlowEmulation)
                {
                    if (m_enableDepth)
                    {
                        DepthProvider.UpdateTangoEmulation();
                    }

                    if (m_enableVideoOverlay)
                    {
                        VideoOverlayProvider.UpdateTangoEmulation(m_videoOverlayUseByteBufferMethod);
                    }
                }
            }
#endif

            PoseListener.SendIfAvailable(m_enableAreaDescriptions);
            DepthListener.SendIfAvailable();
            VideoOverlayListener.SendIfAvailable();
            TangoEventListener.SendIfAvailable();
            AreaDescriptionEventListener.SendIfAvailable();

            if (m_tango3DReconstruction != null)
            {
                m_tango3DReconstruction.SendEventIfAvailable();
            }
        }

        /// <summary>
        /// Unity callback when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            PoseListener.Reset();
            DepthListener.Reset();
            VideoOverlayListener.Reset();
            TangoEventListener.Reset();
            AreaDescriptionEventListener.Reset();

            Shutdown();

            // Clean up configs.
            if (m_tangoConfig != null)
            {
                m_tangoConfig.Dispose();
                m_tangoConfig = null;
            }

            if (m_tangoRuntimeConfig != null)
            {
                m_tangoRuntimeConfig.Dispose();
                m_tangoRuntimeConfig = null;
            }

            if (m_tango3DReconstruction != null)
            {
                m_tango3DReconstruction.Dispose();
            }

            Debug.Log(CLASS_NAME + ".OnDestroy() called");
        }

        /// <summary>
        /// Data of Android message for Unity main thread to consume.
        /// </summary>
        private struct AndroidMessage
        {
            /// <summary>
            /// Type of the message. This is used to differentiate different Android callbacks.
            /// </summary>
            public AndroidMessageType m_type;

            /// <summary>
            /// Parameters data from the callback functions.
            /// </summary>
            public object[] m_messages;

            /// <summary>
            /// Constructor of Android message.
            /// </summary>
            /// <param name="type">Type of this message.</param>
            /// <param name="messages">Content of this message, it's the parameter list returned from the call.</param>
            public AndroidMessage(AndroidMessageType type, params object[] messages)
            {
                m_type = type;
                m_messages = messages;
            }
        }

        #region NATIVE_FUNCTIONS

        /// <summary>
        /// Interface for native function calls to Tango Service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct API
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_initialize(IntPtr jniEnv, IntPtr appContext);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_connect(IntPtr callbackContext, IntPtr config);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern void TangoService_disconnect();
#else
            public static int TangoService_initialize(IntPtr jniEnv, IntPtr appContext)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_connect(IntPtr callbackContext, IntPtr config)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static void TangoService_disconnect()
            {
            }
#endif
        }

        #endregion // NATIVE_FUNCTIONS
    }
}
