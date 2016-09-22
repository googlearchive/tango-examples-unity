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
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Serialization;

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
    /// Delegate for service disconnection.
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
    /// This sends out events to Components that derive from the ITangoPose, ITangoDepth, etc. interfaces and
    /// register themselves via Register.  This also sends out events to callbacks passed in through 
    /// RegisterOnTangoConnect, RegisterOnTangoDisconnect, and RegisterPermissionsCallback.
    /// 
    /// Note: To connect to the Tango Service, you should call <c>InitApplication</c> after properly registering 
    /// everything.
    /// </summary>
    public class TangoApplication : MonoBehaviour
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
        private IntPtr m_callbackContext = IntPtr.Zero;
        private bool m_isServiceInitialized = false;
        private bool m_isServiceConnected = false;
        private bool m_shouldReconnectService = false;
        private bool m_sendPermissions = false;
        private bool m_permissionsSuccessful = false;
        private bool m_displayChanged = false;
        private YUVTexture m_yuvTexture;
        private TangoConfig m_tangoConfig;
        private TangoConfig m_tangoRuntimeConfig;

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
            SERVICE_BOUND = 0x4,
        }

        /// <summary>
        /// Gets a value indicating whether this is connected to a Tango service.
        /// </summary>
        /// <value><c>true</c> if connected to a Tango service; otherwise, <c>false</c>.</value>
        public bool IsServiceConnected
        {
            get { return m_isServiceConnected; }
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

            ITangoLifecycle tangoLifecycle = tangoObject as ITangoLifecycle;
            if (tangoLifecycle != null)
            {
                _RegisterPermissionsCallback(tangoLifecycle.OnTangoPermissions);
                _RegisterOnTangoConnect(tangoLifecycle.OnTangoServiceConnected);
                _RegisterOnTangoDisconnect(tangoLifecycle.OnTangoServiceDisconnected);
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

            ITangoLifecycle tangoLifecycle = tangoObject as ITangoLifecycle;
            if (tangoLifecycle != null)
            {
                _UnregisterPermissionsCallback(tangoLifecycle.OnTangoPermissions);
                _UnregisterOnTangoConnect(tangoLifecycle.OnTangoServiceConnected);
                _UnregisterOnTangoDisconnect(tangoLifecycle.OnTangoServiceDisconnected);
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
            _ResetPermissionsFlags();
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
                Debug.Log("TangoApplication.Startup() -- ERROR: Not all required permissions were accepted yet.");
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
        /// Set the frame rate of the depth camera.
        /// 
        /// Disabling or reducing the frame rate of the depth camera when it is running can save a significant amount
        /// of battery.
        /// </summary>
        /// <param name="rate">The rate in frames per second, for the depth camera to run at.</param>
        public void SetDepthCameraRate(int rate)
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
        /// Extract a single mesh for the entire 3D Reconstruction state.
        /// </summary>
        /// <returns>Status of the extraction.</returns>
        /// <param name="vertices">Filled out with extracted vertices.</param>
        /// <param name="normals">Filled out with extracted normals.</param>
        /// <param name="colors">Filled out with extracted colors.</param>
        /// <param name="triangles">Filled out with extracted triangle indices.</param>
        /// <param name="numVertices">Filled out with the number of valid vertices.</param>
        /// <param name="numTriangles">Filled out with the number of valid triangles.</param>
        public Tango3DReconstruction.Status Tango3DRExtractWholeMesh(
            Vector3[] vertices, Vector3[] normals, Color32[] colors, int[] triangles, out int numVertices,
            out int numTriangles)
        {
            if (m_tango3DReconstruction != null)
            {
                return m_tango3DReconstruction.ExtractWholeMesh(vertices, normals, colors, triangles, out numVertices,
                                                                out numTriangles);
            }

            numVertices = 0;
            numTriangles = 0;
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
        /// Register to get an event callback when all permissions are granted.
        /// 
        /// The passed event will get called once all Tango permissions have been granted.  Registering 
        /// after all permissions have already been granted will cause the event to never fire.
        /// </summary>
        /// <param name="permissionsEventHandler">Event to call.</param>
        private void _RegisterPermissionsCallback(PermissionsEvent permissionsEventHandler)
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
        private void _UnregisterPermissionsCallback(PermissionsEvent permissionsEventHandler)
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
        private void _RegisterOnTangoConnect(OnTangoConnectEventHandler handler)
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
        private void _UnregisterOnTangoConnect(OnTangoConnectEventHandler handler)
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
        private void _RegisterOnTangoDisconnect(OnTangoDisconnectEventHandler handler)
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
        private void _UnregisterOnTangoDisconnect(OnTangoDisconnectEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoDisconnect -= handler;
            }
        }

        /// <summary>
        /// Helper method that will resume the tango services on App Resume.
        /// Locks the config again and connects the service.
        /// </summary>
        private void _ResumeTangoServices()
        {
            RequestPermissions();
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

            m_isServiceInitialized = true;
            Debug.Log(CLASS_NAME + ".Initialize() Tango was initialized!");
            return true;
        }
        
        /// <summary>
        /// Connect to the Tango Service.
        /// </summary>
        private void _TangoConnect()
        {
            Debug.Log("TangoApplication._TangoConnect()");

            if (!m_isServiceInitialized)
            {
                return;
            }

            if (!m_isServiceConnected)
            {
                m_isServiceConnected = true;
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
            AndroidHelper.UnbindTangoService();

            if (!m_isServiceConnected)
            {
                Debug.Log(CLASS_NAME + ".Disconnect() Not disconnecting from Tango Service "
                          + "as this TangoApplication was not connected");
                return;
            }

            Debug.Log(CLASS_NAME + ".Disconnect() Disconnecting from the Tango Service");
            m_isServiceConnected = false;

            // This is necessary because tango_client_api clears camera callbacks when
            // TangoService_disconnect() is called, unlike other callbacks.
            VideoOverlayListener.ClearTangoCallbacks();

            API.TangoService_disconnect();
            Debug.Log(CLASS_NAME + ".Disconnect() Tango client disconnected from service!");

            if (OnTangoDisconnect != null)
            {
                OnTangoDisconnect();
            }

#if UNITY_EDITOR
            PoseProvider.ResetTangoEmulation();
#endif
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
        /// <param name="data">Intent data.</param>
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
        /// Delegate for when connected to the Tango Android service.
        /// </summary>
        /// <param name="binder">Binder for the service.</param>
        private void _androidOnTangoServiceConnected(AndroidJavaObject binder)
        {
            Debug.Log("_androidOnTangoServiceConnected");

            // By keeping this logic in C#, the client app can respond if this call fails.
            int result = AndroidHelper.TangoSetBinder(binder);
            if (result != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Error when calling TangoService_setBinder " + result);
                _PermissionWasDenied();
            }

            _FlipBitAndCheckPermissions(PermissionsTypes.SERVICE_BOUND);
        }

        /// <summary>
        /// Delegate for when disconnected from the Tango Android service.
        /// </summary>
        private void _androidOnTangoServiceDisconnected()
        {
            Debug.Log("_androidOnTangoServiceDisconnected");
        }

        /// <summary>
        /// Delegate for the Android display rotation changed.
        /// </summary>
        private void _androidOnDisplayChanged()
        {      
            m_displayChanged = true;
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

            AndroidHelper.RegisterPauseEvent(_androidOnPause);
            AndroidHelper.RegisterResumeEvent(_androidOnResume);
            AndroidHelper.RegisterOnActivityResultEvent(_androidOnActivityResult);
            AndroidHelper.RegisterOnDisplayChangedEvent(_androidOnDisplayChanged);
            AndroidHelper.RegisterOnTangoServiceConnected(_androidOnTangoServiceConnected);
            AndroidHelper.RegisterOnTangoServiceDisconnected(_androidOnTangoServiceDisconnected);

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
        /// Reset permissions flags.
        /// </summary>
        private void _ResetPermissionsFlags()
        {
#if UNITY_EDITOR
            m_requiredPermissions = PermissionsTypes.NONE;
#else
            if (m_requiredPermissions == PermissionsTypes.NONE)
            {
                m_requiredPermissions |= (m_enableAreaDescriptions && !m_enableDriftCorrection) ? PermissionsTypes.AREA_LEARNING : PermissionsTypes.NONE;
            }

            // It is always required to rebind to the service.
            m_requiredPermissions |= PermissionsTypes.SERVICE_BOUND;
#endif
        }

        /// <summary>
        /// Flip a permission bit and check to see if all permissions were accepted.
        /// </summary>
        /// <param name="permission">Permission bit to flip.</param>
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
            else if ((m_requiredPermissions & PermissionsTypes.SERVICE_BOUND) == PermissionsTypes.SERVICE_BOUND)
            {
                if (!AndroidHelper.BindTangoService())
                {
                    _PermissionWasDenied();
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
        /// Disperse any events related to Tango functionality.
        /// </summary>
        private void Update()
        {
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
            }

            if (m_sendPermissions)
            {
                if (PermissionEvent != null)
                {
                    PermissionEvent(m_permissionsSuccessful);
                }

                if (m_permissionsSuccessful && m_autoConnectToService)
                {
                    Startup(null);
                }

                m_sendPermissions = false;
            }

            // Update any emulation
#if UNITY_EDITOR
            if(m_isServiceConnected)
            {
                PoseProvider.UpdateTangoEmulation();
                if (m_doSlowEmulation)
                {
                    if (m_enableDepth)
                    {
                        DepthProvider.UpdateTangoEmulation();
                    }

                    if(m_enableVideoOverlay)
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

            if (m_displayChanged && m_isServiceConnected)
            {
                OrientationManager.Rotation displayRotation = AndroidHelper.GetDisplayRotation();
                OrientationManager.Rotation colorCameraRotation = AndroidHelper.GetColorCameraRotation();
                TangoSupport.UpdatePoseMatrixFromDeviceRotation(displayRotation, colorCameraRotation);
                OnDisplayChanged(displayRotation, colorCameraRotation);
                m_displayChanged = false;
            }
        }

        /// <summary>
        /// Unity callback when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("TangoApplication.OnDestroy()");
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
