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
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
    Justification = "Files can start with an interface that has a different name.")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1609:PropertyDocumentationMustHaveValue",
    Justification = "The underlying rule should be removed but has not been yet.")]

namespace Tango
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using TangoExtensions;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Delegate for handling device display orientation change event.
    /// </summary>
    /// <param name="displayRotation">Rotation of current display. Index enum is same as Android screen
    /// rotation standard.</param>
    /// <param name="colorCameraRotation">Rotation of current color camera sensor. Index enum is same as Android
    /// camera rotation standard.</param>
    public delegate void OnDisplayChangedEventHandler(OrientationManager.Rotation displayRotation,
        OrientationManager.Rotation colorCameraRotation);

    /// <summary>
    /// Defines an interface to access and modify settings related to the TangoApplication.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
    internal interface ITangoApplicationSettings
    {
        bool AllowOutOfDateTangoAPI { get; set; }

        bool AreaDescriptionLearningMode { get; set; }

        bool Enable3DReconstruction { get; set; }

        bool Enable3DReconstructionColors { get; set; }

        bool Enable3DReconstructionTexCoords { get; set; }

        bool Enable3DReconstructionNormals { get; set; }

        bool EnableAreaDescriptions { get; set; }

        bool EnableCloudADF { get; set; }

        bool EnableMotionTracking { get; set; }

        bool EnableDepth { get; set; }

        bool EnableDriftCorrection { get; set; }

        bool EnableVideoOverlay { get; set; }

        bool MotionTrackingAutoReset { get; set; }

        bool VideoOverlayUseByteBufferMethod { get; set; }

        bool VideoOverlayUseTextureMethod { get; set; }

        bool VideoOverlayUseYUVTextureIdMethod { get; set; }

        float ReconstructionMeshResolution { get; set; }

#if UNITY_EDITOR
        Vector3 EmulatedAreaDescriptionStartOffset { get; set; }
#endif
    }

    /// <summary>
    /// Main entry point for the Tango Service.
    ///
    /// This component handles nearly all communication with the underlying TangoService.  You must have one of these
    /// in your scene for Tango to work.  Customization of the Tango connection can be done in the Unity editor or by
    /// programmatically setting the member flags.
    ///
    /// This sends out event callbacks to Components that implement ITangoPose, ITangoDepth, etc. interfaces and
    /// register themselves via Register.
    ///
    /// Note: To connect to the Tango Service, you should call <c>Startup</c> after properly registering
    /// everything.
    /// </summary>
    public partial class TangoApplication : MonoBehaviour, ITangoApplication
    {
#region PublicFields
        public bool m_autoConnectToService = false;
        public bool m_allowOutOfDateTangoAPI = false;
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
        public bool m_enableCloudADF = false;
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
        /// for performance reasons (some Tango devices have very high-resolution
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
#endregion

#region NonPublicFields
        private const string CLASS_NAME = "TangoApplication";

        private const int TANGO_SHUTDOWN_RATE = 5;

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
        /// Manages Tango3DReconstruction library.
        /// </summary>
        private Tango3DReconstruction m_tango3DReconstruction;

        /// <summary>
        /// A set of unresolved ux exceptions.
        /// </summary>
        private HashSet<TangoUxEnums.UxExceptionEventType> m_unresolvedUxExceptions =
            new HashSet<TangoUxEnums.UxExceptionEventType>();

        private YUVTexture m_yuvTexture;

        /// <summary>
        /// Occurs when required android permissions have been resolved.  NOTE: this event currently only
        /// fires for the TangoUx class.
        /// </summary>
        private Action<bool> m_androidPermissionsEvent;

        /// <summary>
        /// Occurs when all required tango permissions have been resolved.
        /// </summary>
        private Action<bool> m_tangoBindEvent;

        /// <summary>
        /// The transformation matrix of globalTLocal.
        /// </summary>
        private DMatrix4x4? m_globalTLocal = null;

        /// <summary>
        /// Occurs when tango service connection is complete.
        /// </summary>
        private OnTangoConnectDelegate m_tangoConnectEvent;

        /// <summary>
        /// Occurs when on tango service disconnection is complete.
        /// </summary>
        private OnTangoDisconnectDelegate m_tangoDisconnectEvent;

        private ITangoAndroidMessageManager m_androidMessageManager;

        private TangoApplicationState m_applicationState;

        private ITangoEventRegistrationManager m_eventRegistrationManager;

        private ITangoPermissionsManager m_permissionsManager;

        private ITangoSetupTeardownManager m_setupTeardownManager;

        private ITangoDepthCameraManager m_tangoDepthCameraManager;
#endregion

        /// <summary>
        /// Delegate for handling the resolution of tango service binding.
        /// </summary>
        /// <param name="success"><c>true</c> if the tango service was bound, otherwise <c>false</c>.</param>
        internal delegate void OnTangoBindDelegate(bool success);

        /// <summary>
        /// Delegate for handling service connection event.
        /// </summary>
        internal delegate void OnTangoConnectDelegate();

        /// <summary>
        /// Delegate for handling service disconnection event.
        /// </summary>
        internal delegate void OnTangoDisconnectDelegate();

        /// <summary>
        /// Delegate for handling resolution of all required android permissions.
        /// </summary>
        /// <param name="permissionsGranted"><c>true</c> if all required android permissions were granted, otherwise <c>false</c>.</param>
        private delegate void OnAndroidPermissionsDelegate(bool permissionsGranted);

#region Properties
        /// <summary>
        /// Gets or sets resolution of the reconstructed mesh if Tango 3D Reconstruction is enabled. In meters.
        /// </summary>
        public float ReconstructionMeshResolution
        {
            get { return m_3drResolutionMeters; }
            set { m_3drResolutionMeters = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether TangoApplication should run if the Tango software is out of date.
        /// </summary>
        public bool AllowOutOfDateTangoAPI
        {
            get { return m_allowOutOfDateTangoAPI; }
            set { m_allowOutOfDateTangoAPI = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Tango area description learning mode is enabled.
        /// </summary>
        public bool AreaDescriptionLearningMode
        {
            get { return m_areaDescriptionLearningMode; }
            set { m_areaDescriptionLearningMode = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Tango 3D Reconstruction is enabled.
        /// </summary>
        public bool Enable3DReconstruction
        {
            get { return m_enable3DReconstruction; }
            set { m_enable3DReconstruction = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Tango 3D Reconstruction generates vertex colors.
        /// </summary>
        public bool Enable3DReconstructionColors
        {
            get { return m_3drGenerateColor; }
            set { m_3drGenerateColor = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Tango 3D Reconstruction generates texture coordinates (UVs).
        /// </summary>
        public bool Enable3DReconstructionTexCoords
        {
            get { return m_3drGenerateTexCoord; }
            set { m_3drGenerateTexCoord = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Tango 3D Reconstruction generates vertex normals.
        /// </summary>
        public bool Enable3DReconstructionNormals
        {
            get { return m_3drGenerateNormal; }
            set { m_3drGenerateNormal = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tango area descriptions are enabled.
        /// </summary>
        public bool EnableAreaDescriptions
        {
            get { return m_enableAreaDescriptions; }
            set { m_enableAreaDescriptions = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tango cloud ADF is enabled.
        /// </summary>
        public bool EnableCloudADF
        {
            get { return m_enableCloudADF; }
            set { m_enableCloudADF = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether motion tracking is enabled.
        /// </summary>
        public bool EnableMotionTracking
        {
            get { return m_enableMotionTracking; }
            set { m_enableMotionTracking = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether depth is enabled.
        /// </summary>
        public bool EnableDepth
        {
            get { return m_enableDepth; }
            set { m_enableDepth = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether drift correction is enabled.
        /// </summary>
        public bool EnableDriftCorrection
        {
            get { return m_enableDriftCorrection; }
            set { m_enableDriftCorrection = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether video overlay is enabled.
        /// </summary>
        public bool EnableVideoOverlay
        {
            get { return m_enableVideoOverlay; }
            set { m_enableVideoOverlay = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether motion tracking auto reset is enabled.
        /// </summary>
        public bool MotionTrackingAutoReset
        {
            get { return m_motionTrackingAutoReset; }
            set { m_motionTrackingAutoReset = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether video overlay uses buffering method.
        /// </summary>
        public bool VideoOverlayUseByteBufferMethod
        {
            get { return m_videoOverlayUseByteBufferMethod; }
            set { m_videoOverlayUseByteBufferMethod = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether video overlay uses texture method.
        /// </summary>
        public bool VideoOverlayUseTextureMethod
        {
            get { return m_videoOverlayUseTextureMethod; }
            set { m_videoOverlayUseTextureMethod = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether video overlay uses YUVTextureId method.
        /// </summary>
        public bool VideoOverlayUseYUVTextureIdMethod
        {
            get { return m_videoOverlayUseYUVTextureIdMethod; }
            set { m_videoOverlayUseYUVTextureIdMethod = value; }
        }

#if UNITY_EDITOR
        /// <summary>
        /// The start offset for the emulated area description.
        /// </summary>
        public Vector3 EmulatedAreaDescriptionStartOffset { get { return m_emulatedAreaDescriptionStartOffset; } set { m_emulatedAreaDescriptionStartOffset = value; } }
#endif

        /// <summary>
        /// Gets a value indicating whether all Tango permissions have been granted.
        /// </summary>
        public bool HasRequiredPermissions
        {
            get { return m_permissionsManager.PermissionRequestState == PermissionRequestState.ALL_PERMISSIONS_GRANTED; }
        }

        /// <summary>
        /// Gets a value indicating whether this is connected to a Tango service.
        /// </summary>
        /// <value><c>true</c> if connected to a Tango service; otherwise, <c>false</c>.</value>
        public bool IsServiceConnected
        {
            get { return m_applicationState != null ? m_applicationState.IsTangoStarted : false; }
        }

        /// <summary>
        /// Gets the Tango config.  Useful for debugging.
        /// </summary>
        /// <value>The config.</value>
        internal ITangoConfig Config
        {
            get { return m_applicationState != null ? m_applicationState.TangoConfig : null; }
        }

        /// <summary>
        /// Gets the current Tango runtime config.  Useful for debugging.
        /// </summary>
        /// <value>The current runtime config.</value>
        internal ITangoConfig RuntimeConfig
        {
            get { return m_applicationState != null ? m_applicationState.TangoRuntimeConfig : null; }
        }
#endregion

        /// <summary>
        /// Get the Tango Service version name.
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

#region UnityCallbacks
        /// <summary>
        /// Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            if (!AndroidHelper.LoadTangoLibrary())
            {
                Debug.Log("Unable to load Tango library.  Things may not work.");
                return;
            }

            // Generate new application state.
            m_applicationState = new TangoApplicationState();

            // Initialize the registration manager.
            m_eventRegistrationManager = new TangoEventRegistrationManager(this);

            // Initializae the permissions manager.
            m_permissionsManager =
                new TangoPermissionsManager(this,
                    AndroidHelperWrapper.Instance,
                    delegate(bool granted)
                    {
                        // Cannot pass event directly due to delegate instance immutability.
                        if (m_androidPermissionsEvent != null)
                        {
                            m_androidPermissionsEvent(granted);
                        }
                    },
                    delegate(bool granted)
                    {
                        // Cannot pass event directly due to delegate instance immutability.
                        if (m_tangoBindEvent != null)
                        {
                            m_tangoBindEvent(granted);
                        }
                    });

            // Initialize the depth camera manager.
            m_tangoDepthCameraManager = new TangoDepthCameraManager(m_applicationState, new DepthListenerWrapper());

            // Generate m_yuvTexture if needed.
            if (m_enableVideoOverlay)
            {
                int yTextureWidth = 0;
                int yTextureHeight = 0;
                int uvTextureWidth = 0;
                int uvTextureHeight = 0;

                m_yuvTexture = new YUVTexture(yTextureWidth, yTextureHeight, uvTextureWidth, uvTextureHeight,
                    TextureFormat.RGBA32, false);
            }
            else
            {
                m_yuvTexture = null;
            }

            // Initialize the setup and teardown manager.
            m_setupTeardownManager =
                new TangoSetupTeardownManager(this, m_applicationState, GetComponent<TangoUx>(),
                delegate()
                {
                    // Cannot pass event directly due to delegate instance immutability.
                    if (m_tangoConnectEvent != null)
                    {
                        m_tangoConnectEvent();
                    }
                },
                delegate()
                {
                    // Cannot pass event directly due to delegate instance immutability.
                    if (m_tangoDisconnectEvent != null)
                    {
                        m_tangoDisconnectEvent();
                    }
                },
                m_yuvTexture);

            // Initialize the android message manager.
            m_androidMessageManager =
                new TangoAndroidMessageManager(m_applicationState, m_permissionsManager, m_tangoDepthCameraManager,
                    m_setupTeardownManager.OnAndroidPauseResumeAsync, delegate(
                    OrientationManager.Rotation displayRotation,
                    OrientationManager.Rotation colorCameraRotation)
                    {
                        // Cannot pass event directly due to delegate instance immutability.
                        if (OnDisplayChanged != null)
                        {
                            OnDisplayChanged(displayRotation, colorCameraRotation);
                        }
                    },
                    AndroidHelperWrapper.Instance);

            m_androidMessageManager.RegisterHandlers();
            if (m_enableDepth)
            {
                DepthListener.SetPointCloudLimit(m_initialPointCloudMaxPoints);
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

#if UNITY_EDITOR
            // We must initialize this on the main Unity thread, since the value
            // is sometimes used within a separate saving thread.
            AreaDescription.GenerateEmulatedSavePath();
#endif

            // Importing and exporting Area Descriptions can be done before you connect. We must
            // propogate those events if they happen.
            AreaDescriptionEventListener.SetCallback();

            if (m_adjustScreenResolution)
            {
                _ChangeResolutionForPerformance();
            }

            if (m_keepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
        /// Unity Update() method.
        /// </summary>
        public void Update()
        {
            m_androidMessageManager.DrainQueue();
            if (m_setupTeardownManager.IsApplicationPausedAsync)
            {
                return;
            }

            // Autoconnect requesting permissions MUST happen after Start() to allow all other scripts to call Register()
            // in Awake() or Start().
            if (m_autoConnectToService)
            {
                if (m_permissionsManager.PermissionRequestState == PermissionRequestState.NONE)
                {
                    m_permissionsManager.RequestPermissions();
                }

                if (m_permissionsManager.PermissionRequestState == PermissionRequestState.ALL_PERMISSIONS_GRANTED && !m_applicationState.IsTangoStarted)
                {
                    Startup(null);
                }
            }

            _UpdateEmulation();
            _SendQueuedAsyncEvents();
        }

        /// <summary>
        /// Unity callback when this object is destroyed.
        /// </summary>
        public void OnDestroy()
        {
            PoseListener.Reset();
            DepthListener.Reset();
            VideoOverlayListener.Reset();
            TangoEventListener.Reset();
            AreaDescriptionEventListener.Reset();

            Shutdown();
            m_setupTeardownManager.CleanUpOnDispose();

            if (m_tango3DReconstruction != null)
            {
                m_tango3DReconstruction.Dispose();
            }

            Debug.Log(CLASS_NAME + ".OnDestroy() called");
        }
#endregion

        /// <summary>
        /// DEPRECATED: Get the video overlay texture.
        /// </summary>
        /// <returns>The video overlay texture.</returns>
        public YUVTexture GetVideoOverlayTextureYUV()
        {
            return m_yuvTexture;
        }

#region ManualTangoSetup
        /// <summary>
        /// Registers an object to receive event callbacks from TangoApplication.
        /// </summary>
        /// <param name="tangoObject">An object that implements ITangoDepth, ITangoEvent, ITangoPose, ITangoVideoOverlay,
        /// or ITangoExperimentalTangoVideoOverlay.</param>
        public void Register(object tangoObject)
        {
            m_eventRegistrationManager.RegisterObject(tangoObject);
        }

        /// <summary>
        /// Unregister from Tango callbacks.  See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="tangoObject">An object that implements ITangoDepth, ITangoEvent, ITangoPose, ITangoVideoOverlay,
        /// or ITangoExperimentalTangoVideoOverlay.</param>
        public void Unregister(System.Object tangoObject)
        {
            m_eventRegistrationManager.UnregisterObject(tangoObject);
        }

        /// <summary>
        /// Requests android permissions needed by the tango application.
        /// </summary>
        public void RequestPermissions()
        {
            m_permissionsManager.RequestPermissions();
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
            if (m_permissionsManager.PermissionRequestState != PermissionRequestState.ALL_PERMISSIONS_GRANTED)
            {
                Debug.LogError(CLASS_NAME
                    + ".Startup() -- ERROR: Not all required permissions were accepted yet. Needed: "
                    + m_permissionsManager.PendingRequiredPermissions.ToString());
                return;
                }

            if (Enable3DReconstruction)
            {
                Register(m_tango3DReconstruction);
            }

            m_setupTeardownManager.Startup(areaDescription);
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
            m_tangoDepthCameraManager.SetDepthCameraRate(TANGO_SHUTDOWN_RATE, false);
            m_permissionsManager.Reset();
            m_setupTeardownManager.Shutdown();
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
#endregion

#region DepthCameraAccess
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
                m_tangoDepthCameraManager.SetDepthCameraRate(0);
                break;

            case TangoEnums.TangoDepthCameraRate.MAXIMUM:
                // Set the depth frame rate to a sufficiently high number, it will get rounded down.  There is no
                // way to actually get the maximum value to pass in.
                m_tangoDepthCameraManager.SetDepthCameraRate(5);
                break;
            }
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
            m_tangoDepthCameraManager.SetDepthCameraRate(rate);
        }
#endregion

#region 3DReconstruction
        /// <summary>
        /// Enable or disable the 3D Reconstruction.
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public void Set3DReconstructionEnabled(bool enabled)
        {
            m_tango3DReconstruction.SetEnabled(enabled);
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
        /// <param name="vertices">A list to which mesh vertices will be appended; can be null.</param>
        /// <param name="normals">A list to which mesh normals will be appended; can be null.</param>
        /// <param name="colors">A list to which mesh colors will be appended; can be null.</param>
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
        /// Returns Status.SUCCESS if the voxels are fully extracted and stored in the array.  In this case, <c>numVoxels</c>
        /// will say how many voxels are used; the rest of the array is untouched.
        ///
        /// Returns Status.INVALID if the array length does not exactly equal the number of voxels in a single grid
        /// index.  By default, the number of voxels in a grid index is 16*16*16.
        ///
        /// Returns Status.INVALID if some other error occurs.
        /// </returns>
        /// <param name="gridIndex">Grid index to extract.</param>
        /// <param name="voxels">
        /// On successful extraction this is filled out with the signed distance voxels.
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
        /// Gets the transformation matrix from a local frame to a global frame.
        /// </summary>
        ///
        /// In cloud area description mode, a local frame has to be used to avoid floating point precision errors
        /// when calculating in the global frame. The main requirement of the local frame is that it should be close to
        /// the device position to avoid precision errors. By default, this method uses an local frame centred on the
        /// device's start of service position.
        ///
        /// Otherwise, by default, the transformation in non-cloud area description mode is the identity matrix.
        ///
        /// <returns><c>true</c>, if a valid local to global transformation was returned, <c>false</c>
        /// otherwise.</returns>
        /// <param name="globalTLocal">Transformation matrix from the local frame to global frame.</param>
        public bool GetGlobalTLocal(out DMatrix4x4 globalTLocal)
        {
            if (m_globalTLocal == null)
            {
                // No existing value, set defaults
                if (!m_enableCloudADF)
                {
                    m_globalTLocal = DMatrix4x4.Identity;
                }
                else
                {
                    TangoCoordinateFramePair pair;
                    pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_GLOBAL_WGS84;
                    pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                    TangoPoseData ecefTStartOfService = new TangoPoseData();

                    PoseProvider.GetPoseAtTime(ecefTStartOfService, 0.0, pair);

                    if (ecefTStartOfService.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
                    {
                        // Set the out parameter, but not the member variable
                        globalTLocal = DMatrix4x4.Identity;
                        return false;
                    }

                    m_globalTLocal = DMatrix4x4.TR(ecefTStartOfService.translation,
                                                   ecefTStartOfService.orientation);
                }
            }

            globalTLocal = m_globalTLocal.Value;
            return true;
        }

        /// <summary>
        /// Sets the transformation matrix from a local frame to a global frame.
        /// </summary>
        ///
        /// This method can be used to override the default behaviour for determining the local frame of reference with
        /// respect to a global frame. To return to the default behaviour, set this to null.
        ///
        /// <param name="globalTLocal">Transformation matrix from local frame to global frame (nullable).</param>
        public void SetGlobalTLocal(DMatrix4x4? globalTLocal)
        {
            m_globalTLocal = globalTLocal;
        }
#endregion

#region PrivateMethods
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
        /// Updates the tango emulation.
        /// </summary>
        private void _UpdateEmulation()
        {
#if UNITY_EDITOR
            if (m_applicationState.IsTangoStarted)
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
        }

        /// <summary>
        /// Dispatches all events that arrived outside the Unity thread.
        /// </summary>
        private void _SendQueuedAsyncEvents()
        {
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
#endregion
    }

    /// <summary>
    /// Implements configuration related inner entities of TangoApplication.
    /// </summary>
    public partial class TangoApplication
    {
        /// <summary>
        /// Represents state information for the TangoApplication.
        /// </summary>
        internal class TangoApplicationState
        {
            private ITangoConfig m_tangoConfig;

            private ITangoConfig m_tangoRuntimeConfig;

            /// <summary>
            /// Object contstructor.
            /// </summary>
            public TangoApplicationState()
            {
                IsTangoUpToDate = false;
                IsTangoStarted = false;
            }

            /// <summary>
            /// Gets or sets a value indicating whether the device has a compatible version of Tango Core.
            /// </summary>
            public bool IsTangoUpToDate { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether TangoService is running and providing data to client,
            /// e.g motion tracking and point cloud data.
            /// </summary>
            public bool IsTangoStarted { get; set; }

            /// <summary>
            /// Gets the tango configuration object.
            /// </summary>
            public virtual ITangoConfig TangoConfig
            {
                get
                {
                    if (m_tangoConfig == null)
                    {
                        m_tangoConfig = new TangoConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_DEFAULT);
                    }

                    return m_tangoConfig;
                }
            }

            /// <summary>
            /// Gets the runtime tango configuration object.
            /// </summary>
            public virtual ITangoConfig TangoRuntimeConfig
            {
                get
                {
                    if (m_tangoRuntimeConfig == null)
                    {
                        m_tangoRuntimeConfig = new TangoConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_RUNTIME);
                    }

                    return m_tangoRuntimeConfig;
                }
            }

            /// <summary>
            /// Clears tango configurations and disposes of related resources.
            /// </summary>
            public virtual void ClearConfigs()
            {
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
        }
    }

    /// <summary>
    ///  Implements depth camera related inner entities of TangoApplication.
    /// </summary>
    public partial class TangoApplication
    {
        /// <summary>
        /// Interface that manages interactions with the depth camera.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
        internal interface ITangoDepthCameraManager
        {
            int LastSetDepthCameraRate { get; }

            void SetDepthCameraRate(int rate, bool maintainOnResume = true);

            void SetMaxDepthPoints(int maxDepthPoints);
        }

        /// <summary>
        /// Manages interactions with the depth camera.
        /// </summary>
        internal class TangoDepthCameraManager : ITangoDepthCameraManager
        {
            private TangoApplicationState m_tangoApplicationState;

            private IDepthListenerWrapper m_depthListener;

            /// <summary>
            /// Constructor for TangoDepthCameraManager.
            /// </summary>
            /// <param name="tangoApplicationState">The application state.</param>
            /// <param name="depthListener">The depth listener.</param>
            public TangoDepthCameraManager(TangoApplicationState tangoApplicationState, IDepthListenerWrapper depthListener)
            {
                m_tangoApplicationState = tangoApplicationState;
                m_depthListener = depthListener;
            }

            /// <summary>
            /// Gets the last rate set by the depth camera.
            /// </summary>
            /// <returns></returns>
            public int LastSetDepthCameraRate { get; private set; }

            /// <summary>
            /// Sets the depth camera rate.
            /// </summary>
            /// <param name="rate">The depth camera rate.</param>
            /// <param name="maintainOnResume">Whether to maintain the new rate on android resume event.</param>
            public void SetDepthCameraRate(int rate, bool maintainOnResume = true)
            {
                if (rate < 0)
                {
                    Debug.Log("Invalid rate passed to SetDepthCameraRate");
                    return;
                }

                _UpdateDepthCameraRateConfig(rate);
                LastSetDepthCameraRate = maintainOnResume ? rate : LastSetDepthCameraRate;
            }

            /// <summary>
            /// Sets the maximum number of depth camera points.
            /// </summary>
            /// <param name="maxDepthPoints">The maximum number of points.</param>
            public void SetMaxDepthPoints(int maxDepthPoints)
            {
                m_depthListener.SetPointCloudLimit(maxDepthPoints);
            }

            /// <summary>
            /// Updates the depth camera rate via the tango runtime config.
            /// </summary>
            /// <param name="rate">The new depth camera rate.</param>
            private void _UpdateDepthCameraRateConfig(int rate)
            {
                ITangoConfig tangoRuntimeConfig = m_tangoApplicationState.TangoRuntimeConfig;
                if (tangoRuntimeConfig == null)
                {
                    Debug.Log("Failed to set depth camera due to invalid runtime configuration.");
                    return;
                }

                tangoRuntimeConfig.SetInt32(TangoConfig.Keys.RUNTIME_DEPTH_FRAMERATE, rate);
                tangoRuntimeConfig.SetRuntimeConfig();
            }
        }
    }

    /// <summary>
    /// Implements registration related inner entities of TangoApplication.
    /// </summary>
    public partial class TangoApplication
    {
        /// <summary>
        /// Interface that manages registration and deregistration on objects from the Tango device.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
        internal interface ITangoEventRegistrationManager
        {
            void RegisterObject(object tangoObject);

            void UnregisterObject(System.Object tangoObject);
        }

        /// <summary>
        /// Manages registration and deregistration on objects from the Tango device.
        /// </summary>
        internal class TangoEventRegistrationManager : ITangoEventRegistrationManager
        {
            private TangoApplication m_tangoApplication;

            /// <summary>
            /// Constructor for TangoEventRegistrationManager.
            /// </summary>
            /// <param name="tangoApplication">The tango application.</param>
            public TangoEventRegistrationManager(TangoApplication tangoApplication)
            {
                m_tangoApplication = tangoApplication;
            }

            /// <summary>
            /// Registers an object for tango event callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            public void RegisterObject(object tangoObject)
            {
                _RegistrationChange(tangoObject, true);
            }

            /// <summary>
            /// Unregisters an object for tango event callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            public void UnregisterObject(System.Object tangoObject)
            {
                _RegistrationChange(tangoObject, false);
            }

            /// <summary>
            /// Registers or unregisters an object for callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            /// <param name="isRegister">If this is a registration request (otherwise deregistration).</param>
            private void _RegistrationChange(object tangoObject, bool isRegister)
            {
                _RegistrationChangeDefault(tangoObject, isRegister);

                if (m_tangoApplication.m_enableMotionTracking)
                {
                    _RegistrationChangeMotionTracking(tangoObject, isRegister);
                }

                if (m_tangoApplication.m_enableDepth)
                {
                    _RegistrationChangeDepth(tangoObject, isRegister);
                }

                if (m_tangoApplication.m_enableVideoOverlay)
                {
                    _RegistrationChangeVideoOverlay(tangoObject, isRegister);
                }

                if (m_tangoApplication.m_enable3DReconstruction)
                {
                   _RegistrationChange3DReconstruction(tangoObject, isRegister);
                }
            }

            /// <summary>
            /// Registers or unregisters an object for a default set of callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            /// <param name="isRegister">If this is a registration request (otherwise deregistration).</param>
            private void _RegistrationChangeDefault(object tangoObject, bool isRegister)
            {
                tangoObject.SafeConvert<ITangoAreaDescriptionEvent>((areaDescriptionEvent) =>
                {
                    if (isRegister)
                    {
                        AreaDescriptionEventListener.Register(areaDescriptionEvent.OnAreaDescriptionImported,
                            areaDescriptionEvent.OnAreaDescriptionExported);
                    }
                    else
                    {
                        AreaDescriptionEventListener.Unregister(areaDescriptionEvent.OnAreaDescriptionImported,
                            areaDescriptionEvent.OnAreaDescriptionExported);
                    }
                });

                tangoObject.SafeConvert<ITangoEvent>((tangoEvent) =>
                {
                    if (isRegister)
                    {
                        TangoEventListener.RegisterOnTangoEventAvailable(tangoEvent.OnTangoEventAvailableEventHandler);
                    }
                    else
                    {
                        TangoEventListener.UnregisterOnTangoEventAvailable(tangoEvent.OnTangoEventAvailableEventHandler);
                    }
                });

                tangoObject.SafeConvert<ITangoEventMultithreaded>((tangoEventMultithreaded) =>
                {
                    if (isRegister)
                    {
                        TangoEventListener.RegisterOnTangoEventMultithreadedAvailable(
                            tangoEventMultithreaded.OnTangoEventMultithreadedAvailableEventHandler);
                    }
                    else
                    {
                        TangoEventListener.UnregisterOnTangoEventMultithreadedAvailable(
                            tangoEventMultithreaded.OnTangoEventMultithreadedAvailableEventHandler);
                    }
                });

                tangoObject.SafeConvert<TangoUx>((tangoUx) =>
                {
                    if (isRegister)
                    {
                        m_tangoApplication.m_androidPermissionsEvent += tangoUx.OnAndroidPermissions;
                    }
                    else
                    {
                        m_tangoApplication.m_androidPermissionsEvent -= tangoUx.OnAndroidPermissions;
                    }
                });

                tangoObject.SafeConvert<ITangoLifecycle>((tangoLifecycle) =>
                {
                    if (isRegister)
                    {
                        // We attach OnTangoBindEvent to OnTangoPermissions lifecycle for backwards compatability.
                        m_tangoApplication.m_tangoBindEvent += tangoLifecycle.OnTangoPermissions;
                        m_tangoApplication.m_tangoConnectEvent += tangoLifecycle.OnTangoServiceConnected;
                        m_tangoApplication.m_tangoDisconnectEvent += tangoLifecycle.OnTangoServiceDisconnected;
                    }
                    else
                    {
                        m_tangoApplication.m_tangoBindEvent -= tangoLifecycle.OnTangoPermissions;
                        m_tangoApplication.m_tangoConnectEvent -= tangoLifecycle.OnTangoServiceConnected;
                        m_tangoApplication.m_tangoDisconnectEvent -= tangoLifecycle.OnTangoServiceDisconnected;
                    }
                });
            }

            /// <summary>
            /// Registers or unregisters an object for motion tracking callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            /// <param name="isRegister">If this is a registration request (otherwise deregistration).</param>
            private void _RegistrationChangeMotionTracking(object tangoObject, bool isRegister)
            {
                tangoObject.SafeConvert<ITangoPose>((poseHandler) =>
                {
                    if (isRegister)
                    {
                        PoseListener.RegisterTangoPoseAvailable(poseHandler.OnTangoPoseAvailable);
                    }
                    else
                    {
                        PoseListener.UnregisterTangoPoseAvailable(poseHandler.OnTangoPoseAvailable);
                    }
                });
            }

            /// <summary>
            /// Registers or unregisters an object for depth callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            /// <param name="isRegister">If this is a registration request (otherwise deregistration).</param>
            private void _RegistrationChangeDepth(object tangoObject, bool isRegister)
            {
                tangoObject.SafeConvert<ITangoPointCloud>((pointCloudHandler) =>
                {
                    if (isRegister)
                    {
                        DepthListener.RegisterOnPointCloudAvailable(pointCloudHandler.OnTangoPointCloudAvailable);
                    }
                    else
                    {
                        DepthListener.UnregisterOnPointCloudAvailable(pointCloudHandler.OnTangoPointCloudAvailable);
                    }
                });

                tangoObject.SafeConvert<ITangoPointCloudMultithreaded>((pointCloudMultithreadedHandler) =>
                {
                    if (isRegister)
                    {
                        DepthListener.RegisterOnPointCloudMultithreadedAvailable(
                            pointCloudMultithreadedHandler.OnTangoPointCloudMultithreadedAvailable);
                    }
                    else
                    {
                        DepthListener.UnregisterOnPointCloudMultithreadedAvailable(
                            pointCloudMultithreadedHandler.OnTangoPointCloudMultithreadedAvailable);
                    }
                });

                tangoObject.SafeConvert<ITangoDepth>((depthHandler) =>
                {
                    if (isRegister)
                    {
                        DepthListener.RegisterOnTangoDepthAvailable(depthHandler.OnTangoDepthAvailable);
                    }
                    else
                    {
                        DepthListener.UnregisterOnTangoDepthAvailable(depthHandler.OnTangoDepthAvailable);
                    }
                });

                tangoObject.SafeConvert<ITangoDepthMultithreaded>((depthMultithreadedHandler) =>
                {
                    if (isRegister)
                    {
                        DepthListener.RegisterOnTangoDepthMultithreadedAvailable(
                            depthMultithreadedHandler.OnTangoDepthMultithreadedAvailable);
                    }
                    else
                    {
                        DepthListener.RegisterOnTangoDepthMultithreadedAvailable(
                            depthMultithreadedHandler.OnTangoDepthMultithreadedAvailable);
                    }
                });
            }

            /// <summary>
            /// Registers or unregisters an object for video overlay callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            /// <param name="isRegister">If this is a registration request (otherwise deregistration).</param>
            private void _RegistrationChangeVideoOverlay(object tangoObject, bool isRegister)
            {
                if (m_tangoApplication.m_videoOverlayUseTextureMethod)
                {
                    tangoObject.SafeConvert<ITangoCameraTexture>((handler) =>
                    {
                        if (isRegister)
                        {
                            VideoOverlayListener.RegisterOnTangoCameraTextureAvailable(handler.OnTangoCameraTextureAvailable);
                        }
                        else
                        {
                            VideoOverlayListener.UnregisterOnTangoCameraTextureAvailable(handler.OnTangoCameraTextureAvailable);
                        }
                    });
                }

                if (m_tangoApplication.m_videoOverlayUseYUVTextureIdMethod)
                {
                    tangoObject.SafeConvert<IExperimentalTangoVideoOverlay>((handler) =>
                    {
                        if (isRegister)
                        {
                            VideoOverlayListener.RegisterOnTangoYUVTextureAvailable(handler.OnExperimentalTangoImageAvailable);
                        }
                        else
                        {
                            VideoOverlayListener.UnregisterOnTangoYUVTextureAvailable(handler.OnExperimentalTangoImageAvailable);
                        }
                    });
                }

                if (m_tangoApplication.m_videoOverlayUseByteBufferMethod)
                {
                    tangoObject.SafeConvert<ITangoVideoOverlay>((handler) =>
                    {
                        if (isRegister)
                        {
                            VideoOverlayListener.RegisterOnTangoImageAvailable(handler.OnTangoImageAvailableEventHandler);
                        }
                        else
                        {
                            VideoOverlayListener.UnregisterOnTangoImageAvailable(handler.OnTangoImageAvailableEventHandler);
                        }
                    });

                    tangoObject.SafeConvert<ITangoVideoOverlayMultithreaded>((multithreadedHandler) =>
                    {
                        if (isRegister)
                        {
                            VideoOverlayListener.RegisterOnTangoImageMultithreadedAvailable(multithreadedHandler.OnTangoImageMultithreadedAvailable);
                        }
                        else
                        {
                            VideoOverlayListener.UnregisterOnTangoImageMultithreadedAvailable(multithreadedHandler.OnTangoImageMultithreadedAvailable);
                        }
                    });
                }
            }

            /// <summary>
            /// Registers or unregisters an object for 3d reconstruction callbacks.
            /// </summary>
            /// <param name="tangoObject">The object.</param>
            /// <param name="isRegister">If this is a registration request (otherwise deregistration).</param>
            private void _RegistrationChange3DReconstruction(object tangoObject, bool isRegister)
            {
                tangoObject.SafeConvert<ITango3DReconstruction>((t3drHandler) =>
                {
                    if (isRegister)
                    {
                        m_tangoApplication.m_tango3DReconstruction.RegisterGridIndicesDirty(t3drHandler.OnTango3DReconstructionGridIndicesDirty);
                    }
                    else
                    {
                        m_tangoApplication.m_tango3DReconstruction.UnregisterGridIndicesDirty(t3drHandler.OnTango3DReconstructionGridIndicesDirty);
                    }
                });
            }
        }
    }

    /// <summary>
    /// Implements setup/teardown related inner entities of TangoApplication.
    /// </summary>
    public partial class TangoApplication
    {
        /// <summary>
        /// Interface that manages the setup and teardown process for a tango application.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
        internal interface ITangoSetupTeardownManager
        {
            bool IsApplicationPausedAsync { get; }

            void OnAndroidPauseResumeAsync(bool isPaused);

            void Startup(AreaDescription areaDescription);

            void Shutdown();

            void CleanUpOnDispose();
        }

        /// <summary>
        /// Marshals API calls needed by TangoApplication bewtween managed and unmanaged code.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1600:ElementsMustBeDocumented", Justification = "Marshals lower-level API.")]
        private struct API
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_connect(IntPtr callbackContext, IntPtr config);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern void TangoService_disconnect();
#else
            public static int TangoService_connect(IntPtr callbackContext, IntPtr config)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static void TangoService_disconnect()
            {
            }
#endif
        }

        /// <summary>
        /// Manages the setup and teardown process for a tango application.
        /// </summary>
        internal class TangoSetupTeardownManager : ITangoSetupTeardownManager
        {
            private ITangoApplicationSettings m_applicationSettings;
            private TangoApplicationState m_applicationState;
            private TangoUx m_tangoUx;
            private Action m_fireTangeConnectEvent;
            private Action m_fireTangoDisconnectEvent;
            private YUVTexture m_yuvTexture;
            private object m_tangoLifecycleLock = new object();
            private bool m_isApplicationPaused = false;

            /// <summary>
            /// Constructor for TangoSetupTeardownManager.
            /// </summary>
            /// <param name="applicationSettings">The application settings.</param>
            /// <param name="applicationState">The application state.</param>
            /// <param name="tangoUx">The tangoUx object.</param>
            /// <param name="fireTangoConnectEvent">Callback that fires tango connected event.</param>
            /// <param name="fireTangoDisconnectEvent">Callback that fires tango disconnected event.</param>
            /// <param name="yuvTexture">The yuvTexture.</param>
            public TangoSetupTeardownManager(ITangoApplicationSettings applicationSettings, TangoApplicationState applicationState,
                TangoUx tangoUx, Action fireTangoConnectEvent, Action fireTangoDisconnectEvent, YUVTexture yuvTexture)
            {
                m_applicationSettings = applicationSettings;
                m_applicationState = applicationState;
                m_tangoUx = tangoUx;
                m_fireTangeConnectEvent = fireTangoConnectEvent;
                m_fireTangoDisconnectEvent = fireTangoDisconnectEvent;
                m_yuvTexture = yuvTexture;
            }

            /// <summary>
            /// Gets a value indicating whether the android device is paused.
            /// </summary>
            public bool IsApplicationPausedAsync
            {
                get
                {
                    lock (m_tangoLifecycleLock)
                    {
                        return m_isApplicationPaused;
                    }
                }

                private set
                {
                    lock (m_tangoLifecycleLock)
                    {
                        m_isApplicationPaused = value;
                    }
                }
            }

            /// <summary>
            /// Performs startup of the Tango service.
            /// </summary>
            /// <param name="areaDescription">The area description.</param>
            public void Startup(AreaDescription areaDescription)
            {
                if (!_VerifyTangoVersion())
                {
                    return;
                }

                if (m_applicationSettings.EnableVideoOverlay && m_applicationSettings.VideoOverlayUseYUVTextureIdMethod)
                {
                   _InitializeVideoOverlayTextureSizes();
                }

                _InitializeMotionTracking(areaDescription != null ? areaDescription.m_uuid : null);
                _SetCallbacks();
                _TangoConnect();
            }

            /// <summary>
            /// Performs shutdown of the tango service.
            /// </summary>
            public void Shutdown()
            {
                _TangoDisconnect();
            }

            /// <summary>
            /// Handles when android device pauses or resumes asyncronously.
            /// </summary>
            /// <param name="isPaused">Is this a pause (otherwise resume).</param>
            public void OnAndroidPauseResumeAsync(bool isPaused)
            {
                IsApplicationPausedAsync = isPaused;
                if (isPaused)
                {
                    _TangoDisconnect(true);
                }
            }

            /// <summary>
            /// Cleans up any used resources.
            /// </summary>
            public void CleanUpOnDispose()
            {
                m_applicationState.ClearConfigs();
            }

            /// <summary>
            /// Verifies that the tango api version is up-to-date.
            /// </summary>
            /// <returns>True if tango version is up-to-date, false otherwise.</returns>
            private bool _VerifyTangoVersion()
            {
                if (!AndroidHelper.IsTangoCoreUpToDate())
                {
                    Debug.LogWarning(string.Format(CLASS_NAME
                        + ".Initialize() Invalid API version. Please update Project Tango Core to at least {0}.",
                        AndroidHelper.TANGO_MINIMUM_VERSION_CODE));
                    if (!m_applicationSettings.AllowOutOfDateTangoAPI)
                    {
                        if (m_tangoUx != null && m_tangoUx.isActiveAndEnabled)
                        {
                            m_tangoUx.ShowTangoOutOfDate();
                        }

                        return false;
                    }
                }

                m_applicationState.IsTangoUpToDate = true;
                Debug.Log(CLASS_NAME + ".Initialize() Tango was initialized!");
                return true;
            }

            /// <summary>
            /// Sets the listener callbacks.
            /// </summary>
            private void _SetCallbacks()
            {
                var tangoConfig = m_applicationState.TangoConfig;

                if (tangoConfig.SetBool(TangoConfig.Keys.ENABLE_DEPTH_PERCEPTION_BOOL, m_applicationSettings.EnableDepth)
                    && tangoConfig.SetInt32(TangoConfig.Keys.DEPTH_MODE, (int)TangoConfig.DepthMode.XYZC)
                    && m_applicationSettings.EnableDepth)
                {
                    DepthListener.SetCallback();
                }

                if (tangoConfig.SetBool(TangoConfig.Keys.ENABLE_COLOR_CAMERA_BOOL, m_applicationSettings.EnableVideoOverlay) &&
                    m_applicationSettings.EnableVideoOverlay)
                {
                    _SetVideoOverlayCallbacks();
                }

                TangoEventListener.SetCallback();
            }

            /// <summary>
            /// Initializes the sizes of video overlay textures.
            /// </summary>
            private void _InitializeVideoOverlayTextureSizes()
            {
                var tangoConfig = m_applicationState.TangoConfig;
                int yTextureWidth = 0;
                int yTextureHeight = 0;
                int uvTextureWidth = 0;
                int uvTextureHeight = 0;

                tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_Y_TEXTURE_WIDTH,
                    ref yTextureWidth);
                tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_Y_TEXTURE_HEIGHT,
                    ref yTextureHeight);
                tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_UV_TEXTURE_WIDTH,
                    ref uvTextureWidth);
                tangoConfig.GetInt32(TangoConfig.Keys.EXPERIMENTAL_UV_TEXTURE_HEIGHT,
                    ref uvTextureHeight);

                if (yTextureWidth == 0 || yTextureHeight == 0 || uvTextureWidth == 0 || uvTextureHeight == 0)
                {
                    Debug.Log("Video overlay texture sizes were not set properly");
                }

                m_yuvTexture.ResizeAll(yTextureWidth, yTextureHeight, uvTextureWidth, uvTextureHeight);
            }

            /// <summary>
            /// Initialize motion tracking.
            /// </summary>
            /// <param name="uuid">ADF UUID to load.</param>
            private void _InitializeMotionTracking(string uuid)
            {
                Debug.Log("TangoApplication._InitializeMotionTracking(" + uuid + ")");

                var tangoConfig = m_applicationState.TangoConfig;
                System.Collections.Generic.List<TangoCoordinateFramePair> framePairs =
                    new System.Collections.Generic.List<TangoCoordinateFramePair>();
                bool usedUUID = false;

                if (tangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_BOOL,
                    m_applicationSettings.EnableMotionTracking)
                    && m_applicationSettings.EnableMotionTracking)
                {
                    TangoCoordinateFramePair motionTracking;
                    motionTracking.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                    motionTracking.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                    framePairs.Add(motionTracking);

                    if (m_applicationSettings.EnableAreaDescriptions)
                    {
                        if (!m_applicationSettings.EnableDriftCorrection)
                        {
                            tangoConfig.SetBool(TangoConfig.Keys.ENABLE_AREA_LEARNING_BOOL, m_applicationSettings.AreaDescriptionLearningMode);

                            if (!string.IsNullOrEmpty(uuid))
                            {
                                if (tangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, uuid))
                                {
                                    usedUUID = true;
                                }
                            }

                            if (m_applicationSettings.EnableCloudADF)
                            {
                                tangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, string.Empty);
                                tangoConfig.SetBool(TangoConfig.Keys.ENABLE_CLOUD_ADF_BOOL, true);
                                Debug.Log("Local AreaDescription cannot be loaded when cloud ADF is enabled, Tango is starting"
                                    + "with cloud Area Description only." + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            tangoConfig.SetBool(TangoConfig.Keys.EXPERIMENTAL_ENABLE_DRIFT_CORRECTION_BOOL,
                                m_applicationSettings.EnableDriftCorrection);
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
                tangoConfig.SetBool(TangoConfig.Keys.ENABLE_LOW_LATENCY_IMU_INTEGRATION, true);

                tangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL, m_applicationSettings.MotionTrackingAutoReset);

                // Check if the UUID passed in was actually used.
                if (!usedUUID && !string.IsNullOrEmpty(uuid))
                {
                    Debug.Log("An AreaDescription UUID was passed in, but motion tracking and area descriptions are not "
                            + "both enabled." + Environment.StackTrace);
                }

#if UNITY_EDITOR
                EmulatedAreaDescriptionHelper.InitEmulationForUUID(uuid, m_applicationSettings.EnableAreaDescriptions,
                    m_applicationSettings.AreaDescriptionLearningMode, m_applicationSettings.EnableDriftCorrection,
                    m_applicationSettings.EmulatedAreaDescriptionStartOffset);
#endif
            }

            /// <summary>
            /// Connects to the tango service.
            /// </summary>
            private void _TangoConnect()
            {
                Debug.Log("Tango Startup");
                if (!m_applicationState.IsTangoUpToDate)
                {
                    return;
                }

                lock (m_tangoLifecycleLock)
                {
                    if (!m_applicationState.IsTangoStarted && !m_isApplicationPaused)
                    {
                        m_applicationState.IsTangoStarted = true;

                        AndroidHelper.PerformanceLog("Unity _TangoConnect start");
                        if (API.TangoService_connect(IntPtr.Zero, m_applicationState.TangoConfig.GetHandle()) != Common.ErrorType.TANGO_SUCCESS)
                        {
                            AndroidHelper.ShowAndroidToastMessage("Failed to connect to Tango Service.");
                            Debug.Log(CLASS_NAME + ".Connect() Could not connect to the Tango Service!");
                        }
                        else
                        {
                            AndroidHelper.PerformanceLog("Unity _TangoConnect end");
                            Debug.Log(CLASS_NAME + ".Connect() Tango client connected to service!");
                        }

                        m_fireTangeConnectEvent();
                    }
                }
            }

            /// <summary>
            /// Disconnect from the Tango Service.
            /// </summary>
            /// <param name="isPause">Indicates if service disconnection is caused by a pause event.</param>
            private void _TangoDisconnect(bool isPause = false)
            {
                if (!isPause)
                {
                    Debug.Log("Tango Shutdown");
                }

                lock (m_tangoLifecycleLock)
                {
                    if (!m_applicationState.IsTangoStarted)
                    {
                        AndroidHelper.UnbindTangoService();
                        Debug.Log(CLASS_NAME + ".Disconnect() Not disconnecting from Tango Service "
                                + "as this TangoApplication was not connected");
                        return;
                    }

                    Debug.Log(CLASS_NAME + ".Disconnect() Disconnecting from the Tango Service");
                    m_applicationState.IsTangoStarted = false;

                    // This is necessary because tango_client_api clears camera callbacks when
                    // TangoService_disconnect() is called, unlike other callbacks.
                    VideoOverlayListener.ClearTangoCallbacks();

                    API.TangoService_disconnect();
                    Debug.Log(CLASS_NAME + ".Disconnect() Tango client disconnected from service!");

                    m_fireTangoDisconnectEvent();

                    AndroidHelper.UnbindTangoService();
                    Debug.Log(CLASS_NAME + ".Disconnect() Tango client unbind from service!");
#if UNITY_EDITOR
                    PoseProvider.ResetTangoEmulation();
#endif
                }
            }

            /// <summary>
            /// Set callbacks for all VideoOverlayListener objects.
            /// </summary>
            private void _SetVideoOverlayCallbacks()
            {
                Debug.Log("TangoApplication._SetVideoOverlayCallbacks()");

                if (m_applicationSettings.VideoOverlayUseTextureMethod)
                {
                    VideoOverlayListener.SetCallbackTextureMethod();
                }

                if (m_applicationSettings.VideoOverlayUseYUVTextureIdMethod)
                {
                    VideoOverlayListener.SetCallbackYUVTextureIdMethod(m_yuvTexture);
                }

                if (m_applicationSettings.VideoOverlayUseByteBufferMethod)
                {
                    VideoOverlayListener.SetCallbackByteBufferMethod();
                }
            }
        }
    }

    /// <summary>
    /// Implements permission related inner entities of TangoApplication.
    /// </summary>
    public partial class TangoApplication
    {
        /// <summary>
        /// Permission types used by Tango applications.
        /// </summary>
        [Flags]
        public enum PermissionsTypes
        {
            NONE = 0,
            AREA_LEARNING = 0x1,
            SERVICE_BOUND = 0x2,
            ANDROID_CAMERA = 0x4,
            ANDROID_ACCESS_FINE_LOCATION = 0x8,
        }

        /// <summary>
        /// State of the permission request process.
        /// </summary>
        internal enum PermissionRequestState
        {
            NONE = 0,
            PERMISSION_REQUEST_INIT = 1,
            REQUEST_ANDROID_PERMISSIONS = 2,
            BIND_TO_SERVICE = 3,
            ALL_PERMISSIONS_GRANTED = 4,
            SOME_PERMISSIONS_DENIED = 5,
        }

        /// <summary>
        /// Interface that manages permissions requests and responses for a tango application.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
        internal interface ITangoPermissionsManager
        {
            HashSet<PermissionsTypes> PendingRequiredPermissions { get; }

            PermissionRequestState PermissionRequestState { get; }

            bool IsPermissionsRequestPending { get; }

            void RequestPermissions();

            void OnPermissionResult(PermissionsTypes permissionType, bool isGranted);

            void Reset();
        }

        /// <summary>
        /// Manages permissions requests and responses for a tango application.
        /// </summary>
        internal class TangoPermissionsManager : ITangoPermissionsManager
        {
            private ITangoApplicationSettings m_tangoApplicationSettings;
            private IAndroidHelperWrapper m_androidHelper;
            private Action<bool> m_onAndroidPermissionsResolved;
            private Action<bool> m_onTangoBindResolved;

            /// <summary>
            /// Constructor for TangoPermissionsManager.
            /// </summary>
            /// <param name="tangoApplicationSettings">The application settings.</param>
            /// <param name="androidHelper">The android helper.</param>
            /// <param name="onAndroidPermissionsResolved">Callback method handling resolution of android permissions.</param>
            /// <param name="onTangoBindResolved">Callback method handling resolution of the tango bind.</param>
            public TangoPermissionsManager(ITangoApplicationSettings tangoApplicationSettings,
                IAndroidHelperWrapper androidHelper, Action<bool> onAndroidPermissionsResolved,
                Action<bool> onTangoBindResolved)
            {
                m_tangoApplicationSettings = tangoApplicationSettings;
                m_androidHelper = androidHelper;
                m_onAndroidPermissionsResolved = onAndroidPermissionsResolved;
                m_onTangoBindResolved = onTangoBindResolved;
                PendingRequiredPermissions = new HashSet<PermissionsTypes>();
                PermissionRequestState = PermissionRequestState.NONE;
            }

            /// <summary>
            /// Gets or sets a HashSet of permissions android permissions still required for the application.
            /// </summary>
            public HashSet<PermissionsTypes> PendingRequiredPermissions { get; set; }

            /// <summary>
            /// Gets or sets the current state of android tango permissions request.
            /// </summary>
            public PermissionRequestState PermissionRequestState { get; set; }

            /// <summary>
            /// Gets a value indicating whether a request for android tango permissions is pending.
            /// </summary>
            public bool IsPermissionsRequestPending
            {
                get
                {
                    return PermissionRequestState != PermissionRequestState.NONE
                        && PermissionRequestState != PermissionRequestState.SOME_PERMISSIONS_DENIED
                        && PermissionRequestState != PermissionRequestState.ALL_PERMISSIONS_GRANTED;
                }
            }

            /// <summary>
            /// Requestions android permissions needed by Tango.
            /// </summary>
            public void RequestPermissions()
            {
                PendingRequiredPermissions.Clear();

                if (m_androidHelper.IsRunningOnAndroid())
                {
                    if ((m_tangoApplicationSettings.EnableVideoOverlay || m_tangoApplicationSettings.EnableDepth)
                        && !m_androidHelper.CheckPermission(Common.ANDROID_CAMERA_PERMISSION))
                    {
                        PendingRequiredPermissions.Add(PermissionsTypes.ANDROID_CAMERA);
                    }

                    if (m_tangoApplicationSettings.EnableAreaDescriptions
                        && !m_tangoApplicationSettings.EnableDriftCorrection
                        && !m_androidHelper.CheckPermission(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS))
                    {
                        PendingRequiredPermissions.Add(PermissionsTypes.AREA_LEARNING);
                    }

                    if (m_tangoApplicationSettings.EnableCloudADF
                        && !m_androidHelper.CheckPermission(Common.ANDROID_ACCESS_FINE_LOCATION_PERMISSION))
                    {
                        PendingRequiredPermissions.Add(PermissionsTypes.ANDROID_ACCESS_FINE_LOCATION);
                    }

                    PendingRequiredPermissions.Add(PermissionsTypes.SERVICE_BOUND);
                }

                PermissionRequestState = PermissionRequestState.PERMISSION_REQUEST_INIT;
                _RequestNextPermission();
            }

            /// <summary>
            /// Handles when a permission result arrives.
            /// </summary>
            /// <param name="permissionType">The type of permission.</param>
            /// <param name="isGranted">Whether the permission was granted.</param>
            public void OnPermissionResult(PermissionsTypes permissionType, bool isGranted)
            {
                if (isGranted)
                {
                    PendingRequiredPermissions.Remove(permissionType);
                    _RequestNextPermission();
                }
                else
                {
                   _PermissionWasDenied(permissionType);
                }
            }

            /// <summary>
            /// Resets the state of permissions request.
            /// </summary>
            public void Reset()
            {
                PermissionRequestState = PermissionRequestState.NONE;
                PendingRequiredPermissions.Clear();
            }

            /// <summary>
            /// Requests the next permission in the list of needed permissions and updates state.
            /// </summary>
            private void _RequestNextPermission()
            {
                if (PermissionRequestState == PermissionRequestState.PERMISSION_REQUEST_INIT)
                {
                    PermissionRequestState = PermissionRequestState.REQUEST_ANDROID_PERMISSIONS;
                }

                if (PermissionRequestState == PermissionRequestState.REQUEST_ANDROID_PERMISSIONS)
                {
                    if (PendingRequiredPermissions.Contains(PermissionsTypes.AREA_LEARNING))
                    {
                        m_androidHelper.StartTangoPermissionsActivity(Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS);
                        return;
                    }
                    else if (PendingRequiredPermissions.Contains(PermissionsTypes.ANDROID_CAMERA))
                    {
                        m_androidHelper.RequestPermission(Common.ANDROID_CAMERA_PERMISSION,
                            Common.ANDROID_PERMISSION_REQUEST_CODE);
                        return;
                    }
                    else if (PendingRequiredPermissions.Contains(PermissionsTypes.ANDROID_ACCESS_FINE_LOCATION))
                    {
                        m_androidHelper.RequestPermission(Common.ANDROID_ACCESS_FINE_LOCATION_PERMISSION,
                            Common.ANDROID_PERMISSION_REQUEST_CODE);
                        return;
                    }
                    else
                    {
                        if (m_onAndroidPermissionsResolved != null)
                        {
                            m_onAndroidPermissionsResolved(true);
                        }

                        PermissionRequestState = PermissionRequestState.BIND_TO_SERVICE;
                    }
                }

                if (PermissionRequestState == PermissionRequestState.BIND_TO_SERVICE)
                {
                     if (PendingRequiredPermissions.Contains(PermissionsTypes.SERVICE_BOUND)
                        && !m_androidHelper.BindTangoService())
                    {
                        _PermissionWasDenied(PermissionsTypes.SERVICE_BOUND);
                    }
                    else if (!PendingRequiredPermissions.Contains(PermissionsTypes.SERVICE_BOUND))
                    {
                        _TangoBindResolved();
                    }
                }
            }

            /// <summary>
            /// Handles when a permission is denied.
            /// </summary>
            /// <param name="permissionType">The denied permission type.</param>
            private void _PermissionWasDenied(PermissionsTypes permissionType)
            {
                Debug.Log("_PermissionWasDenied() Permission denied: " + permissionType);
                PermissionRequestState = PermissionRequestState.SOME_PERMISSIONS_DENIED;

                if (PermissionRequestState == PermissionRequestState.REQUEST_ANDROID_PERMISSIONS)
                {
                    m_onAndroidPermissionsResolved(false);
                }

                m_onTangoBindResolved(false);
            }

            /// <summary>
            /// Handles when the tango bind permission is granted.
            /// </summary>
            private void _TangoBindResolved()
            {
                PermissionRequestState = PermissionRequestState.ALL_PERMISSIONS_GRANTED;

                if (m_onTangoBindResolved != null)
                {
                    m_onTangoBindResolved(true);
                }
            }
        }
    }

    /// <summary>
    /// Implements android message related inner entities of TangoApplication.
    /// </summary>
    public partial class TangoApplication
    {
        /// <summary>
        /// Type of Android messages. This mainly mirrors the callback from Android activity.
        /// </summary>
        internal enum AndroidMessageType
        {
            NONE,
            ON_PAUSE,
            ON_RESUME,
            ON_ACTIVITY_RESULT,
            ON_TANGO_SERVICE_CONNECTED,
            ON_TANGO_SERVICE_DISCONNECTED,
            ON_REQUEST_PERMISSION_RESULT,
            ON_DISPLAY_CHANGED
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1600:ElementsMustBeDocumented", Justification = "Interface for testing; methods documented on implementation.")]
        internal interface ITangoAndroidMessageManager
        {
            void RegisterHandlers();

            void UnregisterHandlers();

            void DrainQueue();
        }

        /// <summary>
        /// A message from the android runtime.
        /// </summary>
        internal struct AndroidMessage
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
            /// Constructor for AndroidMessage.
            /// </summary>
            /// <param name="type">The message type.</param>
            /// <param name="messages">Parameter data from the callback functions.</param>
            public AndroidMessage(AndroidMessageType type, params object[] messages)
            {
                m_type = type;
                m_messages = messages;
            }
        }

        /// <summary>
        /// A class that is responsible for registering callbacks for android messages, queueing asynchronous android
        /// messages, and responding to those messages on a synchronized (Unity) thread.
        /// </summary>
        internal class TangoAndroidMessageManager : ITangoAndroidMessageManager
        {
            private object m_messageQueueLock = new object();
            private Queue<AndroidMessage> m_androidMessageQueue = new Queue<AndroidMessage>();
            private ITangoDepthCameraManager m_depthCameraManager;
            private TangoApplicationState m_applicationState;
            private ITangoPermissionsManager m_permissionsManager;
            private Action<bool> m_onAndroidPauseResumeAsync;
            private OnDisplayChangedEventHandler m_onDisplayChanged;
            private IAndroidHelperWrapper m_androidHelper;
            private bool m_isPaused = false;
            private int savedDepthCameraRate;

            /// <summary>
            /// Constructor for TangoAndroidMessageManager.
            /// </summary>
            /// <param name="applicationState">The application state.</param>
            /// <param name="permissionsManager">The permissions manager.</param>
            /// <param name="depthCameraManager">The depth camera manager.</param>
            /// <param name="onAndroidPauseResumeAsync">Callback handling asyncronous pause/resume.</param>
            /// <param name="onDisplayChanged">Callback handling display changed event.</param>
            /// <param name="androidHelper">The android helper.</param>
            public TangoAndroidMessageManager(
                TangoApplicationState applicationState,
                ITangoPermissionsManager permissionsManager,
                ITangoDepthCameraManager depthCameraManager,
                Action<bool> onAndroidPauseResumeAsync,
                OnDisplayChangedEventHandler onDisplayChanged,
                IAndroidHelperWrapper androidHelper)
            {
                m_applicationState = applicationState;
                m_permissionsManager = permissionsManager;
                m_depthCameraManager = depthCameraManager;
                m_onAndroidPauseResumeAsync = onAndroidPauseResumeAsync;
                m_onDisplayChanged = onDisplayChanged;
                m_androidHelper = androidHelper;
            }

            /// <summary>
            /// Register android handlers.
            /// </summary>
            public void RegisterHandlers()
            {
                m_androidHelper.RegisterPauseEvent(_androidOnPause);
                m_androidHelper.RegisterResumeEvent(_androidOnResume);
                m_androidHelper.RegisterOnActivityResultEvent(_androidOnActivityResult);
                m_androidHelper.RegisterOnDisplayChangedEvent(_androidOnDisplayChanged);
                m_androidHelper.RegisterOnTangoServiceConnected(_androidOnTangoServiceConnected);
                m_androidHelper.RegisterOnTangoServiceDisconnected(_androidOnTangoServiceDisconnected);
                m_androidHelper.RegisterOnRequestPermissionsResultEvent(_androidOnRequestPermissionsResult);
            }

            /// <summary>
            /// Unregister android handlers.
            /// </summary>
            public void UnregisterHandlers()
            {
                m_androidHelper.UnregisterPauseEvent(_androidOnPause);
                m_androidHelper.UnregisterResumeEvent(_androidOnResume);
                m_androidHelper.UnregisterOnActivityResultEvent(_androidOnActivityResult);
                m_androidHelper.UnregisterOnDisplayChangedEvent(_androidOnDisplayChanged);
            }

            /// <summary>
            /// Process all queued android messages.
            /// </summary>
            public void DrainQueue()
            {
                while (m_androidMessageQueue.Count != 0)
                {
                    AndroidMessage msg;
                    lock (m_messageQueueLock)
                    {
                        msg = m_androidMessageQueue.Dequeue();
                    }

                    _HandleMessage(msg);
                }
            }

            /// <summary>
            /// Process a message from the android runtime.
            /// </summary>
            /// <param name="message">The message to process.</param>
            private void _HandleMessage(AndroidMessage message)
            {
                switch (message.m_type)
                {
                case AndroidMessageType.ON_PAUSE:
                    if (m_applicationState.IsTangoStarted && !m_permissionsManager.IsPermissionsRequestPending)
                    {
                        m_isPaused = true;
                        m_permissionsManager.Reset();
                        savedDepthCameraRate = m_depthCameraManager.LastSetDepthCameraRate;
                    }

                    break;
                case AndroidMessageType.ON_RESUME:
                    if (m_isPaused)
                    {
                        m_isPaused  = false;
                        m_depthCameraManager.SetDepthCameraRate(savedDepthCameraRate);
                    }

                    break;
                case AndroidMessageType.ON_ACTIVITY_RESULT:
                    int requestCode = (int)message.m_messages[0];
                    int resultCode = (int)message.m_messages[1];
                    if (requestCode == Common.TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE)
                    {
                        m_permissionsManager.OnPermissionResult(PermissionsTypes.AREA_LEARNING,
                                          resultCode == (int)Common.AndroidResult.SUCCESS);
                    }

                    break;
                case AndroidMessageType.ON_REQUEST_PERMISSION_RESULT:
                    requestCode = (int)message.m_messages[0];
                    string[] permissions = (string[])message.m_messages[1];
                    AndroidPermissionGrantResult[] grantResults = (AndroidPermissionGrantResult[])message.m_messages[2];

                    if (requestCode != Common.ANDROID_PERMISSION_REQUEST_CODE)
                    {
                        break;
                    }

                    for (int i = 0; i < permissions.Length; i++)
                    {
                        string permission = permissions[i];
                        if (permission == Common.ANDROID_CAMERA_PERMISSION)
                        {
                            m_permissionsManager.OnPermissionResult(PermissionsTypes.ANDROID_CAMERA,
                                grantResults[i] == AndroidPermissionGrantResult.GRANTED);
                        }
                        else if (permission == Common.ANDROID_ACCESS_FINE_LOCATION_PERMISSION)
                        {
                            m_permissionsManager.OnPermissionResult(PermissionsTypes.ANDROID_ACCESS_FINE_LOCATION,
                                grantResults[i] == AndroidPermissionGrantResult.GRANTED);
                        }
                    }

                    break;
                case AndroidMessageType.ON_TANGO_SERVICE_CONNECTED:
                    AndroidJavaObject binder = (AndroidJavaObject)message.m_messages[0];

                    // By keeping this logic in C#, the client app can respond if this call fails.
                    int result = m_androidHelper.TangoSetBinder(binder);

                    // Tango Support initialize has to be called after service binder is set because
                    // it invokes TangoService functions under the hood.
                    TangoSupport.Initialize();
                    _updateDisplayRotation();

                    m_permissionsManager.OnPermissionResult(PermissionsTypes.SERVICE_BOUND,
                        result == Common.ErrorType.TANGO_SUCCESS);
                    break;
                case AndroidMessageType.ON_DISPLAY_CHANGED:
                    _updateDisplayRotation();
                    break;
                default:
                    break;
                }
            }

            /// <summary>
            /// Update display rotation parameters based on current display orientation.
            /// </summary>
            private void _updateDisplayRotation()
            {
                OrientationManager.Rotation displayRotation = m_androidHelper.GetDisplayRotation();
                OrientationManager.Rotation colorCameraRotation = m_androidHelper.GetColorCameraRotation();
                TangoSupport.UpdatePoseMatrixFromDeviceRotation(displayRotation, colorCameraRotation);
                m_onDisplayChanged(displayRotation, colorCameraRotation);
            }

            /// <summary>
            /// Handles android pause event.
            /// </summary>
            private void _androidOnPause()
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_PAUSE));
                m_onAndroidPauseResumeAsync(true);
            }

            /// <summary>
            /// Handles android resume event.
            /// </summary>
            private void _androidOnResume()
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_RESUME));
                m_onAndroidPauseResumeAsync(false);
            }

            /// <summary>
            /// Handles android activity result event.
            /// </summary>
            /// <param name="requestCode">The request code.</param>
            /// <param name="resultCode">Thre reslt code.</param>
            /// <param name="data">The data.</param>
            private void _androidOnActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_ACTIVITY_RESULT,
                    requestCode, resultCode, data));
            }

            /// <summary>
            /// Handles android permission request result available event.
            /// </summary>
            /// <param name="requestCode">The code of the request.</param>
            /// <param name="permissions">The array of permissions.</param>
            /// <param name="grantResults">The array of permission grant results.</param>
            private void _androidOnRequestPermissionsResult(
                int requestCode, string[] permissions, AndroidPermissionGrantResult[] grantResults)
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_REQUEST_PERMISSION_RESULT,
                    requestCode, permissions, grantResults));
            }

            /// <summary>
            /// Handles android tango service connected changed event.
            /// </summary>
            /// <param name="binder">The android binder object.</param>
            private void _androidOnTangoServiceConnected(AndroidJavaObject binder)
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_TANGO_SERVICE_CONNECTED, binder));
                Debug.Log(CLASS_NAME + "._androidOnTangoServiceConnected() Android OnTangoServiceConnected() called "
                    + "from Android main thread.");
            }

            /// <summary>
            /// Handles android tango service disconnected changed event.
            /// </summary>
            private void _androidOnTangoServiceDisconnected()
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_TANGO_SERVICE_DISCONNECTED));
                Debug.Log(CLASS_NAME + "._androidOnTangoServiceDisconnected() Android OnServiceDisconnected() called "
                    + "from Android main thread.");
            }

            /// <summary>
            /// Handles android display changed event.
            /// </summary>
            private void _androidOnDisplayChanged()
            {
                _EnqueueMessage(new AndroidMessage(AndroidMessageType.ON_DISPLAY_CHANGED));
            }

            /// <summary>
            /// Appends a received android message to the pending message queue.
            /// </summary>
            /// <param name="message">The android message to enqueue.</param>
            private void _EnqueueMessage(AndroidMessage message)
            {
                lock (m_messageQueueLock)
                {
                    m_androidMessageQueue.Enqueue(message);
                }
            }
        }
    }
}
