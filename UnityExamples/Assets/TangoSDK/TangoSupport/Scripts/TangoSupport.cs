//-----------------------------------------------------------------------
// <copyright file="TangoSupport.cs" company="Google">
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
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Contains the Project Tango Support Unity API. The Project Tango Support
    /// Unity API provides helper methods useful to external developers for
    /// manipulating Project Tango data. The Project Tango Support Unity API is
    /// experimental and subject to change.
    /// </summary>
    public class TangoSupport
    {
        /// <summary>
        /// Fits a plane to a point cloud near a user-specified location. This
        /// occurs in two passes. First, all points in cloud within
        /// maxPixelDistance to uvCoordinates after projection are kept. Then a
        /// plane is fit to the subset cloud using RANSAC. After the initial fit
        /// all inliers from the original cloud are used to refine the plane
        /// model.
        /// </summary>
        /// <returns>
        /// Common.ErrorType.TANGO_SUCCESS on success,
        /// Common.ErrorType.TANGO_INVALID on invalid input, and
        /// Common.ErrorType.TANGO_ERROR on failure.
        /// </returns>
        /// <param name="pointCloud">The input point cloud. Cannot be null and must have at least three points.</param>
        /// <param name="pointCount">Number of points to read from the point cloud.</param>
        /// <param name="timestamp">Timestamp of the depth points.</param>
        /// <param name="intrinsics">The camera intrinsics for the color camera.  Cannot be null.</param>
        /// <param name="matrix">Transformation matrix of the color camera with respect to the Unity World frame.</param>
        /// <param name="uvCoordinates">The UV coordinates for the user selection. This is expected to be between (0.0f, 0.0f) and (1.0f, 1.0f).</param>
        /// <param name="intersectionPoint">The output point in depth camera coordinates that the user selected.</param>
        /// <param name="plane">The the plane fit.</param>
        public static int FitPlaneModelNearClick(
            Vector3[] pointCloud, int pointCount, double timestamp, TangoCameraIntrinsics intrinsics,
            ref Matrix4x4 matrix, Vector2 uvCoordinates, out Vector3 intersectionPoint, out Plane plane)
        {
            GCHandle pointCloudHandle = GCHandle.Alloc(pointCloud, GCHandleType.Pinned);

            TangoXYZij pointCloudXyzIj = new TangoXYZij();
            pointCloudXyzIj.timestamp = timestamp;
            pointCloudXyzIj.xyz_count = pointCount;
            pointCloudXyzIj.xyz = pointCloudHandle.AddrOfPinnedObject();

            // Unity has Y pointing screen up; Tango camera has Y pointing screen down.
            float[] uvCoordinatesArray = new float[2];
            uvCoordinatesArray[0] = uvCoordinates.x;
            uvCoordinatesArray[1] = 1.0f - uvCoordinates.y;

            double[] intersectionPointArray = new double[3];
            double[] planeArray = new double[4];

            int returnValue = TangoSupportAPI.TangoSupport_fitPlaneModelNearClickMatrixTransform(
                pointCloudXyzIj, intrinsics, ref matrix,
                uvCoordinatesArray, intersectionPointArray, planeArray);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                intersectionPoint = new Vector3(0.0f, 0.0f, 0.0f);
                plane = new Plane(new Vector3(0.0f, 0.0f, 0.0f), 0.0f);
            }
            else
            {
                intersectionPoint = new Vector3((float)intersectionPointArray[0],
                                                (float)intersectionPointArray[1],
                                                (float)intersectionPointArray[2]);
                Vector3 normal = new Vector3((float)planeArray[0],
                                             (float)planeArray[1],
                                             (float)planeArray[2]);
                float distance = (float)planeArray[3] / normal.magnitude;

                plane = new Plane(normal, distance);
            }

            pointCloudHandle.Free();

            return returnValue;
        }

        #region API_Functions
        /// <summary>
        /// Name of the Tango Support C API shared library.
        /// </summary>
        internal const string TANGO_SUPPORT_UNITY_DLL = "tango_support_api";

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
            public static extern int TangoSupport_fitPlaneModelNearClickMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics intrinsics, ref Matrix4x4 matrix,
                [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] float[] uvCoordinates,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] double[] intersectionPoint,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] double[] planeModel);
#else
            public static int TangoSupport_fitPlaneModelNearClickMatrixTransform(
                TangoXYZij pointCloud, TangoCameraIntrinsics intrinsics, ref Matrix4x4 colorCameraTUnityWorld,
                float[] uvCoordinates, double[] intersectionPoint, double[] planeModel)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
        #endregion
    }
}
