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
        public bool m_autoConnectToService = false;

        public bool m_allowOutOfDateTangoAPI = false;
        public GameObject m_testEnvironment;

        public bool m_enableMotionTracking = true;
        public bool m_motionTrackingAutoReset = true;

        [FormerlySerializedAs("m_enableADFLoading")]
        public bool m_enableAreaDescriptions = false;
        [FormerlySerializedAs("m_enableAreaLearning")]
        public bool m_areaDescriptionLearningMode = false;

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

        public bool m_enableVideoOverlay = false;
        [FormerlySerializedAs("m_useExperimentalVideoOverlay")]
        public bool m_videoOverlayUseTextureIdMethod = true;
        public bool m_videoOverlayUseByteBufferMethod = false;

        internal bool m_enableCloudADF = false;

        private const string CLASS_NAME = "TangoApplication";
        private static string m_tangoServiceVersion = string.Empty;

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
        private bool m_screenOrientationChanged = false;
        private PoseListener m_poseListener;
        private DepthListener m_depthListener;
        private VideoOverlayListener m_videoOverlayListener;
        private TangoEventListener m_tangoEventListener;
        private TangoCloudEventListener m_tangoCloudEventListener;
        private AreaDescriptionEventListener m_areaDescriptionEventListener;
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
        public void Register(object tangoObject)
        {
            ITangoAreaDescriptionEvent areaDescriptionEvent = tangoObject as ITangoAreaDescriptionEvent;
            if (areaDescriptionEvent != null)
            {
                _RegisterOnAreaDescriptionEvent(areaDescriptionEvent.OnAreaDescriptionImported,
                                                areaDescriptionEvent.OnAreaDescriptionExported);
            }

            ITangoEvent tangoEvent = tangoObject as ITangoEvent;
            if (tangoEvent != null)
            {
                _RegisterOnTangoEvent(tangoEvent.OnTangoEventAvailableEventHandler);
            }

            ITangoEventMultithreaded tangoEventMultithreaded = tangoObject as ITangoEventMultithreaded;

            if (tangoEventMultithreaded != null)
            {
                _RegisterOnTangoEventMultithreaded(tangoEventMultithreaded.OnTangoEventMultithreadedAvailableEventHandler);
            }

            ITangoLifecycle tangoLifecycle = tangoObject as ITangoLifecycle;
            if (tangoLifecycle != null)
            {
                _RegisterPermissionsCallback(tangoLifecycle.OnTangoPermissions);
                _RegisterOnTangoConnect(tangoLifecycle.OnTangoServiceConnected);
                _RegisterOnTangoDisconnect(tangoLifecycle.OnTangoServiceDisconnected);
            }

            ITangoCloudEvent tangoCloudEvent = tangoObject as ITangoCloudEvent;
            if (tangoCloudEvent != null)
            {
                _RegisterOnTangoCloudEvent(tangoCloudEvent.OnTangoCloudEventAvailableEventHandler);
            }

            if (m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;
                if (poseHandler != null)
                {
                    _RegisterOnTangoPoseEvent(poseHandler.OnTangoPoseAvailable);
                }
            }

            if (m_enableDepth)
            {
                ITangoDepth depthHandler = tangoObject as ITangoDepth;
                if (depthHandler != null)
                {
                    _RegisterOnTangoDepthEvent(depthHandler.OnTangoDepthAvailable);
                }

                ITangoDepthMultithreaded depthMultithreadedHandler = tangoObject as ITangoDepthMultithreaded;
                if (depthMultithreadedHandler != null && m_depthListener != null)
                {
                    m_depthListener.RegisterOnTangoDepthMultithreadedAvailable(depthMultithreadedHandler.OnTangoDepthMultithreadedAvailable);
                }
            }
            
            if (m_enableVideoOverlay)
            {
                if (m_videoOverlayUseTextureIdMethod)
                {
                    IExperimentalTangoVideoOverlay videoOverlayHandler = tangoObject as IExperimentalTangoVideoOverlay;
                    if (videoOverlayHandler != null)
                    {
                        _RegisterOnExperimentalTangoVideoOverlay(videoOverlayHandler.OnExperimentalTangoImageAvailable);
                    }
                }

                if (m_videoOverlayUseByteBufferMethod)
                {
                    ITangoVideoOverlay videoOverlayHandler = tangoObject as ITangoVideoOverlay;
                    if (videoOverlayHandler != null)
                    {
                        _RegisterOnTangoVideoOverlay(videoOverlayHandler.OnTangoImageAvailableEventHandler);
                    }

                    ITangoVideoOverlayMultithreaded videoOverlayMultithreadedHandler = tangoObject as ITangoVideoOverlayMultithreaded;
                    if (videoOverlayMultithreadedHandler != null && m_videoOverlayListener != null)
                    {
                        m_videoOverlayListener.RegisterOnTangoImageMultithreadedAvailable(videoOverlayMultithreadedHandler.OnTangoImageMultithreadedAvailable);
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
                _UnregisterOnAreaDescriptionEvent(areaDescriptionEvent.OnAreaDescriptionImported,
                                                  areaDescriptionEvent.OnAreaDescriptionExported);
            }

            ITangoEvent tangoEvent = tangoObject as ITangoEvent;
            if (tangoEvent != null)
            {
                _UnregisterOnTangoEvent(tangoEvent.OnTangoEventAvailableEventHandler);
            }

            ITangoEventMultithreaded tangoEventMultithreaded = tangoObject as ITangoEventMultithreaded;
            if (tangoEventMultithreaded != null)
            {
                _UnregisterOnTangoEventMultithreaded(tangoEventMultithreaded.OnTangoEventMultithreadedAvailableEventHandler);
            }

            ITangoLifecycle tangoLifecycle = tangoObject as ITangoLifecycle;
            if (tangoLifecycle != null)
            {
                _UnregisterPermissionsCallback(tangoLifecycle.OnTangoPermissions);
                _UnregisterOnTangoConnect(tangoLifecycle.OnTangoServiceConnected);
                _UnregisterOnTangoDisconnect(tangoLifecycle.OnTangoServiceDisconnected);
            }

            ITangoCloudEvent tangoCloudEvent = tangoObject as ITangoCloudEvent;
            if (tangoCloudEvent != null)
            {
                _UnregisterOnTangoCloudEvent(tangoCloudEvent.OnTangoCloudEventAvailableEventHandler);
            }

            if (m_enableMotionTracking)
            {
                ITangoPose poseHandler = tangoObject as ITangoPose;
                if (poseHandler != null)
                {
                    _UnregisterOnTangoPoseEvent(poseHandler.OnTangoPoseAvailable);
                }
            }

            if (m_enableDepth)
            {
                ITangoDepth depthHandler = tangoObject as ITangoDepth;
                if (depthHandler != null)
                {
                    _UnregisterOnTangoDepthEvent(depthHandler.OnTangoDepthAvailable);
                }

                ITangoDepthMultithreaded depthMultithreadedHandler = tangoObject as ITangoDepthMultithreaded;
                if (depthMultithreadedHandler != null && m_depthListener != null)
                {
                    m_depthListener.UnregisterOnTangoDepthMultithreadedAvailable(depthMultithreadedHandler.OnTangoDepthMultithreadedAvailable);
                }
            }

            if (m_enableVideoOverlay)
            {
                if (m_videoOverlayUseTextureIdMethod)
                {
                    IExperimentalTangoVideoOverlay videoOverlayHandler = tangoObject as IExperimentalTangoVideoOverlay;
                    if (videoOverlayHandler != null)
                    {
                        _UnregisterOnExperimentalTangoVideoOverlay(videoOverlayHandler.OnExperimentalTangoImageAvailable);
                    }
                }

                if (m_videoOverlayUseByteBufferMethod)
                {
                    ITangoVideoOverlay videoOverlayHandler = tangoObject as ITangoVideoOverlay;
                    if (videoOverlayHandler != null)
                    {
                        _UnregisterOnTangoVideoOverlay(videoOverlayHandler.OnTangoImageAvailableEventHandler);
                    }

                    ITangoVideoOverlayMultithreaded videoOverlayMulitthreadedHandler = tangoObject as ITangoVideoOverlayMultithreaded;
                    if (videoOverlayHandler != null && m_videoOverlayListener != null)
                    {
                        m_videoOverlayListener.UnregisterOnTangoImageMultithreadedAvailable(videoOverlayMulitthreadedHandler.OnTangoImageMultithreadedAvailable);
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
        /// Manual initalization step 2: Call this to connect to the Tango service.
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

            _CheckTangoVersion();

            // Setup configs.
            m_tangoConfig = new TangoConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_DEFAULT);
            m_tangoRuntimeConfig = new TangoConfig(TangoEnums.TangoConfigType.TANGO_CONFIG_RUNTIME);

            if (m_enableVideoOverlay && m_videoOverlayUseTextureIdMethod)
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

            if (m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_DEPTH_PERCEPTION_BOOL, m_enableDepth) && m_enableDepth)
            {
                _SetDepthCallbacks();
            }

            if (m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_COLOR_CAMERA_BOOL, m_enableVideoOverlay) && 
                m_enableVideoOverlay)
            {
                _SetVideoOverlayCallbacks();
            }

            _SetEventCallbacks();

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
        /// Set the framerate of the depth camera.
        /// 
        /// Disabling or reducing the framerate of the depth camera when it is running can save a significant amount
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
        /// Set the framerate of the depth camera.
        /// 
        /// Disabling or reducing the framerate of the depth camera when it is running can save a significant amount
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
                SetDepthCameraRate(9000);
                break;
            }
        }

        /// <summary>
        /// Clear the 3D reconstruction data.  The reconstruction will start fresh.
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
        /// Extract a single mesh for the entire 3D reconstruction state.
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
        /// Returns Status.SUCCESS if the voxels are fully extracted and stared in the array.  In this case, numVoxels
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
        /// Enable or disable the 3D reconstruction.
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public void Set3DReconstructionEnabled(bool enabled)
        {
            m_tango3DReconstruction.SetEnabled(enabled);
        }

        /// <summary>
        /// Propagates an event from the java plugin connected to the Cloud Service through UnitySendMessage().
        /// </summary>
        /// <param name="message">A string representation of the cloud event key and value.</param>
        internal void SendCloudEvent(string message)
        {
            Debug.Log("New message from Cloud Service: " + message);
            string[] keyValue = message.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            int key;
            int value;
            if (m_tangoCloudEventListener != null &&
                    keyValue.Length == 2 &&
                    Int32.TryParse(keyValue[0], out key) &&
                    Int32.TryParse(keyValue[1], out value))
            {
                m_tangoCloudEventListener.OnCloudEventAvailable(key, value);
            }
        }

        /// <summary>
        /// Register to get Tango pose callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Callback handler.</param>
        private void _RegisterOnTangoPoseEvent(OnTangoPoseAvailableEventHandler handler)
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
        /// <param name="handler">Event handler to remove.</param>
        private void _UnregisterOnTangoPoseEvent(OnTangoPoseAvailableEventHandler handler)
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
        /// <param name="handler">Event handler.</param>
        private void _RegisterOnTangoDepthEvent(OnTangoDepthAvailableEventHandler handler)
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
        /// <param name="handler">Event handler to remove.</param>
        private void _UnregisterOnTangoDepthEvent(OnTangoDepthAvailableEventHandler handler)
        {
            if (m_depthListener != null)
            {
                m_depthListener.UnregisterOnTangoDepthAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango cloud event callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="handler">Event handler.</param>
        private void _RegisterOnTangoCloudEvent(OnTangoCloudEventAvailableEventHandler handler)
        {
            if (m_tangoCloudEventListener != null)
            {
                m_tangoCloudEventListener.RegisterOnTangoCloudEventAvailable(handler);
            }
        }

        /// <summary>
        /// Unregister from the Tango cloud event callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event handler to remove.</param>
        private void _UnregisterOnTangoCloudEvent(OnTangoCloudEventAvailableEventHandler handler)
        {
            if (m_tangoCloudEventListener != null)
            {
                m_tangoCloudEventListener.UnregisterOnTangoCloudEventAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="handler">Event handler.</param>
        private void _RegisterOnTangoEvent(OnTangoEventAvailableEventHandler handler)
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
        /// <param name="handler">Event handler to remove.</param>
        private void _UnregisterOnTangoEvent(OnTangoEventAvailableEventHandler handler)
        {
            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.UnregisterOnTangoEventAvailable(handler);
            }
        }
        
        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="handler">Event handler.</param>
        private void _RegisterOnTangoEventMultithreaded(OnTangoEventAvailableEventHandler handler)
        {
            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.RegisterOnTangoEventMultithreadedAvailable(handler);
            }
        }
        
        /// <summary>
        /// Unregister from the Tango event callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="handler">Event to remove.</param>
        private void _UnregisterOnTangoEventMultithreaded(OnTangoEventAvailableEventHandler handler)
        {
            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.UnregisterOnTangoEventMultithreadedAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango video overlay callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="handler">Event handler.</param>
        private void _RegisterOnTangoVideoOverlay(OnTangoImageAvailableEventHandler handler)
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
        /// <param name="handler">Event handler to remove.</param>
        private void _UnregisterOnTangoVideoOverlay(OnTangoImageAvailableEventHandler handler)
        {
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.UnregisterOnTangoImageAvailable(handler);
            }
        }

        /// <summary>
        /// Experimental API only, subject to change.  Register to get Tango video overlay callbacks.
        /// </summary>
        /// <param name="handler">Event handler.</param>
        private void _RegisterOnExperimentalTangoVideoOverlay(OnExperimentalTangoImageAvailableEventHandler handler)
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
        /// <param name="handler">Event handler to remove.</param>
        private void _UnregisterOnExperimentalTangoVideoOverlay(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.UnregisterOnExperimentalTangoImageAvailable(handler);
            }
        }

        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// See TangoApplication.Register for details.
        /// </summary>
        /// <param name="import">The handler to the import callback function.</param>
        /// <param name="export">The handler to the export callback function.</param>
        private void _RegisterOnAreaDescriptionEvent(OnAreaDescriptionImportEventHandler import,
                                                     OnAreaDescriptionExportEventHandler export)
        {
            if (m_areaDescriptionEventListener != null)
            {
                m_areaDescriptionEventListener.Register(import, export);
            }
        }

        /// <summary>
        /// Unregister from the Tango event callbacks.
        /// 
        /// See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="import">The handler to the import callback function.</param>
        /// <param name="export">The handler to the export callback function.</param>
        private void _UnregisterOnAreaDescriptionEvent(OnAreaDescriptionImportEventHandler import,
                                                       OnAreaDescriptionExportEventHandler export)
        {
            if (m_areaDescriptionEventListener != null)
            {
                m_areaDescriptionEventListener.Unregister(import, export);
            }
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
        /// Set callbacks on all PoseListener objects.
        /// </summary>
        /// <param name="framePairs">Frame pairs.</param>
        private void _SetMotionTrackingCallbacks(TangoCoordinateFramePair[] framePairs)
        {
            Debug.Log("TangoApplication._SetMotionTrackingCallbacks()");

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
            Debug.Log("TangoApplication._SetDepthCallbacks()");

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
            Debug.Log("TangoApplication._SetEventCallbacks()");

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
            Debug.Log("TangoApplication._SetVideoOverlayCallbacks()");

            if (m_videoOverlayListener != null)
            {
                if (m_videoOverlayUseTextureIdMethod)
                {
                    m_videoOverlayListener.SetCallbackTextureIdMethod(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR,
                                                                      m_yuvTexture);
                }

                if (m_videoOverlayUseByteBufferMethod)
                {
                    m_videoOverlayListener.SetCallbackByteBufferMethod(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);
                }
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
                    if (m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_AREA_LEARNING_BOOL, m_areaDescriptionLearningMode) && m_areaDescriptionLearningMode)
                    {
                        Debug.Log("Area Learning is enabled.");
                    }

                    if (!string.IsNullOrEmpty(uuid))
                    {
                        if (m_tangoConfig.SetString(TangoConfig.Keys.LOAD_AREA_DESCRIPTION_UUID_STRING, uuid))
                        {
                            usedUUID = true;
                        }
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
            m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_LOW_LATENCY_IMU_INTEGRATION, true);

            m_tangoConfig.SetBool(TangoConfig.Keys.ENABLE_MOTION_TRACKING_AUTO_RECOVERY_BOOL, m_motionTrackingAutoReset);

            if (m_enableCloudADF)
            {
                Debug.Log("Connect to Cloud Service.");
                AndroidHelper.BindTangoCloudService();
            }

            // Check if the UUID passed in was actually used.
            if (!usedUUID && !string.IsNullOrEmpty(uuid))
            {
                Debug.Log("An AreaDescription UUID was passed in, but motion tracking and area descriptions are not "
                          + "both enabled." + Environment.StackTrace);
            }
        }

        /// <summary>
        /// Validate the TangoService version is supported.
        /// </summary>
        private void _CheckTangoVersion()
        {
            if (!AndroidHelper.IsTangoCoreUpToDate())
            {
                Debug.Log(string.Format(CLASS_NAME + ".Initialize() Invalid API version. Please update Project Tango Core to at least {0}.", AndroidHelper.TANGO_MINIMUM_VERSION_CODE));
                if (!m_allowOutOfDateTangoAPI)
                {
                    AndroidHelper.ShowAndroidToastMessage("Please update Tango Core");
                    return;
                }
            }

            m_isServiceInitialized = true;
            Debug.Log(CLASS_NAME + ".Initialize() Tango was initialized!");
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
                if (TangoServiceAPI.TangoService_connect(m_callbackContext, m_tangoConfig.GetHandle()) != Common.ErrorType.TANGO_SUCCESS)
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

            if (m_enableCloudADF)
            {
                Debug.Log("Disconnect from Cloud Service.");
                AndroidHelper.UnbindTangoCloudService();
            }
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
        /// Delegate for the Android screen orientation changed.
        /// </summary>
        /// <param name="newOrientation">The index of new orientation.</param>
        private void _androidOnScreenOrientationChanged(AndroidScreenRotation newOrientation)
        {
            m_screenOrientationChanged = true;
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
            AndroidHelper.RegisterOnScreenOrientationChangedEvent(_androidOnScreenOrientationChanged);
            AndroidHelper.RegisterOnTangoServiceConnected(_androidOnTangoServiceConnected);
            AndroidHelper.RegisterOnTangoServiceDisconnected(_androidOnTangoServiceDisconnected);

            // Setup listeners.
            m_tangoEventListener = new TangoEventListener();
            m_areaDescriptionEventListener = new AreaDescriptionEventListener();

            if (m_enableCloudADF)
            {
                m_tangoCloudEventListener = new TangoCloudEventListener();
            }

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

            if (m_enable3DReconstruction)
            {
                m_tango3DReconstruction = new Tango3DReconstruction(m_3drResolutionMeters, m_3drGenerateColor, m_3drSpaceClearing);
                m_tango3DReconstruction.m_useAreaDescriptionPose = m_3drUseAreaDescriptionPose;
                m_tango3DReconstruction.m_sendColorToUpdate = m_3drGenerateColor;
            }

            TangoSupport.UpdateCurrentRotationIndex();

#if UNITY_EDITOR
            EmulatedEnvironmentRenderHelper.InitForEnvironment(m_testEnvironment);
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
                m_requiredPermissions |= m_enableAreaDescriptions ? PermissionsTypes.AREA_LEARNING : PermissionsTypes.NONE;
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
            PoseProvider.UpdateTangoEmulation();
            if (m_enableDepth && m_testEnvironment != null)
            {
                DepthProvider.UpdateTangoEmulation();
            }
#endif

            if (m_poseListener != null)
            {
                m_poseListener.SendPoseIfAvailable(m_enableAreaDescriptions);
            }

            if (m_tangoEventListener != null)
            {
                m_tangoEventListener.SendIfTangoEventAvailable();
            }

            if (m_tangoCloudEventListener != null)
            {
                m_tangoCloudEventListener.SendIfTangoCloudEventAvailable();
            }

            if (m_depthListener != null)
            {
                m_depthListener.SendDepthIfAvailable();
            }

            if (m_videoOverlayListener != null)
            {
                m_videoOverlayListener.SendIfVideoOverlayAvailable();
            }

            if (m_areaDescriptionEventListener != null)
            {
                m_areaDescriptionEventListener.SendEventIfAvailable();
            }

            if (m_tango3DReconstruction != null)
            {
                m_tango3DReconstruction.SendEventIfAvailable();
            }

            if (m_screenOrientationChanged)
            {
                TangoSupport.UpdateCurrentRotationIndex();
                m_screenOrientationChanged = false;
            }
        }

        /// <summary>
        /// Unity callback when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
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
