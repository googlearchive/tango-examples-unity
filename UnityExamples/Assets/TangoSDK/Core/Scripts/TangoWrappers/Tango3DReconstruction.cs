//-----------------------------------------------------------------------
// <copyright file="Tango3DReconstruction.cs" company="Google">
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
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango 3D Reconstruction GridIndicesDirty events.
    /// </summary>
    /// <param name="gridIndexList">List of GridIndex objects that are dirty and should be updated.</param>
    internal delegate void OnTango3DReconstructionGridIndiciesDirtyEventHandler(List<Tango3DReconstruction.GridIndex> gridIndexList);

    /// <summary>
    /// Manages a single instance of the Tango 3D Reconstruction library, updating a single 3D model based on depth
    /// and color information.
    /// </summary>
    public class Tango3DReconstruction
        : IDisposable, ITangoLifecycle, ITangoPointCloudMultithreaded, ITangoVideoOverlay
    {
        /// <summary>
        /// If set 3D Reconstruction will happen in the area description's reference frame.
        /// </summary>
        internal bool m_useAreaDescriptionPose;

        /// <summary>
        /// If set, 3D Reconstruction will pass color information into the reconstruction.
        /// </summary>
        internal bool m_sendColorToUpdate;

        /// <summary>
        /// The handle for the Tango 3D Reconstruction library.
        /// </summary>
        private IntPtr m_context;

        /// <summary>
        /// Grid indices that have been updated since the last call to SendEventIfAvailable.
        /// </summary>
        private List<GridIndex> m_updatedIndices = new List<GridIndex>();

        /// <summary>
        /// Synchronization object.
        /// </summary>
        private object m_lockObject = new object();

        /// <summary>
        /// Called when the 3D Reconstruction is dirty.
        /// </summary>
        private OnTango3DReconstructionGridIndiciesDirtyEventHandler m_onGridIndicesDirty;

        /// <summary>
        /// Constant matrix for the transformation from the device frame to the depth camera frame.
        /// </summary>
        private Matrix4x4 m_device_T_depthCamera;

        /// <summary>
        /// Constant matrix for the transformation from the device frame to the color camera frame.
        /// </summary>
        private Matrix4x4 m_device_T_colorCamera;

        /// <summary>
        /// Cache of the most recent depth received, to send with color information.
        /// </summary>
        private APIPointCloud m_mostRecentDepth;

        /// <summary>
        /// Cache of the most recent depth's points.
        ///
        /// This is a separate array so the code can use Marshal.Copy.
        /// </summary>
        private float[] m_mostRecentDepthPoints = new float[Common.MAX_NUM_POINTS * 4];

        /// <summary>
        /// Cache of the most recent depth's pose, to send with color information.
        /// </summary>
        private APIPose m_mostRecentDepthPose;

        /// <summary>
        /// If set, <c>m_mostRecentDepth</c> and <c>m_mostRecentDepthPose</c> are set and should be sent to
        /// reconstruction once combined with other data.
        /// </summary>
        private bool m_mostRecentDepthIsValid;

        /// <summary>
        /// If true, 3D Reconstruction will be updated with depth.  Otherwise, it will not.
        /// </summary>
        private bool m_enabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango3DReconstruction"/> class.
        /// </summary>
        /// <param name="resolution">Size in meters of each grid cell.</param>
        /// <param name="generateColor">If true the reconstruction will contain color information.</param>
        /// <param name="spaceClearing">If true the reconstruction will clear empty space it detects.</param>
        /// <param name="minNumVertices">
        /// If non-zero, any submesh with less than this number of vertices will get removed, assumed to be noise.
        /// </param>
        /// <param name="updateMethod">Method used to update voxels.</param>
        internal Tango3DReconstruction(float resolution, bool generateColor, bool spaceClearing, int minNumVertices,
                                       UpdateMethod updateMethod)
        {
            IntPtr config = API.Tango3DR_Config_create((int)APIConfigType.Context);
            API.Tango3DR_Config_setDouble(config, "resolution", resolution);
            API.Tango3DR_Config_setBool(config, "generate_color", generateColor);
            API.Tango3DR_Config_setBool(config, "use_space_clearing", spaceClearing);
            API.Tango3DR_Config_setInt32(config, "min_num_vertices", minNumVertices);
            API.Tango3DR_Config_setInt32(config, "update_method", (int)updateMethod);

            // The 3D Reconstruction library can not handle a left handed transformation during update.  Instead,
            // transform into the Unity world space via the external_T_tango config.
            APIMatrix3x3 unityWorld_T_startService = new APIMatrix3x3();
            unityWorld_T_startService.SetRow(0, new Vector3(1, 0, 0));
            unityWorld_T_startService.SetRow(1, new Vector3(0, 0, 1));
            unityWorld_T_startService.SetRow(2, new Vector3(0, 1, 0));
            API.Tango3DR_Config_setMatrix3x3(config, "external_T_tango", ref unityWorld_T_startService);
            API.Tango3DR_Config_setBool(config, "use_clockwise_winding_order", true);

            m_context = API.Tango3DR_create(config);
            API.Tango3DR_Config_destroy(config);
        }

        /// <summary>
        /// Corresponds to a Tango3DR_Status.
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// An error occured.
            /// </summary>
            ERROR = -3,

            /// <summary>
            /// Extraction was only partially successful. The data filled out
            /// is valid, but there is more data available.
            /// </summary>
            INSUFFICIENT_SPACE = -2,

            /// <summary>
            /// Invalid parameters were passed in.
            /// </summary>
            INVALID = -1,

            /// <summary>
            /// The operation was successful.
            /// </summary>
            SUCCESS = 0
        }

        /// <summary>
        /// Corresponds to a Tango3DR_UpdateMethod.
        /// </summary>
        public enum UpdateMethod
        {
            /// <summary>
            /// Associates voxels with depth readings by traversing (raycasting) forward from the camera to the
            /// observed depth. Results in slightly higher reconstruction quality. Can be significantly slower,
            /// especially on updates with a high number of depth points.
            /// </summary>
            TRAVERSAL = 0,

            /// <summary>
            /// Associates voxels with depth readings by projecting voxels into a depth  image plane using a
            /// projection matrix. Requires that the depth camera calibration has been set using the
            /// Tango3DR_setDepthCalibration method. Results in slightly lower reconstruction quality. Under this mode,
            /// the speed of updates is independent of the number of depth points.
            /// </summary>
            PROJECTIVE = 1,
        }

        /// @cond
        /// <summary>
        /// Corresponds to a Tango3DR_ConfigType.
        /// </summary>
        private enum APIConfigType
        {
            /// <summary>
            /// Needed by Tango3DR_create.
            /// </summary>
            Context = 0,

            /// <summary>
            /// Needed by Tango3DR_textureMeshFromDataset.
            /// </summary>
            Texturing = 1
        }

        /// <summary>
        /// Releases all resource used by the <see cref="Tango3DReconstruction"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Tango3DReconstruction"/>.
        /// The <see cref="Dispose"/> method leaves the <see cref="Tango3DReconstruction"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="Tango3DReconstruction"/>
        /// so the garbage collector can reclaim the memory that the <see cref="Tango3DReconstruction"/> was occupying.
        /// </remarks>
        public void Dispose()
        {
            if (m_context != IntPtr.Zero)
            {
                API.Tango3DR_destroy(m_context);
            }

            m_context = IntPtr.Zero;
        }

        /// <summary>
        /// This is called when the permission granting process is finished.
        /// </summary>
        /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
        /// <c>false</c>
        public void OnTangoPermissions(bool permissionsGranted)
        {
            // Nothing to do.
        }

        /// <summary>
        /// This is called when successfully connected to the Tango service.
        /// </summary>
        public void OnTangoServiceConnected()
        {
            // Calculate the camera extrinsics.
            TangoCoordinateFramePair pair;

            TangoPoseData imu_T_devicePose = new TangoPoseData();
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            PoseProvider.GetPoseAtTime(imu_T_devicePose, 0, pair);

            TangoPoseData imu_T_depthCameraPose = new TangoPoseData();
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH;
            PoseProvider.GetPoseAtTime(imu_T_depthCameraPose, 0, pair);

            TangoPoseData imu_T_colorCameraPose = new TangoPoseData();
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
            PoseProvider.GetPoseAtTime(imu_T_colorCameraPose, 0, pair);

            // Convert into matrix form to combine the poses.
            Matrix4x4 device_T_imu = Matrix4x4.Inverse(imu_T_devicePose.ToMatrix4x4());
            m_device_T_depthCamera = device_T_imu * imu_T_depthCameraPose.ToMatrix4x4();
            m_device_T_colorCamera = device_T_imu * imu_T_colorCameraPose.ToMatrix4x4();

            // Update the camera intrinsics.
            TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
            Status status;

            APICameraCalibration colorCameraCalibration;
            VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);
            colorCameraCalibration.calibration_type = (int)intrinsics.calibration_type;
            colorCameraCalibration.width = intrinsics.width;
            colorCameraCalibration.height = intrinsics.height;
            colorCameraCalibration.cx = intrinsics.cx;
            colorCameraCalibration.cy = intrinsics.cy;
            colorCameraCalibration.fx = intrinsics.fx;
            colorCameraCalibration.fy = intrinsics.fy;
            colorCameraCalibration.distortion0 = intrinsics.distortion0;
            colorCameraCalibration.distortion1 = intrinsics.distortion1;
            colorCameraCalibration.distortion2 = intrinsics.distortion2;
            colorCameraCalibration.distortion3 = intrinsics.distortion3;
            colorCameraCalibration.distortion4 = intrinsics.distortion4;
            status = (Status)API.Tango3DR_setColorCalibration(m_context, ref colorCameraCalibration);
            if (status != Status.SUCCESS)
            {
                Debug.Log("Unable to set color calibration." + Environment.StackTrace);
            }

            APICameraCalibration depthCameraCalibration;
            VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_DEPTH, intrinsics);
            depthCameraCalibration.calibration_type = (int)intrinsics.calibration_type;
            depthCameraCalibration.width = intrinsics.width;
            depthCameraCalibration.height = intrinsics.height;
            depthCameraCalibration.cx = intrinsics.cx;
            depthCameraCalibration.cy = intrinsics.cy;
            depthCameraCalibration.fx = intrinsics.fx;
            depthCameraCalibration.fy = intrinsics.fy;
            depthCameraCalibration.distortion0 = intrinsics.distortion0;
            depthCameraCalibration.distortion1 = intrinsics.distortion1;
            depthCameraCalibration.distortion2 = intrinsics.distortion2;
            depthCameraCalibration.distortion3 = intrinsics.distortion3;
            depthCameraCalibration.distortion4 = intrinsics.distortion4;
            status = (Status)API.Tango3DR_setDepthCalibration(m_context, ref depthCameraCalibration);
            if (status != Status.SUCCESS)
            {
                Debug.Log("Unable to set depth calibration." + Environment.StackTrace);
            }
        }

        /// <summary>
        /// This is called when disconnected from the Tango service.
        /// </summary>
        public void OnTangoServiceDisconnected()
        {
            // Nothing to do.
        }

        /// <summary>
        /// This is called each time new depth data is available.
        ///
        /// On the Tango tablet, the depth callback occurs at 5 Hz.
        /// </summary>
        /// <param name="pointCloud">Tango depth.</param>
        public void OnTangoPointCloudMultithreadedAvailable(ref TangoPointCloudIntPtr pointCloud)
        {
#if UNITY_EDITOR
            if (m_enabled) 
            {
                Debug.LogError("Mesh Reconstruction is not currently supported in the Unity Editor.");
                m_enabled = false;
            }
#endif

            if (!m_enabled || pointCloud.m_points == IntPtr.Zero)
            {
                return;
            }

            // Build World T depth camera
            TangoPoseData world_T_devicePose = new TangoPoseData();
            if (m_useAreaDescriptionPose)
            {
                TangoCoordinateFramePair pair;
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime(world_T_devicePose, pointCloud.m_timestamp, pair);
            }
            else
            {
                TangoCoordinateFramePair pair;
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime(world_T_devicePose, pointCloud.m_timestamp, pair);
            }

            if (world_T_devicePose.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                Debug.LogFormat("Time {0} has bad status code {1}{2}",
                                pointCloud.m_timestamp, world_T_devicePose.status_code, Environment.StackTrace);
                return;
            }

            // The 3D Reconstruction library can not handle a left handed transformation during update.  Instead,
            // transform into the Unity world space via the external_T_tango config.
            Matrix4x4 world_T_depthCamera = world_T_devicePose.ToMatrix4x4() * m_device_T_depthCamera;

            _UpdateDepth(pointCloud, world_T_depthCamera);
        }

        /// <summary>
        /// This will be called when a new frame is available from the camera.
        ///
        /// The first scan-line of the color image is reserved for metadata instead of image pixels.
        /// </summary>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="imageBuffer">Image buffer.</param>
        public void OnTangoImageAvailableEventHandler(Tango.TangoEnums.TangoCameraId cameraId,
                                                      Tango.TangoUnityImageData imageBuffer)
        {
#if UNITY_EDITOR
            if (m_enabled) 
            {
                Debug.LogError("Mesh Reconstruction is not currently supported in the Unity Editor.");
                m_enabled = false;
            }
#endif

            if (!m_enabled)
            {
                return;
            }

            if (cameraId != TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR)
            {
                return;
            }

            // Build World T depth camera
            TangoPoseData world_T_devicePose = new TangoPoseData();
            if (m_useAreaDescriptionPose)
            {
                TangoCoordinateFramePair pair;
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime(world_T_devicePose, imageBuffer.timestamp, pair);
            }
            else
            {
                TangoCoordinateFramePair pair;
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime(world_T_devicePose, imageBuffer.timestamp, pair);
            }

            if (world_T_devicePose.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                Debug.Log(string.Format("Time {0} has bad status code {1}",
                                        imageBuffer.timestamp, world_T_devicePose.status_code)
                          + Environment.StackTrace);
                return;
            }

            // The 3D Reconstruction library can not handle a left handed transformation during update.  Instead,
            // transform into the Unity world space via the external_T_tango config.
            Matrix4x4 world_T_colorCamera = world_T_devicePose.ToMatrix4x4() * m_device_T_colorCamera;

            _UpdateColor(imageBuffer, world_T_colorCamera);
        }

        /// @endcond
        /// <summary>
        /// Set if the 3DReconstruction is enabled or not.  If disabled, the 3D Reconstruction will not get updated.
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        internal void SetEnabled(bool enabled)
        {
            m_enabled = enabled;
        }

        /// <summary>
        /// Register a Unity main thread handler for the GridIndicesDirty event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterGridIndicesDirty(OnTango3DReconstructionGridIndiciesDirtyEventHandler handler)
        {
            if (handler != null)
            {
                m_onGridIndicesDirty += handler;
            }
        }

        /// <summary>
        /// Unregister a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterGridIndicesDirty(OnTango3DReconstructionGridIndiciesDirtyEventHandler handler)
        {
            if (handler != null)
            {
                m_onGridIndicesDirty -= handler;
            }
        }

        /// <summary>
        /// Raise ITango3DReconstruction events if there is new data.
        /// </summary>
        internal void SendEventIfAvailable()
        {
            lock (m_lockObject)
            {
                if (m_updatedIndices.Count != 0)
                {
                    if (m_onGridIndicesDirty != null)
                    {
                        m_onGridIndicesDirty(m_updatedIndices);
                    }

                    m_updatedIndices.Clear();
                }
            }
        }

        /// <summary>
        /// Extract a mesh for a single grid index, into a suitable format for Unity Mesh.
        /// </summary>
        /// <returns>
        /// Returns Status.SUCCESS if the mesh is fully extracted and stored in the arrays.  In this case, <c>numVertices</c>
        /// and <c>numTriangles</c> will say how many vertices and triangles are used, the rest of the array is untouched.
        ///
        /// Returns Status.INSUFFICIENT_SPACE if the mesh is partially extracted and stored in the arrays.  <c>numVertices</c>
        /// and <c>numTriangles</c> have the same meaning as if Status.SUCCESS is returned, but in this case the array should
        /// grow.
        ///
        /// Returns Status.ERROR or Status.INVALID if some other error occurs.
        /// </returns>
        /// <param name="gridIndex">Grid index to extract.</param>
        /// <param name="vertices">On successful extraction this will get filled out with the vertex positions.</param>
        /// <param name="normals">On successful extraction this will get filled out with vertex normals.</param>
        /// <param name="colors">On successful extraction this will get filled out with vertex colors.</param>
        /// <param name="triangles">On successful extraction this will get filled out with vertex indexes.</param>
        /// <param name="numVertices">Number of vertexes filled out.</param>
        /// <param name="numTriangles">Number of triangles filled out.</param>
        internal Status ExtractMeshSegment(
            GridIndex gridIndex, Vector3[] vertices, Vector3[] normals, Color32[] colors, int[] triangles,
            out int numVertices, out int numTriangles)
        {
            APIMeshGCHandles pinnedHandles = APIMeshGCHandles.Alloc(vertices, normals, colors, triangles);
            APIMesh mesh = APIMesh.FromArrays(vertices, normals, colors, triangles);

            int result = API.Tango3DR_extractPreallocatedMeshSegment(m_context, ref gridIndex, ref mesh);
            numVertices = (int)mesh.numVertices;
            numTriangles = (int)mesh.numFaces;
            pinnedHandles.Free();

            return (Status)result;
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
        internal Status ExtractWholeMesh(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles)
        {
            IntPtr apiMeshIntPtr;
            int result = API.Tango3DR_extractFullMesh(m_context, out apiMeshIntPtr);

            if (((Status)result) == Status.SUCCESS)
            {
                // Marshal mesh structure into managed code.
                APIMesh mesh = (APIMesh)Marshal.PtrToStructure(apiMeshIntPtr, typeof(APIMesh));

                // Marshal structure arrays into managed code.
                MarshallingHelper.MarshalUnmanagedStructArrayToList<Vector3>(mesh.vertices, mesh.numVertices, vertices);
                MarshallingHelper.MarshalUnmanagedStructArrayToList<Vector3>(mesh.normals, mesh.numVertices, normals);
                MarshallingHelper.MarshalUnmanagedStructArrayToList<Color32>(mesh.colors, mesh.numVertices, colors);

                // Marshal face array to managed code and add to list.
                int[] faces = new int[mesh.numFaces * 3];
                Marshal.Copy(mesh.faces, faces, 0, mesh.numFaces * 3);
                triangles.AddRange(faces);

                // Free unmanaged mesh memory.
                API.Tango3DR_Mesh_destroy(apiMeshIntPtr);
            }

            return (Status)result;
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
        internal Status ExtractSignedDistanceVoxel(
            GridIndex gridIndex, SignedDistanceVoxel[] voxels, out int numVoxels)
        {
            numVoxels = 0;

            int result = API.Tango3DR_extractPreallocatedVoxelGridSegment(m_context, ref gridIndex, voxels.Length, voxels);
            if (result == (int)Status.SUCCESS)
            {
                // This is the current default number of voxels per grid volume.
                numVoxels = 16 * 16 * 16;
            }

            return (Status)result;
        }

        /// <summary>
        /// Clear the current mesh in the 3D Reconstruction.
        /// </summary>
        internal void Clear()
        {
            int result = API.Tango3DR_clear(m_context);
            if ((Status)result != Status.SUCCESS)
            {
                Debug.Log("Tango3DR_clear returned non-success." + Environment.StackTrace);
            }
        }

        /// <summary>
        /// Update the 3D Reconstruction with a new point cloud and pose.
        ///
        /// It is expected this will get called in from the Tango binder thread.
        /// </summary>
        /// <param name="pointCloud">Point cloud from Tango.</param>
        /// <param name="depthPose">Pose matrix the point cloud corresponds too.</param>
        private void _UpdateDepth(TangoPointCloudIntPtr pointCloud, Matrix4x4 depthPose)
        {
            if (m_context == IntPtr.Zero)
            {
                Debug.Log("Update called before creating a reconstruction context." + Environment.StackTrace);
                return;
            }

            APIPointCloud apiCloud;
            apiCloud.numPoints = pointCloud.m_numPoints;
            apiCloud.points = pointCloud.m_points;
            apiCloud.timestamp = pointCloud.m_timestamp;

            APIPose apiDepthPose = APIPose.FromMatrix4x4(ref depthPose);

            if (!m_sendColorToUpdate)
            {
                // No need to wait for a color image, update reconstruction immediately.
                IntPtr rawUpdatedIndices;
                Status result = (Status)API.Tango3DR_update(
                    m_context, ref apiCloud, ref apiDepthPose, IntPtr.Zero, IntPtr.Zero, out rawUpdatedIndices);
                if (result != Status.SUCCESS)
                {
                    Debug.Log("Tango3DR_update returned non-success." + Environment.StackTrace);
                    return;
                }

                _AddUpdatedIndices(rawUpdatedIndices);
                API.Tango3DR_GridIndexArray_destroy(rawUpdatedIndices);
            }
            else
            {
                lock (m_lockObject)
                {
                    // We need both a color image and a depth cloud to update reconstruction.  Cache the depth cloud
                    // because there are much less depth points than pixels.
                    m_mostRecentDepth = apiCloud;
                    m_mostRecentDepth.points = IntPtr.Zero;
                    Marshal.Copy(pointCloud.m_points, m_mostRecentDepthPoints, 0, pointCloud.m_numPoints * 4);

                    m_mostRecentDepthPose = apiDepthPose;
                    m_mostRecentDepthIsValid = true;
                }
            }
        }

        /// <summary>
        /// Update the 3D Reconstruction with a new image and pose.
        ///
        /// It is expected this will get called in from the Tango binder thread.
        /// </summary>
        /// <param name="image">Color image from Tango.</param>
        /// <param name="imagePose">Pose matrix the color image corresponds too.</param>
        private void _UpdateColor(Tango.TangoUnityImageData image, Matrix4x4 imagePose)
        {
            if (!m_sendColorToUpdate)
            {
                // There is no depth cloud to process.
                return;
            }

            if (m_context == IntPtr.Zero)
            {
                Debug.Log("Update called before creating a reconstruction context." + Environment.StackTrace);
                return;
            }

            lock (m_lockObject)
            {
                if (!m_mostRecentDepthIsValid)
                {
                    return;
                }

                APIImageBuffer apiImage;
                apiImage.width = image.width;
                apiImage.height = image.height;
                apiImage.stride = image.stride;
                apiImage.timestamp = image.timestamp;
                apiImage.format = (int)image.format;

                GCHandle pinnedImageDataHandle = GCHandle.Alloc(image.data, GCHandleType.Pinned);
                apiImage.data = pinnedImageDataHandle.AddrOfPinnedObject();
                pinnedImageDataHandle.Free();

                APIPose apiImagePose = APIPose.FromMatrix4x4(ref imagePose);

                // Update the depth points to have the right value
                GCHandle mostRecentDepthPointsHandle = GCHandle.Alloc(m_mostRecentDepthPoints, GCHandleType.Pinned);
                m_mostRecentDepth.points = Marshal.UnsafeAddrOfPinnedArrayElement(m_mostRecentDepthPoints, 0);

                IntPtr rawUpdatedIndices;
                Status result = (Status)API.Tango3DR_update(
                    m_context, ref m_mostRecentDepth, ref m_mostRecentDepthPose,
                    ref apiImage, ref apiImagePose, out rawUpdatedIndices);

                m_mostRecentDepthIsValid = false;
                mostRecentDepthPointsHandle.Free();

                if (result != Status.SUCCESS)
                {
                    Debug.Log("Tango3DR_update returned non-success." + Environment.StackTrace);
                    return;
                }

                _AddUpdatedIndices(rawUpdatedIndices);
                API.Tango3DR_GridIndexArray_destroy(rawUpdatedIndices);
            }
        }

        /// <summary>
        /// Add to the list of updated GridIndex objects that gets sent to the main thread.
        /// </summary>
        /// <param name="rawUpdatedIndices"><c>IntPtr</c> to a list of updated indices.</param>
        private void _AddUpdatedIndices(IntPtr rawUpdatedIndices)
        {
            int numUpdatedIndices = Marshal.ReadInt32(rawUpdatedIndices, 0);
            IntPtr rawIndices = Marshal.ReadIntPtr(rawUpdatedIndices, 4);
            lock (m_lockObject)
            {
                if (m_updatedIndices.Count == 0)
                {
                    // Update in fast mode, no duplicates are possible.
                    for (int it = 0; it < numUpdatedIndices; ++it)
                    {
                        GridIndex index;
                        index.x = Marshal.ReadInt32(rawIndices, (0 + (it * 3)) * 4);
                        index.y = Marshal.ReadInt32(rawIndices, (1 + (it * 3)) * 4);
                        index.z = Marshal.ReadInt32(rawIndices, (2 + (it * 3)) * 4);
                        m_updatedIndices.Add(index);
                    }
                }
                else
                {
                    // Duplicates are possible, need to check while adding.
                    for (int it = 0; it < numUpdatedIndices; ++it)
                    {
                        GridIndex index;
                        index.x = Marshal.ReadInt32(rawIndices, (0 + (it * 3)) * 4);
                        index.y = Marshal.ReadInt32(rawIndices, (1 + (it * 3)) * 4);
                        index.z = Marshal.ReadInt32(rawIndices, (2 + (it * 3)) * 4);
                        if (!m_updatedIndices.Contains(index))
                        {
                            m_updatedIndices.Add(index);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indexes into the 3D Reconstruction mesh's grid cells.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct GridIndex
        {
            /// <summary>
            /// Index in 3D reconstrction's X direction.
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public Int32 x;

            /// <summary>
            /// Index in 3D reconstruction's Y direction.
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public Int32 y;

            /// <summary>
            /// Index in 3D reconstruction's Z direction.
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public Int32 z;
        }

        /// <summary>
        /// Corresponds to a Tango3DR_SignedDistanceVoxel.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SignedDistanceVoxel
        {
            /// <summary>
            /// The signed distance function's value, in normalized units.
            ///
            /// The signed distance function is stored as a truncated signed distance function.  The floating point
            /// value is represented as a signed integer where <c>Int16.MinValue</c> represents the most negative value
            /// possible and <c>Int16.MaxValue</c> represents the most positive value possible.
            /// </summary>
            [MarshalAs(UnmanagedType.I2)]
            public Int16 sdf;

            /// <summary>
            /// Observation weight.
            ///
            /// The greater this value, the more certain the system is about the value in <c>sdf</c>.
            /// </summary>
            [MarshalAs(UnmanagedType.U2)]
            public UInt16 weight;
        }

        /// <summary>
        /// Corresponds to a Tango3DR_CameraCalibration.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APICameraCalibration
        {
            [MarshalAs(UnmanagedType.U4)]
            public int calibration_type;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 width;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 height;

            [MarshalAs(UnmanagedType.R8)]
            public double fx;

            [MarshalAs(UnmanagedType.R8)]
            public double fy;

            [MarshalAs(UnmanagedType.R8)]
            public double cx;

            [MarshalAs(UnmanagedType.R8)]
            public double cy;

            [MarshalAs(UnmanagedType.R8)]
            public double distortion0;

            [MarshalAs(UnmanagedType.R8)]
            public double distortion1;

            [MarshalAs(UnmanagedType.R8)]
            public double distortion2;

            [MarshalAs(UnmanagedType.R8)]
            public double distortion3;

            [MarshalAs(UnmanagedType.R8)]
            public double distortion4;
        }

        /// <summary>
        /// Corresponds to a Tango3DR_ImageBuffer.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIImageBuffer
        {
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 width;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 height;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 stride;

            [MarshalAs(UnmanagedType.R8)]
            public double timestamp;

            [MarshalAs(UnmanagedType.I4)]
            public int format;

            public IntPtr data;
        }

        /// <summary>
        /// Corresponds to a Tango3DR_Matrix3x3.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIMatrix3x3
        {
            [MarshalAs(UnmanagedType.R8)]
            public double r00;

            [MarshalAs(UnmanagedType.R8)]
            public double r10;

            [MarshalAs(UnmanagedType.R8)]
            public double r20;

            [MarshalAs(UnmanagedType.R8)]
            public double r01;

            [MarshalAs(UnmanagedType.R8)]
            public double r11;

            [MarshalAs(UnmanagedType.R8)]
            public double r21;

            [MarshalAs(UnmanagedType.R8)]
            public double r02;

            [MarshalAs(UnmanagedType.R8)]
            public double r12;

            [MarshalAs(UnmanagedType.R8)]
            public double r22;

            /// <summary>
            /// Set a row in the matrix.
            /// </summary>
            /// <param name="row">Row index to set, 0 to 2.</param>
            /// <param name="value">Value to set.</param>
            public void SetRow(int row, Vector3 value)
            {
                switch (row)
                {
                case 0:
                    r00 = value.x;
                    r01 = value.y;
                    r02 = value.z;
                    break;

                case 1:
                    r10 = value.x;
                    r11 = value.y;
                    r12 = value.z;
                    break;

                case 2:
                    r20 = value.x;
                    r21 = value.y;
                    r22 = value.z;
                    break;

                default:
                    Debug.Log("Unsupported row index.");
                    break;
                }
            }
        }

        /// <summary>
        /// Corresponds to a Tango3DR_Mesh.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIMesh
        {
            [MarshalAs(UnmanagedType.R8)]
            public double timestamp;

            [MarshalAs(UnmanagedType.U4)]
            public Int32 numVertices;

            [MarshalAs(UnmanagedType.U4)]
            public Int32 numFaces;

            [MarshalAs(UnmanagedType.U4)]
            public Int32 numTextures;

            [MarshalAs(UnmanagedType.U4)]
            public Int32 maxNumVertices;

            [MarshalAs(UnmanagedType.U4)]
            public Int32 maxNumFaces;

            [MarshalAs(UnmanagedType.U4)]
            public Int32 maxNumTextures;

            public IntPtr vertices;
            public IntPtr faces;
            public IntPtr normals;
            public IntPtr colors;
            public IntPtr textureCoords;
            public IntPtr textures;

            /// <summary>
            /// Build an APIMesh from the constituent arrays, pinning the arrays.  Make sure you've pinned the arrays
            /// before calling this method.
            /// </summary>
            /// <returns>APIMesh ready to pass to C API.</returns>
            /// <param name="vertices">Vertex array.</param>
            /// <param name="normals">Normal array.</param>
            /// <param name="colors">Color array.</param>
            /// <param name="triangles">Index array.</param>
            public static APIMesh FromArrays(Vector3[] vertices, Vector3[] normals, Color32[] colors, int[] triangles)
            {
                APIMesh mesh;
                mesh.timestamp = 0;
                mesh.numVertices = 0;
                mesh.numFaces = 0;
                mesh.numTextures = 0;
                mesh.maxNumVertices = vertices.Length;
                mesh.maxNumFaces = triangles.Length / 3;
                mesh.maxNumTextures = 0;

                mesh.vertices = _AddrOfOptionalArray(vertices);
                mesh.faces = _AddrOfOptionalArray(triangles);
                mesh.normals = _AddrOfOptionalArray(normals);
                mesh.colors = _AddrOfOptionalArray(colors);
                mesh.textureCoords = IntPtr.Zero;
                mesh.textures = IntPtr.Zero;

                return mesh;
            }

            /// <summary>
            /// Get the address for the start of an Array, or <c>IntPtr.Zero</c> if the array is null.
            /// </summary>
            /// <returns><c>IntPtr</c> representing the address of the start of the array.</returns>
            /// <param name="array">Array to get the address of.</param>
            private static IntPtr _AddrOfOptionalArray(Array array)
            {
                if (array == null)
                {
                    return IntPtr.Zero;
                }
                else
                {
                    return Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
                }
            }
        }

        /// <summary>
        /// Contains all the GCHandles for pinning an APIMesh.
        /// </summary>
        private struct APIMeshGCHandles
        {
            /// <summary>
            /// GCHandle for the vertices field.
            /// </summary>
            private GCHandle m_verticesGCHandle;

            /// <summary>
            /// GCHandle for the faces field.
            /// </summary>
            private GCHandle m_facesGCHandle;

            /// <summary>
            /// GCHandle for the normals field.
            /// </summary>
            private GCHandle m_normalsGCHandle;

            /// <summary>
            /// GCHandle for the colors field.
            /// </summary>
            private GCHandle m_colorsGCHandle;

            /// <summary>
            /// Pin all the arrays passed in.
            /// </summary>
            /// <returns>A single object that you can Free to unpin all.</returns>
            /// <param name="vertices">Vertex array.</param>
            /// <param name="normals">Normal array.</param>
            /// <param name="colors">Color array.</param>
            /// <param name="triangles">Index array.</param>
            public static APIMeshGCHandles Alloc(
                Vector3[] vertices, Vector3[] normals, Color32[] colors, int[] triangles)
            {
                APIMeshGCHandles handles;
                handles.m_verticesGCHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                handles.m_facesGCHandle = GCHandle.Alloc(triangles, GCHandleType.Pinned);
                handles.m_normalsGCHandle = GCHandle.Alloc(normals, GCHandleType.Pinned);
                handles.m_colorsGCHandle = GCHandle.Alloc(colors, GCHandleType.Pinned);

                return handles;
            }

            /// <summary>
            /// Call GCHandle.Free() on all the internal handles.
            /// </summary>
            public void Free()
            {
                m_verticesGCHandle.Free();
                m_facesGCHandle.Free();
                m_normalsGCHandle.Free();
                m_colorsGCHandle.Free();
            }
        }

        /// <summary>
        /// Corresponds to a Tango3DR_PointCloud.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIPointCloud
        {
            [MarshalAs(UnmanagedType.R8)]
            public double timestamp;

            [MarshalAs(UnmanagedType.I4)]
            public Int32 numPoints;

            public IntPtr points;
        }

        /// <summary>
        /// Corresponds to a Tango3DR_Pose.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIPose
        {
            [MarshalAs(UnmanagedType.R8)]
            public double translation0;

            [MarshalAs(UnmanagedType.R8)]
            public double translation1;

            [MarshalAs(UnmanagedType.R8)]
            public double translation2;

            [MarshalAs(UnmanagedType.R8)]
            public double orientation0;

            [MarshalAs(UnmanagedType.R8)]
            public double orientation1;

            [MarshalAs(UnmanagedType.R8)]
            public double orientation2;

            [MarshalAs(UnmanagedType.R8)]
            public double orientation3;

            /// <summary>
            /// Initializes the <see cref="Tango.Tango3DReconstruction+APIPose"/> struct.
            /// </summary>
            /// <param name="matrix">Right handed matrix to represent.</param>
            /// <returns>APIPose for the matrix.</returns>
            public static APIPose FromMatrix4x4(ref Matrix4x4 matrix)
            {
                Vector3 position = matrix.GetColumn(3);
                Quaternion orientation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));

                APIPose pose;
                pose.translation0 = position.x;
                pose.translation1 = position.y;
                pose.translation2 = position.z;
                pose.orientation0 = orientation.x;
                pose.orientation1 = orientation.y;
                pose.orientation2 = orientation.z;
                pose.orientation3 = orientation.w;
                return pose;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private static class API
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            private const string TANGO_3DR_DLL = "tango_3d_reconstruction";

            [DllImport(TANGO_3DR_DLL)]
            public static extern IntPtr Tango3DR_create(IntPtr config);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_destroy(IntPtr context);

            [DllImport(TANGO_3DR_DLL)]
            public static extern IntPtr Tango3DR_Config_create(Int32 config);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_destroy(IntPtr config);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Mesh_destroy(IntPtr mesh);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_setBool(IntPtr config, string key, bool value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_setInt32(IntPtr config, string key, Int32 value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_setDouble(IntPtr config, string key, double value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_setMatrix3x3(IntPtr config, string key, ref APIMatrix3x3 value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_getBool(IntPtr config, string key, out bool value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_getInt32(IntPtr config, string key, out Int32 value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_getDouble(IntPtr config, string key, out double value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_Config_getMatrix3x3(IntPtr config, string key, out APIMatrix3x3 value);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_setColorCalibration(IntPtr contxet, ref APICameraCalibration calibration);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_setDepthCalibration(IntPtr contxet, ref APICameraCalibration calibration);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_GridIndexArray_destroy(IntPtr gridIndexArray);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_clear(IntPtr context);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_update(IntPtr context, ref APIPointCloud cloud, ref APIPose cloud_pose,
                                                     ref APIImageBuffer image, ref APIPose image_pose,
                                                     out IntPtr updated_indices);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_update(IntPtr context, ref APIPointCloud cloud, ref APIPose cloud_pose,
                                                     IntPtr image, IntPtr image_pose, out IntPtr updated_indices);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_extractPreallocatedMeshSegment(
                IntPtr context, ref GridIndex gridIndex, ref APIMesh mesh);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_extractPreallocatedFullMesh(IntPtr context, ref APIMesh mesh);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_extractFullMesh(IntPtr context, out IntPtr mesh);

            [DllImport(TANGO_3DR_DLL)]
            public static extern int Tango3DR_extractPreallocatedVoxelGridSegment(
                IntPtr context, ref GridIndex gridIndex, Int32 maxNumVoxels, SignedDistanceVoxel[] voxels);
#else
            public static IntPtr Tango3DR_create(IntPtr config)
            {
                return IntPtr.Zero;
            }

            public static int Tango3DR_destroy(IntPtr context)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Mesh_destroy(IntPtr mesh)
            {
                return (int)Status.SUCCESS;
            }

            public static IntPtr Tango3DR_Config_create(Int32 config)
            {
                return IntPtr.Zero;
            }

            public static int Tango3DR_Config_destroy(IntPtr config)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_setBool(IntPtr config, string key, bool value)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_setInt32(IntPtr config, string key, Int32 value)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_setDouble(IntPtr config, string key, double value)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_setMatrix3x3(IntPtr config, string key, ref APIMatrix3x3 value)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_getBool(IntPtr config, string key, out bool value)
            {
                value = false;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_getInt32(IntPtr config, string key, out Int32 value)
            {
                value = 0;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_getDouble(IntPtr config, string key, out double value)
            {
                value = 0;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_Config_getMatrix3x3(IntPtr config, string key, out APIMatrix3x3 value)
            {
                value.r00 = value.r11 = value.r22 = 1;
                value.r10 = value.r20 = value.r01 = value.r21 = value.r02 = value.r12 = 0;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_setColorCalibration(IntPtr contxet, ref APICameraCalibration calibration)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_setDepthCalibration(IntPtr contxet, ref APICameraCalibration calibration)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_GridIndexArray_destroy(IntPtr gridIndexArray)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_clear(IntPtr context)
            {
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_update(IntPtr context, ref APIPointCloud cloud, ref APIPose cloud_pose,
                                              ref APIImageBuffer image, ref APIPose image_pose,
                                              out IntPtr updated_indices)
            {
                updated_indices = IntPtr.Zero;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_update(IntPtr context, ref APIPointCloud cloud, ref APIPose cloud_pose,
                                              IntPtr image, IntPtr image_pose,
                                              out IntPtr updated_indices)
            {
                updated_indices = IntPtr.Zero;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_extractPreallocatedMeshSegment(
                IntPtr context, ref GridIndex gridIndex, ref APIMesh mesh)
            {
                mesh.numVertices = 0;
                mesh.numFaces = 0;
                mesh.numTextures = 0;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_extractPreallocatedFullMesh(IntPtr context, ref APIMesh mesh)
            {
                mesh.numVertices = 0;
                mesh.numFaces = 0;
                mesh.numTextures = 0;
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_extractFullMesh(IntPtr context, out IntPtr mesh)
            {
                mesh = new IntPtr();
                return (int)Status.SUCCESS;
            }

            public static int Tango3DR_extractPreallocatedVoxelGridSegment(
                IntPtr context, ref GridIndex gridIndex, Int32 maxNumVoxels, SignedDistanceVoxel[] voxels)
            {
                return (int)Status.SUCCESS;
            }
#endif
        }
    }
}
