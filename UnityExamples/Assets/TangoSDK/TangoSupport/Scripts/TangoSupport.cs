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
        /// Matrix that transforms from Start of Service to the Unity World.
        /// </summary>
        private static readonly Matrix4x4 UNITY_WORLD_T_START_SERVICE = new Matrix4x4
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
        private static readonly Matrix4x4 DEVICE_T_UNITY_CAMERA = new Matrix4x4
        {
            m00 = 1.0f, m01 = 0.0f, m02 =  0.0f, m03 = 0.0f,
            m10 = 0.0f, m11 = 1.0f, m12 =  0.0f, m13 = 0.0f,
            m20 = 0.0f, m21 = 0.0f, m22 = -1.0f, m23 = 0.0f,
            m30 = 0.0f, m31 = 0.0f, m32 =  0.0f, m33 = 1.0f
        };

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

        /// <summary>
        /// Fits a plane to a point cloud near a user-specified location. This
        /// occurs in two passes. First, all points in cloud within
        /// <c>maxPixelDistance</c> to <c>uvCoordinates</c> after projection are kept. Then a
        /// plane is fit to the subset cloud using RANSAC. After the initial fit
        /// all inliers from the original cloud are used to refine the plane
        /// model.
        /// </summary>
        /// <returns>
        /// Common.ErrorType.TANGO_SUCCESS on success,
        /// Common.ErrorType.TANGO_INVALID on invalid input, and
        /// Common.ErrorType.TANGO_ERROR on failure.
        /// </returns>
        /// <param name="pointCloud">
        /// The point cloud. Cannot be null and must have at least three points.
        /// </param>
        /// <param name="pointCount">
        /// The number of points to read from the point cloud.
        /// </param>
        /// <param name="timestamp">The timestamp of the point cloud.</param>
        /// <param name="cameraIntrinsics">
        /// The camera intrinsics for the color camera. Cannot be null.
        /// </param>
        /// <param name="matrix">
        /// Transformation matrix of the color camera with respect to the Unity
        /// World frame.
        /// </param>
        /// <param name="uvCoordinates">
        /// The UV coordinates for the user selection. This is expected to be
        /// between (0.0, 0.0) and (1.0, 1.0).
        /// </param>
        /// <param name="intersectionPoint">
        /// The output point in depth camera coordinates that the user selected.
        /// </param>
        /// <param name="plane">The plane fit.</param>
        public static int FitPlaneModelNearClick(
            Vector3[] pointCloud, int pointCount, double timestamp,
            TangoCameraIntrinsics cameraIntrinsics, ref Matrix4x4 matrix,
            Vector2 uvCoordinates, out Vector3 intersectionPoint,
            out Plane plane)
        {
            GCHandle pointCloudHandle = GCHandle.Alloc(pointCloud,
                                                       GCHandleType.Pinned);

            TangoXYZij pointCloudXyzIj = new TangoXYZij();
            pointCloudXyzIj.timestamp = timestamp;
            pointCloudXyzIj.xyz_count = pointCount;
            pointCloudXyzIj.xyz = pointCloudHandle.AddrOfPinnedObject();

            DMatrix4x4 doubleMatrix = new DMatrix4x4(matrix);

            // Unity has Y pointing screen up; Tango camera has Y pointing
            // screen down.
            Vector2 uvCoordinatesTango = new Vector2(uvCoordinates.x,
                                                     1.0f - uvCoordinates.y);

            DVector3 doubleIntersectionPoint = new DVector3();
            double[] planeArray = new double[4];

            int returnValue = TangoSupportAPI.TangoSupport_fitPlaneModelNearPointMatrixTransform(
                pointCloudXyzIj, cameraIntrinsics, ref doubleMatrix,
                ref uvCoordinatesTango,
                out doubleIntersectionPoint, planeArray);

            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                intersectionPoint = new Vector3(0.0f, 0.0f, 0.0f);
                plane = new Plane(new Vector3(0.0f, 0.0f, 0.0f), 0.0f);
            }
            else
            {
                intersectionPoint = doubleIntersectionPoint.ToVector3();
                Vector3 normal = new Vector3((float)planeArray[0],
                                             (float)planeArray[1],
                                             (float)planeArray[2]);
                float distance = (float)planeArray[3] / normal.magnitude;

                plane = new Plane(normal, distance);
            }

            pointCloudHandle.Free();

            return returnValue;
        }

        /// <summary>
        /// DEPRECATED. Use ScreenCoordinateToWorldNearestNeighbor instead.
        ///
        /// Calculates the depth in the color camera space at a user-specified
        /// location using nearest-neighbor interpolation.
        /// </summary>
        /// <returns>
        /// Common.ErrorType.TANGO_SUCCESS on success and
        /// Common.ErrorType.TANGO_INVALID on invalid input.
        /// </returns>
        /// <param name="pointCloud">
        /// The point cloud. Cannot be null and must have at least one point.
        /// </param>
        /// <param name="pointCount">
        /// The number of points to read from the point cloud.
        /// </param>
        /// <param name="timestamp">The timestamp of the depth points.</param>
        /// <param name="cameraIntrinsics">
        /// The camera intrinsics for the color camera. Cannot be null.
        /// </param>
        /// <param name="matrix">
        /// Transformation matrix of the color camera with respect to the Unity
        /// World frame.
        /// </param>
        /// <param name="uvCoordinates">
        /// The UV coordinates for the user selection. This is expected to be
        /// between (0.0, 0.0) and (1.0, 1.0).
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
        /// <param name="isValidPoint">
        /// A flag valued true if there is a point cloud point close to the user
        /// selection after projection onto the image plane and valued false
        /// otherwise.
        /// </param>
        [Obsolete("Use ScreenCoordinateToWorldNearestNeighbor instead.")]
        public static int GetDepthAtPointNearestNeighbor(
            Vector3[] pointCloud, int pointCount, double timestamp,
            TangoCameraIntrinsics cameraIntrinsics, ref Matrix4x4 matrix,
            Vector2 uvCoordinates, out Vector3 colorCameraPoint,
            out bool isValidPoint)
        {
            return ScreenCoordinateToWorldNearestNeighbor(pointCloud,
                pointCount, timestamp, cameraIntrinsics, ref matrix,
                uvCoordinates, out colorCameraPoint, out isValidPoint);
        }

        /// <summary>
        /// Calculates the depth in the color camera space at a user-specified
        /// location using nearest-neighbor interpolation.
        /// </summary>
        /// <returns>
        /// Common.ErrorType.TANGO_SUCCESS on success and
        /// Common.ErrorType.TANGO_INVALID on invalid input.
        /// </returns>
        /// <param name="pointCloud">
        /// The point cloud. Cannot be null and must have at least one point.
        /// </param>
        /// <param name="pointCount">
        /// The number of points to read from the point cloud.
        /// </param>
        /// <param name="timestamp">The timestamp of the depth points.</param>
        /// <param name="cameraIntrinsics">
        /// The camera intrinsics for the color camera. Cannot be null.
        /// </param>
        /// <param name="matrix">
        /// Transformation matrix of the color camera with respect to the Unity
        /// World frame.
        /// </param>
        /// <param name="uvCoordinates">
        /// The UV coordinates for the user selection. This is expected to be
        /// between (0.0, 0.0) and (1.0, 1.0).
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
        /// <param name="isValidPoint">
        /// A flag valued true if there is a point cloud point close to the user
        /// selection after projection onto the image plane and valued false
        /// otherwise.
        /// </param>
        public static int ScreenCoordinateToWorldNearestNeighbor(
            Vector3[] pointCloud, int pointCount, double timestamp,
            TangoCameraIntrinsics cameraIntrinsics, ref Matrix4x4 matrix,
            Vector2 uvCoordinates, out Vector3 colorCameraPoint,
            out bool isValidPoint)
        {
            GCHandle pointCloudHandle = GCHandle.Alloc(pointCloud,
                                                       GCHandleType.Pinned);

            TangoXYZij pointCloudXyzIj = new TangoXYZij();
            pointCloudXyzIj.timestamp = timestamp;
            pointCloudXyzIj.xyz_count = pointCount;
            pointCloudXyzIj.xyz = pointCloudHandle.AddrOfPinnedObject();

            DMatrix4x4 doubleMatrix = new DMatrix4x4(matrix);

            // Unity has Y pointing screen up; Tango camera has Y pointing
            // screen down.
            Vector2 uvCoordinatesTango = new Vector2(uvCoordinates.x,
                                                     1.0f - uvCoordinates.y);

            int isValidPointInteger;

            int returnValue = TangoSupportAPI.TangoSupport_getDepthAtPointNearestNeighborMatrixTransform(
                pointCloudXyzIj, cameraIntrinsics, ref doubleMatrix,
                ref uvCoordinatesTango, out colorCameraPoint,
                out isValidPointInteger);

            isValidPoint = isValidPointInteger != 0;

            pointCloudHandle.Free();

            return returnValue;
        }

        /// <summary>
        /// Calculates the depth in the color camera space at a user-specified
        /// location using bilateral filtering weighted by both spatial distance
        /// from the user coordinate and by intensity similarity.
        /// </summary>
        /// <returns>
        /// Common.ErrorType.TANGO_SUCCESS on success,
        /// Common.ErrorType.TANGO_INVALID on invalid input, and
        /// Common.ErrorType.TANGO_ERROR on failure.
        /// </returns>
        /// <param name="pointCloud">
        /// The point cloud. Cannot be null and must have at least one point.
        /// </param>
        /// <param name="pointCount">
        /// The number of points to read from the point cloud.
        /// </param>
        /// <param name="timestamp">The timestamp of the depth points.</param>
        /// <param name="cameraIntrinsics">
        /// The camera intrinsics for the color camera. Cannot be null.
        /// </param>
        /// <param name="colorImage">
        /// The color image buffer. Cannot be null.
        /// </param>
        /// <param name="matrix">
        /// Transformation matrix of the color camera with respect to the Unity
        /// World frame.
        /// </param>
        /// <param name="uvCoordinates">
        /// The UV coordinates for the user selection. This is expected to be
        /// between (0.0, 0.0) and (1.0, 1.0).
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
        /// <param name="isValidPoint">
        /// A flag valued true if there is a point cloud point close to the user
        /// selection after projection onto the image plane and valued false
        /// otherwise.
        /// </param>
        public static int ScreenCoordinateToWorldBilateral(
            Vector3[] pointCloud, int pointCount, double timestamp,
            TangoCameraIntrinsics cameraIntrinsics, TangoImageBuffer colorImage,
            ref Matrix4x4 matrix, Vector2 uvCoordinates,
            out Vector3 colorCameraPoint, out bool isValidPoint)
        {
            GCHandle pointCloudHandle = GCHandle.Alloc(pointCloud,
                                                       GCHandleType.Pinned);

            TangoXYZij pointCloudXyzIj = new TangoXYZij();
            pointCloudXyzIj.timestamp = timestamp;
            pointCloudXyzIj.xyz_count = pointCount;
            pointCloudXyzIj.xyz = pointCloudHandle.AddrOfPinnedObject();

            DMatrix4x4 doubleMatrix = new DMatrix4x4(matrix);

            // Unity has Y pointing screen up; Tango camera has Y pointing
            // screen down.
            Vector2 uvCoordinatesTango = new Vector2(uvCoordinates.x,
                                                     1.0f - uvCoordinates.y);

            int isValidPointInteger;

            int returnValue = TangoSupportAPI.TangoSupport_getDepthAtPointBilateralCameraIntrinsicsMatrixTransform(
                pointCloudXyzIj, cameraIntrinsics, colorImage, ref doubleMatrix,
                ref uvCoordinatesTango, out colorCameraPoint,
                out isValidPointInteger);

            isValidPoint = isValidPointInteger != 0;

            pointCloudHandle.Free();

            return returnValue;
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
        /// A double-precision 4x4 transformation matrix.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DMatrix4x4
        {
            /// <summary>
            /// 0,0-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_00;

            /// <summary>
            /// 1,0-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_10;

            /// <summary>
            /// 2,0-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_20;

            /// <summary>
            /// 3,0-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_30;

            /// <summary>
            /// 0,1-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_01;

            /// <summary>
            /// 1,1-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_11;

            /// <summary>
            /// 2,1-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_21;

            /// <summary>
            /// 3,1-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_31;

            /// <summary>
            /// 0,2-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_02;

            /// <summary>
            /// 1,2-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_12;

            /// <summary>
            /// 2,2-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_22;

            /// <summary>
            /// 3,2-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_32;

            /// <summary>
            /// 0,3-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_03;

            /// <summary>
            /// 1,3-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_13;

            /// <summary>
            /// 2,3-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_23;

            /// <summary>
            /// 3,3-th element of this matrix.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_33;

            /// <summary>
            /// Creates a new double-precision matrix from the given
            /// single-precision matrix.
            /// </summary>
            /// <param name="matrix">A single-precision matrix.</param>
            public DMatrix4x4(Matrix4x4 matrix)
            {
                m_00 = matrix.m00;
                m_10 = matrix.m10;
                m_20 = matrix.m20;
                m_30 = matrix.m30;

                m_01 = matrix.m01;
                m_11 = matrix.m11;
                m_21 = matrix.m21;
                m_31 = matrix.m31;

                m_02 = matrix.m02;
                m_12 = matrix.m12;
                m_22 = matrix.m22;
                m_32 = matrix.m32;

                m_03 = matrix.m03;
                m_13 = matrix.m13;
                m_23 = matrix.m23;
                m_33 = matrix.m33;
            }

            /// <summary>
            /// Returns a single-precision matrix representation of this
            /// double-precision matrix.
            /// </summary>
            /// <returns>A single-precision matrix.</returns>
            public Matrix4x4 ToMatrix4x4()
            {
                return new Matrix4x4
                {
                    m00 = (float)m_00, m01 = (float)m_01, m02 = (float)m_02, m03 = (float)m_03,
                    m10 = (float)m_10, m11 = (float)m_11, m12 = (float)m_12, m13 = (float)m_13,
                    m20 = (float)m_20, m21 = (float)m_21, m22 = (float)m_22, m23 = (float)m_23,
                    m30 = (float)m_30, m31 = (float)m_31, m32 = (float)m_32, m33 = (float)m_33
                };
            }

            /// <summary>
            /// Returns a string representation of this matrix.
            /// </summary>
            /// <returns>A string.</returns>
            public override string ToString()
            {
                return string.Format("{0}\t{1}\t{2}\t{3}\n{4}\t{5}\t{6}\t{7}\n{8}\t{9}\t{10}\t{11}\n{12}\t{13}\t{14}\t{15}\n",
                    m_00, m_01, m_02, m_03,
                    m_10, m_11, m_12, m_13,
                    m_20, m_21, m_22, m_23,
                    m_30, m_31, m_32, m_33);
            }
        }

        /// <summary>
        /// A double-precision 3D vector.
        /// </summary>
        private struct DVector3
        {
            /// <summary>
            /// X component of this vector.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_x;

            /// <summary>
            /// Y component of this vector.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_y;

            /// <summary>
            /// Z component of this vector.
            /// </summary>
            [MarshalAs(UnmanagedType.R8)]
            public double m_z;

            /// <summary>
            /// Creates a new double-precision vector from the given
            /// single-precision vector.
            /// </summary>
            /// <param name="vector">A single-precision vector.</param>
            public DVector3(Vector3 vector)
            {
                m_x = vector.x;
                m_y = vector.y;
                m_z = vector.z;
            }

            /// <summary>
            /// Returns a single-precision vector representation of this
            /// double-precision vector.
            /// </summary>
            /// <returns>A single-precision vector.</returns>
            public Vector3 ToVector3()
            {
                return new Vector3((float)m_x, (float)m_y, (float)m_z);
            }

            /// <summary>
            /// Returns a string representation of this vector.
            /// </summary>
            /// <returns>A string.</returns>
            public override string ToString()
            {
                return string.Format("({0}, {1}, {2})", m_x, m_y, m_z);
            }
        }

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
            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_fitPlaneModelNearPointMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics cameraIntrinsics,
                ref DMatrix4x4 matrix, ref Vector2 uvCoordinates,
                out DVector3 intersectionPoint,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] double[] planeModel);

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_getDepthAtPointBilateralCameraIntrinsicsMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics cameraIntrinsics,
                TangoImageBuffer colorImage, ref DMatrix4x4 matrix,
                ref Vector2 uvCoordinates, out Vector3 colorCameraPoint,
                [Out, MarshalAs(UnmanagedType.I4)] out int isValidPoint);

            [DllImport(TANGO_SUPPORT_UNITY_DLL)]
            public static extern int TangoSupport_getDepthAtPointNearestNeighborMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics cameraIntrinsics,
                ref DMatrix4x4 matrix, ref Vector2 uvCoordinates,
                out Vector3 colorCameraPoint,
                [Out, MarshalAs(UnmanagedType.I4)] out int isValidPoint);
#else
            public static int TangoSupport_fitPlaneModelNearPointMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics cameraIntrinsics,
                ref DMatrix4x4 matrix, ref Vector2 uvCoordinates,
                out DVector3 intersectionPoint, double[] planeModel)
            {
                intersectionPoint = new DVector3();
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoSupport_getDepthAtPointBilateralCameraIntrinsicsMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics cameraIntrinsics,
                TangoImageBuffer colorImage, ref DMatrix4x4 matrix,
                ref Vector2 uvCoordinates, out Vector3 colorCameraPoint,
                out int isValidPoint)
            {
                colorCameraPoint = Vector3.zero;
                isValidPoint = 1;
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoSupport_getDepthAtPointNearestNeighborMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics cameraIntrinsics,
                ref DMatrix4x4 matrix, ref Vector2 uvCoordinates,
                out Vector3 colorCameraPoint, out int isValidPoint)
            {
                colorCameraPoint = Vector3.zero;
                isValidPoint = 1;
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
        #endregion
    }
}
