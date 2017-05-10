//-----------------------------------------------------------------------
// <copyright file="TangoSupport.cs" company="Google">
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
    /// Contains the Project Tango Support Unity API. The Project Tango Support
    /// Unity API provides helper methods useful to external developers for
    /// manipulating Project Tango data. The Project Tango Support Unity API is
    /// experimental and subject to change.
    /// </summary>
    public static class TangoSupport
    {
#region MEMBER_FIELDS
        /// <summary>
        /// Matrix that transforms from Start of Service to the Unity World.
        /// </summary>
        public static readonly Matrix4x4 UNITY_WORLD_T_START_SERVICE = new Matrix4x4
        {
            m00 = 1.0f, m01 = 0.0f, m02 = 0.0f, m03 = 0.0f,
            m10 = 0.0f, m11 = 0.0f, m12 = 1.0f, m13 = 0.0f,
            m20 = 0.0f, m21 = 1.0f, m22 = 0.0f, m23 = 0.0f,
            m30 = 0.0f, m31 = 0.0f, m32 = 0.0f, m33 = 1.0f
        };

        /// <summary>
        /// Matrix that transforms from the Unity Camera to Device.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*",
                                                         Justification = "Matrix visibility is more important.")]
        public static readonly Matrix4x4 DEVICE_T_UNITY_CAMERA = new Matrix4x4
        {
            m00 = 1.0f, m01 = 0.0f, m02 =  0.0f, m03 = 0.0f,
            m10 = 0.0f, m11 = 1.0f, m12 =  0.0f, m13 = 0.0f,
            m20 = 0.0f, m21 = 0.0f, m22 = -1.0f, m23 = 0.0f,
            m30 = 0.0f, m31 = 0.0f, m32 =  0.0f, m33 = 1.0f
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*",
                                                         Justification = "Matrix visibility is more important.")]
        public static readonly Matrix4x4 COLOR_CAMERA_T_UNITY_CAMERA = new Matrix4x4
        {
            m00 = 1.0f, m01 =  0.0f, m02 = 0.0f, m03 = 0.0f,
            m10 = 0.0f, m11 = -1.0f, m12 = 0.0f, m13 = 0.0f,
            m20 = 0.0f, m21 =  0.0f, m22 = 1.0f, m23 = 0.0f,
            m30 = 0.0f, m31 =  0.0f, m32 = 0.0f, m33 = 1.0f
        };

        /// <summary>
        /// The rotation matrix need to be applied when using color camera's pose.
        /// </summary>
        public static Matrix4x4 m_colorCameraPoseRotation = Matrix4x4.identity;

        /// <summary>
        /// The rotation matrix need to be applied when using device pose.
        /// </summary>
        public static Matrix4x4 m_devicePoseRotation = Matrix4x4.identity;

        /// <summary>
        /// Name of the Tango Support C API shared library.
        /// </summary>
        internal const string TANGO_SUPPORT_UNITY_DLL = "tango_support_api";

        /// <summary>
        /// Class name for debug logging.
        /// </summary>
        private const string CLASS_NAME = "TangoSupport";

        /// <summary>
        /// Matrix that transforms for device screen rotation of 270 degrees.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*",
                                                         Justification = "Matrix visibility is more important.")]
        private static readonly Matrix4x4 ROTATION270_T_DEFAULT = new Matrix4x4
        {
            m00 = 0.0f, m01 = -1.0f, m02 = 0.0f, m03 = 0.0f,
            m10 = 1.0f, m11 =  0.0f, m12 = 0.0f, m13 = 0.0f,
            m20 = 0.0f, m21 =  0.0f, m22 = 1.0f, m23 = 0.0f,
            m30 = 0.0f, m31 =  0.0f, m32 = 0.0f, m33 = 1.0f
        };

        /// <summary>
        /// Matrix that transforms for device screen rotation of 180 degrees.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*",
                                                         Justification = "Matrix visibility is more important.")]
        private static readonly Matrix4x4 ROTATION180_T_DEFAULT = new Matrix4x4
        {
            m00 = -1.0f, m01 =  0.0f, m02 = 0.0f, m03 = 0.0f,
            m10 =  0.0f, m11 = -1.0f, m12 = 0.0f, m13 = 0.0f,
            m20 =  0.0f, m21 =  0.0f, m22 = 1.0f, m23 = 0.0f,
            m30 =  0.0f, m31 =  0.0f, m32 = 0.0f, m33 = 1.0f
        };

        /// <summary>
        /// Matrix that transforms for device screen rotation of 90 degrees.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*",
                                                         Justification = "Matrix visibility is more important.")]
        private static readonly Matrix4x4 ROTATION90_T_DEFAULT = new Matrix4x4
        {
            m00 =  0.0f, m01 = 1.0f, m02 = 0.0f, m03 = 0.0f,
            m10 = -1.0f, m11 = 0.0f, m12 = 0.0f, m13 = 0.0f,
            m20 =  0.0f, m21 = 0.0f, m22 = 1.0f, m23 = 0.0f,
            m30 =  0.0f, m31 = 0.0f, m32 = 0.0f, m33 = 1.0f
        };

        /// <summary>
        /// A type to define all combinations of markers supported.
        /// </summary>
        public enum MarkerType
        {
            ARTAG = 0x01,
            QRCODE = 0x02,
        }
#endregion

#region INITIALIZATION_FUNCTIONS
        /// <summary>
        /// Initialize the support library with function pointers required by
        /// the library. Either this version or @c TangoSupport_initializeLibrary
        /// should be called during application initialization, but not both. This
        /// version requires each of the initialization parameters and should only be
        /// used if specialized parameters are necessary.
        /// NOTE: This function must be called after the Android service has been
        /// bound.
        /// </summary>
        public static void Initialize()
        {
            TangoSupportAPI.TangoUnity_initializeTangoSupportLibrary();
        }
#endregion

#region TRANSFORMATION_FUNCTIONS
        /// <summary>
        /// Compute a rotation from rotation a to rotation b, in form of the OrientationManager.Rotation enum.
        /// </summary>
        /// <param name="b">Target rotation frame.</param>
        /// <param name="a">Start rotation frame.</param>
        /// <returns>The orientation index that follows Android screen rotation standard.</returns>
        public static Tango.OrientationManager.Rotation RotateFromAToB(Tango.OrientationManager.Rotation b,
                                                                       Tango.OrientationManager.Rotation a)
        {
            int ret = (int)b - (int)a;
            if (ret < 0)
            {
                ret += 4;
            }

            return (Tango.OrientationManager.Rotation)(ret % 4);
        }

        /// <summary>
        /// Convert a TangoPoseData into the Unity coordinate system. This only
        /// works on TangoPoseData that describes the device with respect to the
        /// start of service or area description. The result position and
        /// rotation can be used to set Unity's Transform.position and
        /// Transform.rotation.
        /// </summary>
        /// <param name="poseData">
        /// The input pose data that is going to be converted, please note that
        /// the pose data has to be in the start of service with respect to
        /// device frame.
        /// </param>
        /// <param name="position">The result position data.</param>
        /// <param name="rotation">The result rotation data.</param>
        public static void TangoPoseToWorldTransform(TangoPoseData poseData,
                                                     out Vector3 position,
                                                     out Quaternion rotation)
        {
            if (poseData == null)
            {
                Debug.Log("Invalid poseData.\n" + Environment.StackTrace);
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            if (poseData.framePair.targetFrame != TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                Debug.Log("Invalid target frame of the poseData.\n" + Environment.StackTrace);
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            if (poseData.framePair.baseFrame !=
                TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                poseData.framePair.baseFrame !=
                TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION)
            {
                Debug.Log("Invalid base frame of the poseData.\n" + Environment.StackTrace);
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            Matrix4x4 startServiceTDevice = poseData.ToMatrix4x4();
            Matrix4x4 unityWorldTUnityCamera = UNITY_WORLD_T_START_SERVICE *
                                               startServiceTDevice *
                                               DEVICE_T_UNITY_CAMERA *
                                               m_devicePoseRotation;

            // Extract final position, rotation.
            position = unityWorldTUnityCamera.GetColumn(3);
            rotation = Quaternion.LookRotation(unityWorldTUnityCamera.GetColumn(2),
                                               unityWorldTUnityCamera.GetColumn(1));
        }

        /// <summary>
        /// Update current additional rotation matrix based on the display orientation and color camera orientation.
        ///
        /// Not that m_colorCameraPoseRotation will need to compensate the rotation with physical color camera.
        /// </summary>
        /// <param name="displayRotation">Orientation of current activity. Index enum is same same as Android screen
        /// orientation standard.</param>
        /// <param name="colorCameraRotation">Orientation of current color camera sensor. Index enum is same as Android
        /// camera orientation standard.</param>
        public static void UpdatePoseMatrixFromDeviceRotation(OrientationManager.Rotation displayRotation,
                                                              OrientationManager.Rotation colorCameraRotation)
        {
            Tango.OrientationManager.Rotation r = RotateFromAToB(displayRotation, colorCameraRotation);

            switch (r)
            {
            case Tango.OrientationManager.Rotation.ROTATION_90:
                m_colorCameraPoseRotation = ROTATION90_T_DEFAULT;
                break;
            case Tango.OrientationManager.Rotation.ROTATION_180:
                m_colorCameraPoseRotation = ROTATION180_T_DEFAULT;
                break;
            case Tango.OrientationManager.Rotation.ROTATION_270:
                m_colorCameraPoseRotation = ROTATION270_T_DEFAULT;
                break;
            default:
                m_colorCameraPoseRotation = Matrix4x4.identity;
                break;
            }

            switch (displayRotation)
            {
            case Tango.OrientationManager.Rotation.ROTATION_90:
                m_devicePoseRotation = ROTATION90_T_DEFAULT;
                break;
            case Tango.OrientationManager.Rotation.ROTATION_180:
                m_devicePoseRotation = ROTATION180_T_DEFAULT;
                break;
            case Tango.OrientationManager.Rotation.ROTATION_270:
                m_devicePoseRotation = ROTATION270_T_DEFAULT;
                break;
            default:
                m_devicePoseRotation = Matrix4x4.identity;
                break;
            }
        }
#endregion

#region DEPTH_FUNCTIONS
        /// <summary>
        /// Fits a plane to a point cloud near a user-specified location. This
        /// occurs in two passes. First, all points in cloud within
        /// certain pixel distance to <c>uvCoordinates</c> after projection are kept. Then a
        /// plane is fit to the subset cloud using RANSAC. After the initial fit
        /// all inliers from the original cloud are used to refine the plane
        /// model.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if plane is found successfully, <c>false</c> otherwise.
        /// </returns>
        /// <param name="pointCloud">
        /// The point cloud. Cannot be null and must have at least three points.
        /// </param>
        /// <param name="colorCameraTimestamp">
        /// Color camera's timestamp when UV is captured.
        /// </param>
        /// <param name="uvCoordinates">
        /// The UV coordinates for the user selection. This is expected to be
        /// in Unity viewport space. The bottom-left of the camera is (0,0);
        /// the top-right is (1,1).
        /// </param>
        /// <param name="intersectionPoint">
        /// The output point in depth camera coordinates that the user selected.
        /// </param>
        /// <param name="planeModel">
        /// The four parameters a, b, c, d for the general plane
        /// equation ax + by + cz + d = 0 of the plane fit. The first three
        /// components are a unit vector. The output is in the coordinate system of
        /// the requested output frame. Cannot be NULL.
        /// </param>
        public static bool FitPlaneModelNearClick(
            TangoPointCloudData pointCloud,
            double colorCameraTimestamp,
            Vector2 uvCoordinates,
            out Vector3 intersectionPoint,
            out DVector4 planeModel)
        {
            TangoPoseData depth_T_colorCameraPose = new TangoPoseData();

            int returnValue = TangoSupportAPI.TangoSupport_calculateRelativePose(
                pointCloud.m_timestamp,
                TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH,
                colorCameraTimestamp,
                TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR,
                depth_T_colorCameraPose);

            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.LogError("TangoSupport_calculateRelativePose error. " + Environment.StackTrace);
                intersectionPoint = new Vector3(0.0f, 0.0f, 0.0f);
                planeModel = new DVector4();
                return false;
            }

            GCHandle pointCloudHandle = GCHandle.Alloc(pointCloud.m_points,
                                                       GCHandleType.Pinned);
            TangoPointCloudIntPtr tangoPointCloudIntPtr = new TangoPointCloudIntPtr();
            tangoPointCloudIntPtr.m_points = pointCloudHandle.AddrOfPinnedObject();
            tangoPointCloudIntPtr.m_timestamp = pointCloud.m_timestamp;
            tangoPointCloudIntPtr.m_numPoints = pointCloud.m_numPoints;

            // Unity viewport space is: the bottom-left of the camera is (0,0);
            // the top-right is (1,1).
            // Tango (Android) defined UV space is: the top-left of the camera is (0,0);
            // the bottom-right is (1,1).
            Vector2 uvCoordinatesTango = new Vector2(uvCoordinates.x, 1.0f - uvCoordinates.y);
            DVector3 doubleIntersectionPoint = new DVector3();

            DVector4 pointCloudRotation = DVector4.IdentityQuaternion;
            DVector3 pointCloudTranslation = DVector3.Zero;

            returnValue = TangoSupportAPI.TangoSupport_fitPlaneModelNearPoint(
                ref tangoPointCloudIntPtr,
                ref pointCloudTranslation,
                ref pointCloudRotation,
                ref uvCoordinatesTango,
                AndroidHelper.GetDisplayRotation(),
                ref depth_T_colorCameraPose.translation,
                ref depth_T_colorCameraPose.orientation,
                out doubleIntersectionPoint,
                out planeModel);

            pointCloudHandle.Free();

            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.LogError("TangoSupport_fitPlaneModelNearPoint error. " + Environment.StackTrace);
                intersectionPoint = new Vector3(0.0f, 0.0f, 0.0f);
                planeModel = new DVector4();
                return false;
            }
            else
            {
                intersectionPoint = doubleIntersectionPoint.ToVector3();
            }

            return true;
        }

        /// <summary>
        /// Calculates the depth in the color camera space at a user-specified
        /// location using nearest-neighbor interpolation.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if a point is found is found successfully, <c>false</c> otherwise.
        /// </returns>
        /// <param name="pointCloud">
        /// The point cloud. Cannot be null and must have at least three points.
        /// </param>
        /// <param name="colorCameraTimestamp">
        /// Color camera's timestamp when UV is captured.
        /// </param>
        /// <param name="uvCoordinates">
        /// The UV coordinates for the user selection. This is expected to be
        /// in Unity viewport space. The bottom-left of the camera is (0,0);
        /// the top-right is (1,1).
        /// </param>
        /// <param name="colorCameraPoint">
        /// The point (x, y, z), where (x, y) is the back-projection of the UV
        /// coordinates to the color camera space and z is the z coordinate of
        /// the point in the point cloud nearest to the user selection after
        /// projection onto the image plane. If there is not a point cloud point
        /// close to the user selection after projection onto the image plane,
        /// then the point will be set to (0.0, 0.0, 0.0) and isValidPoint will
        /// be set to false.
        /// </param>
        public static bool ScreenCoordinateToWorldNearestNeighbor(
            TangoPointCloudData pointCloud,
            double colorCameraTimestamp,
            Vector2 uvCoordinates,
            out Vector3 colorCameraPoint)
        {
            TangoPoseData depth_T_colorCameraPose = new TangoPoseData();

            int returnValue = TangoSupportAPI.TangoSupport_calculateRelativePose(
                pointCloud.m_timestamp,
                TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH,
                colorCameraTimestamp,
                TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR,
                depth_T_colorCameraPose);

            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.LogError("TangoSupport_calculateRelativePose error. " + Environment.StackTrace);
                colorCameraPoint = Vector3.zero;
                return false;
            }

            GCHandle pointCloudHandle = GCHandle.Alloc(pointCloud.m_points,
                                                       GCHandleType.Pinned);
            TangoPointCloudIntPtr tangoPointCloudIntPtr = new TangoPointCloudIntPtr();
            tangoPointCloudIntPtr.m_points = pointCloudHandle.AddrOfPinnedObject();
            tangoPointCloudIntPtr.m_timestamp = pointCloud.m_timestamp;
            tangoPointCloudIntPtr.m_numPoints = pointCloud.m_numPoints;

            // Unity viewport space is: the bottom-left of the camera is (0,0);
            // the top-right is (1,1).
            // Tango (Android) defined UV space is: the top-left of the camera is (0,0);
            // the bottom-right is (1,1).
            Vector2 uvCoordinatesTango = new Vector2(uvCoordinates.x, 1.0f - uvCoordinates.y);

            DVector4 pointCloudRotation = DVector4.IdentityQuaternion;
            DVector3 pointCloudTranslation = DVector3.Zero;

            returnValue = TangoSupportAPI.TangoSupport_getDepthAtPointNearestNeighbor(
                ref tangoPointCloudIntPtr, 
                ref pointCloudTranslation,
                ref pointCloudRotation,
                ref uvCoordinatesTango,
                AndroidHelper.GetDisplayRotation(),
                ref depth_T_colorCameraPose.translation,
                ref depth_T_colorCameraPose.orientation,
                out colorCameraPoint);

            pointCloudHandle.Free();

            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.LogError("TangoSupport_getDepthAtPointNearestNeighbor error. " + Environment.StackTrace);
                colorCameraPoint = Vector3.zero;
                return false;
            }

            return true;
        }
#endregion

#region MARKER_DETECTION_FUNCTIONS
        /// <summary>
        /// Detect one or more markers in the input image.
        /// </summary>
        /// <param name="imageBuffer">
        /// The input image buffer.
        /// </param>
        /// <param name="cameraId">
        /// Camera that is used for detecting markers, can be TangoEnums.TangoCameraId.TANGO_CAMERA_FISHEYE or
        /// TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR.
        /// </param>
        /// <param name="markerType">
        /// Target marker's type. Current support marker types are QR marker and Alvar marker.
        /// </param>
        /// <param name="markerSize">
        /// Physical size of marker's length.
        /// </param>
        /// <param name="markers">
        /// The returned marker list.
        /// </param>
        /// <returns>
        /// Common.ErrorType.TANGO_SUCCESS on success, Common.ErrorType.TANGO_INVALID on invalid input, and
        /// Common.ErrorType.TANGO_ERROR on failure.
        /// </returns>
        public static bool DetectMarkers(TangoUnityImageData imageBuffer,
            TangoEnums.TangoCameraId cameraId,
            MarkerType markerType,
            double markerSize,
            List<Marker> markers)
        {
            if (markers == null)
            {
                Debug.Log("markers is null. " + Environment.StackTrace);
                return false;
            }
            
            // Clear any existing marker
            markers.Clear();

            // Detect marker.
            TangoImageBuffer buffer = new TangoImageBuffer();
            GCHandle gchandle = GCHandle.Alloc(imageBuffer.data, GCHandleType.Pinned);
            IntPtr ptr = gchandle.AddrOfPinnedObject();
            buffer.data = ptr;

            buffer.format = imageBuffer.format;
            buffer.frame_number = imageBuffer.frame_number;
            buffer.height = imageBuffer.height;
            buffer.stride = imageBuffer.stride;
            buffer.timestamp = imageBuffer.timestamp;
            buffer.width = imageBuffer.width;

            // Get Pose.
            TangoPoseData poseData = new TangoPoseData();
            TangoCoordinateFramePair pair;
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
            PoseProvider.GetPoseAtTime(poseData, buffer.timestamp, pair);

            APIMarkerList rawAPIMarkerList = new APIMarkerList();
            APIMarkerParam rawMarkerParam = new APIMarkerParam(markerType, markerSize);

            int ret = TangoSupportAPI.TangoSupport_detectMarkers(ref buffer, cameraId,
                                                                 ref poseData.translation,
                                                                 ref poseData.orientation,
                                                                 ref rawMarkerParam,
                                                                 ref rawAPIMarkerList);

            gchandle.Free();

            if (ret != Common.ErrorType.TANGO_SUCCESS)
            {
                return false;
            }

            if (rawAPIMarkerList.markerCount != 0)
            {
                List<APIMarker> apiMarkers = new List<APIMarker>();

                MarshallingHelper.MarshalUnmanagedStructArrayToList<TangoSupport.APIMarker>(
                    rawAPIMarkerList.markers,
                    rawAPIMarkerList.markerCount,
                    apiMarkers);

                for (int i = 0; i < apiMarkers.Count; ++i)
                {
                    APIMarker apiMarker = apiMarkers[i];
                    Marker marker = new Marker();
                    marker.m_type = apiMarker.m_type;
                    marker.m_timestamp = apiMarker.m_timestamp;
                    marker.m_content = apiMarker.m_content;

                    // Covert 2D corner points from pixel space to UV space.
                    marker.m_corner2DP0.x = apiMarker.m_corner2DP0.x / buffer.width;
                    marker.m_corner2DP0.y = apiMarker.m_corner2DP0.y / buffer.height;
                    marker.m_corner2DP1.x = apiMarker.m_corner2DP1.x / buffer.width;
                    marker.m_corner2DP1.y = apiMarker.m_corner2DP1.y / buffer.height;
                    marker.m_corner2DP2.x = apiMarker.m_corner2DP2.x / buffer.width;
                    marker.m_corner2DP2.y = apiMarker.m_corner2DP2.y / buffer.height;
                    marker.m_corner2DP3.x = apiMarker.m_corner2DP3.x / buffer.width;
                    marker.m_corner2DP3.y = apiMarker.m_corner2DP3.y / buffer.height;

                    // Convert 3D corner points from Start of Service space to Unity World space.
                    marker.m_corner3DP0 = GetMarkerInUnitySpace(apiMarker.m_corner3DP0);
                    marker.m_corner3DP1 = GetMarkerInUnitySpace(apiMarker.m_corner3DP1);
                    marker.m_corner3DP2 = GetMarkerInUnitySpace(apiMarker.m_corner3DP2);
                    marker.m_corner3DP3 = GetMarkerInUnitySpace(apiMarker.m_corner3DP3);

                    // Convert pose from Start of Service to Unity World space.
                    Vector3 translation = new Vector3(
                        (float)apiMarker.m_translation.x, 
                        (float)apiMarker.m_translation.y, 
                        (float)apiMarker.m_translation.z);
                    Quaternion orientation = new Quaternion(
                        (float)apiMarker.m_rotation.x,
                        (float)apiMarker.m_rotation.y,
                        (float)apiMarker.m_rotation.z,
                        (float)apiMarker.m_rotation.w);

                    Matrix4x4 ss_T_marker = Matrix4x4.TRS(translation, orientation, Vector3.one);

                    // Note that UNITY_WORLD_T_START_SERVICE is involutory matrix. The actually transform
                    // we wanted to multiply on the right hand side is START_SERVICE_T_UNITY_WORLD.
                    Matrix4x4 uw_T_u_marker = TangoSupport.UNITY_WORLD_T_START_SERVICE * 
                        ss_T_marker * TangoSupport.UNITY_WORLD_T_START_SERVICE;
                    marker.m_translation = uw_T_u_marker.GetColumn(3);
                    marker.m_orientation = Quaternion.LookRotation(uw_T_u_marker.GetColumn(2),
                        uw_T_u_marker.GetColumn(1));

                    // Add the marker to the output list
                    markers.Add(marker);
                }
            }

            TangoSupportAPI.TangoSupport_freeMarkerList(ref rawAPIMarkerList);

            return false;
        }

        /// <summary>
        /// Convert input marker position from Start Of Service frame to Unity frame.
        /// </summary>
        /// <param name="input">
        /// Marker's position in Start Of Service frame.
        /// </param>
        /// <returns>
        /// Marker's position in Unity frame.
        /// </returns>
        private static Vector3 GetMarkerInUnitySpace(Vector3 input)
        {
            // The actual math is:
            //     Matrix4x4 ss_T_marker = Matrix4x4.TRS(input, Quaternion.identity, Vector3.one);
            //     Matrix4x4 uw_T_u_marker = TangoSupport.UNITY_WORLD_T_START_SERVICE * ss_T_marker;
            //     input = uw_T_u_marker * input;
            // To minimize computation, we hard coded the math to an axis swap.
           return new Vector3(input.x, input.z, input.y);
        }
#endregion

#region DATA_TYPES
        /// <summary>
        /// A structure to define contents of a marker, which can be any of the
        /// marker types supported.
        /// </summary>
        public struct Marker
        {
            /// <summary>
            /// The type of the marker.
            /// </summary>
            public MarkerType m_type;

            /// <summary>
            /// The timestamp of the image from which the marker was detected.
            /// </summary>
            public double m_timestamp;

            /// <summary>
            /// The content of the marker. For AR tags, this is the string format of the
            /// tag id. For QR codes, this is the string content of the code.
            /// </summary>
            public string m_content;

            /// <summary>
            /// Marker corners in input image uv coordinates, with coordinate values in range of [0..1].
            /// For all marker types, the first corner is the lower left corner, the
            /// second corner is the lower right corner, the third corner is the upper
            /// right corner, and the last corner is the upper left corner.
            ///
            /// P3 -- P2
            /// |     |
            /// P0 -- P1.
            /// </summary>
            public Vector2 m_corner2DP0;

            /// <summary>
            /// Corner2D P1.
            /// </summary>
            public Vector2 m_corner2DP1;

            /// <summary>
            /// Corner2D P2.
            /// </summary>
            public Vector2 m_corner2DP2;

            /// <summary>
            /// Corner2D P3.
            /// </summary>
            public Vector2 m_corner2DP3;

            /// <summary>
            /// Marker corners in the output frame, which is defined in Unity World space. The
            /// locations of the corners are the same as in m_corner2DP* fields.
            /// </summary>
            public Vector3 m_corner3DP0;

            /// <summary>
            /// Corner3D P1.
            /// </summary>
            public Vector3 m_corner3DP1;

            /// <summary>
            /// Corner3D P2.
            /// </summary>
            public Vector3 m_corner3DP2;

            /// <summary>
            /// Corner3D P3.
            /// </summary>
            public Vector3 m_corner3DP3;

            /// <summary>
            /// Marker pose - orientation is a Unity quaternion. 
            /// Both translation and orientation are defined in the Unity World space.
            /// The marker pose defines a marker local frame, in which:
            ///  X = to the right on the tag
            ///  Y = to the up on the tag
            ///  Z = pointing forward from the user's perspective.
            /// </summary>
            public Vector3 m_translation;
            public Quaternion m_orientation;
        }

        /// <summary>
        /// A structure to define contents of a marker, which can be any of the
        /// marker types supported.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct APIMarker
        {
            /// <summary>
            /// The type of the marker.
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public MarkerType m_type;

            /// <summary>
            /// The timestamp of the image from which the marker was detected.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_timestamp;

            /// <summary>
            /// The content of the marker. For AR tags, this is the string format of the
            /// tag id. For QR codes, this is the string content of the code.
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public string m_content;

            /// <summary>
            /// The size of content, in bytes.
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int m_contentSize;

            /// <summary>
            /// Marker corners in input image pixel coordinates.
            /// For all marker types, the first corner is the lower left corner, the
            /// second corner is the lower right corner, the third corner is the upper
            /// right corner, and the last corner is the upper left corner.
            ///
            /// P3 -- P2
            /// |     |
            /// P0 -- P1.
            /// </summary>
            public Vector2 m_corner2DP0;

            /// <summary>
            /// Corner2D P1.
            /// </summary>
            public Vector2 m_corner2DP1;

            /// <summary>
            /// Corner2D P2.
            /// </summary>
            public Vector2 m_corner2DP2;

            /// <summary>
            /// Corner2D P3.
            /// </summary>
            public Vector2 m_corner2DP3;

            /// <summary>
            /// Marker corners in the output frame, which is defined by the translation
            /// and orientation pair passed to TangoSupport_detectMarkers() function. The
            /// location of the corner is the same as in corners_2d field.
            /// </summary>
            public Vector3 m_corner3DP0;

            /// <summary>
            /// Corner3D P1.
            /// </summary>
            public Vector3 m_corner3DP1;

            /// <summary>
            /// Corner3D P2.
            /// </summary>
            public Vector3 m_corner3DP2;
            
            /// <summary>
            /// Corner3D P3.
            /// </summary>
            public Vector3 m_corner3DP3;

            /// <summary>
            /// Marker pose - orientation is a Hamilton quaternion specified as
            /// (x, y, z, w). Both translation and orientation are defined in the output
            /// frame, which is defined by the translation and orientation pair passed to
            /// TangoSupport_detectMarkers() function.
            /// The marker pose defines a marker local frame, in which:
            ///  X = to the right on the tag
            ///  Y = to the up on the tag
            ///  Z = pointing out of the tag towards the user.
            /// </summary>
            public DVector3 m_translation;
            public DVector4 m_rotation;
        }

        /// <summary>
        /// A structure to define parameters for passing marker detection
        /// parameters.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIMarkerParam
        {
            /// <summary>
            /// Type of marker to be detected.
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public MarkerType m_type;

            /// <summary>
            /// The physical size of the marker in meters.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_markerSize;

            /// <summary>
            /// Construct APIMarkerParam from type and marker's size.
            /// </summary>
            /// <param name="type">Marker's type.</param>
            /// <param name="markerSize">Marker's physical size.</param>
            public APIMarkerParam(MarkerType type, double markerSize)
            {
                m_type = type;
                m_markerSize = markerSize;
            }
        }

        /// <summary>
        /// A structure that stores a list of markers. After calling
        /// TangoSupport_detectMarkers() with a TangoSupportAPIMarkerList object, the
        /// object needs to be released by calling TangoSupport_freeMarkersList()
        /// function.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APIMarkerList 
        {
            public IntPtr markers;
            
            [MarshalAs(UnmanagedType.I4)]
            public int markerCount;
        }
#endregion

#region API_Functions
        /// <summary>
        /// Wraps the Tango Support C API.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper")]
        private struct TangoSupportAPI
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            /// <summary>
            /// Note that we intentionally using Common.TANGO_UNITY_DLL to call support library
            /// initialization function so that all function pointers are set up from C layer directly.
            /// </summary>
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern void TangoUnity_initializeTangoSupportLibrary();

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_calculateRelativePose(
                double bseTimestamp, TangoEnums.TangoCoordinateFrameType baseFrame,
                double targetTimestampe, TangoEnums.TangoCoordinateFrameType target,
                [In, Out] TangoPoseData pose);

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_fitPlaneModelNearPoint(
                ref TangoPointCloudIntPtr pointCloud, 
                ref DVector3 pointCloudTranslation,
                ref DVector4 pointCloundOrientation,
                ref Vector2 uvCoordinates,
                OrientationManager.Rotation rotation,
                ref DVector3 cameraTranslation,
                ref DVector4 cameraOrientation,
                out DVector3 intersectionPoint,
                out DVector4 planeModel);

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_getDepthAtPointNearestNeighbor(
                ref TangoPointCloudIntPtr point_cloud, 
                ref DVector3 poitnCloudTranslation,
                ref DVector4 pointCloudOrientation,
                ref Vector2 uvCoordinatesInColorCamera,
                OrientationManager.Rotation display_rotation,
                ref DVector3 colorCameraTranslation,
                ref DVector4 colorCameraOrientation,
                out Vector3 outputPoint);

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_detectMarkers(ref TangoImageBuffer image,
                TangoEnums.TangoCameraId cameraId,
                ref DVector3 translation,
                ref DVector4 orientation,
                ref APIMarkerParam param,
                ref APIMarkerList apiMarkerList);

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern void TangoSupport_freeMarkerList(ref APIMarkerList list);
#else
            public static int TangoUnity_initializeTangoSupportLibrary()
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoSupport_calculateRelativePose(
                double bseTimestamp, TangoEnums.TangoCoordinateFrameType baseFrame,
                double targetTimestampe, TangoEnums.TangoCoordinateFrameType target,
                TangoPoseData pose)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoSupport_fitPlaneModelNearPoint(
                ref TangoPointCloudIntPtr pointCloud, 
                ref DVector3 pointCloudTranslation,
                ref DVector4 pointCloundOrientation,
                ref Vector2 uvCoordinates,
                OrientationManager.Rotation rotation,
                ref DVector3 cameraTranslation,
                ref DVector4 cameraOrientation,
                out DVector3 intersectionPoint,
                out DVector4 planeModel)
                {
                    intersectionPoint = new DVector3();
                    planeModel = new DVector4();
                    return Common.ErrorType.TANGO_SUCCESS;
                }

            public static int TangoSupport_getDepthAtPointNearestNeighbor(
                ref TangoPointCloudIntPtr point_cloud, 
                ref DVector3 poitnCloudTranslation,
                ref DVector4 pointCloudOrientation,
                ref Vector2 uvCoordinatesInColorCamera,
                OrientationManager.Rotation display_rotation,
                ref DVector3 colorCameraTranslation,
                ref DVector4 colorCameraOrientation,
                out Vector3 outputPoint)
            {
                outputPoint = Vector3.zero;
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoSupport_detectMarkers(ref TangoImageBuffer image,
                TangoEnums.TangoCameraId cameraId, ref DVector3 translation, ref DVector4 orientation,
                ref APIMarkerParam param, ref APIMarkerList apiMarkerList)
            {
                apiMarkerList = new APIMarkerList();
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static void TangoSupport_freeMarkerList(ref APIMarkerList list)
            {
            }
#endif
        }
        #endregion
    }
}
