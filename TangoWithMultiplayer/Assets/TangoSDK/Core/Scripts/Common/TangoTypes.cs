//-----------------------------------------------------------------------
// <copyright file="TangoTypes.cs" company="Google">
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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
                                                         Justification = "Types file.")]

namespace Tango
{
    /// <summary>
    /// The TangoCoordinateFramePair struct contains a pair of coordinate frames of reference.
    ///
    /// Tango pose data is calculated as a transformation between two frames
    /// of reference (so, for example, you can be asking for the pose of the
    /// device within a learned area).
    ///
    /// This struct is used to specify the desired base and target frames of
    /// reference when requesting pose data.  You can also use it when you have
    /// a TangoPoseData structure returned from the API and want to examine which
    /// frames of reference were used to get that pose.
    ///
    /// For more information, including which coordinate frame pairs are valid,
    /// see our page on
    /// <a href ="/project-tango/overview/frames-of-reference">frames of reference</a>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TangoCoordinateFramePair
    {
        /// <summary>
        /// Base frame of reference to compare against when requesting pose data.
        /// For example, if you have loaded an area and want to find out where the
        /// device is within it, you would use the
        /// <code>TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION</code> frame of reference
        /// as your base.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoCoordinateFrameType baseFrame;

        /// <summary>
        /// Target frame of reference when requesting pose data, compared to the
        /// base. For example, if you want the device's pose data, use
        /// <code>TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE</code>.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoCoordinateFrameType targetFrame;
    }

    /// <summary>
    /// Variant of <c>TangoPointCloudData</c> that has no extra processing done on it.  Instead of having arrays, it has
    /// raw <c>IntPtr</c> fields.
    ///
    /// This is the exact struct that the C Tango API provides.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TangoPointCloudIntPtr
    {
        /// <summary>
        /// An integer denoting the version of the structure.
        /// </summary>
        public Int32 m_version;

        /// <summary>
        /// Time of capture of the point cloud (in seconds).
        /// </summary>
        public Double m_timestamp;

        /// <summary>
        /// The number of points in <c>points</c>.
        /// </summary>
        public Int32 m_numPoints;

        /// <summary>
        /// An array of XYZ,C float values.  XYZ is a coordinate in meters.  C is a confidence value in the range
        /// [0, 1], where 1 corresponds to full confidence.
        /// </summary>
        public IntPtr m_points;
    }

    /// <summary>
    /// The TangoImage contains information about a byte buffer holding image data.
    ///
    /// This data is populated by the service when it returns an image. This version supplies
    /// a pointer to the start of each image plane, as there may be padding between the planes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TangoImage
    {
        /// <summary>
        /// The width of the image data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 m_width;

        /// <summary>
        /// The height of the image data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 m_height;

        /// <summary>
        /// The pixel format of the data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoImageFormatType m_format;

        /// <summary>
        /// The timestamp of this image in nanoseconds.
        /// </summary>
        [MarshalAs(UnmanagedType.I8)]
        public Int64 m_timestampNs;

        /// <summary>
        /// Number of planes for the image format of this buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 m_numPlanes;

        /// <summary>
        /// Pixels in the first image plane.
        /// </summary>
        public IntPtr m_planeData0;

        /// <summary>
        /// Pixels in the second image plane.
        /// </summary>
        public IntPtr m_planeData1;

        /// <summary>
        /// Pixels in the third image plane.
        /// </summary>
        public IntPtr m_planeData2;

        /// <summary>
        /// Pixels in the fourth image plane.
        /// </summary>
        public IntPtr m_planeData3;

        /// <summary>
        /// Size of the first image plane for this buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeSize0;

        /// <summary>
        /// Size of the second image plane for this buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeSize1;

        /// <summary>
        /// Size of the third image plane for this buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeSize2;

        /// <summary>
        /// Size of the fourth image plane for this buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeSize3;

        /// <summary>
        /// Row strides for the first image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeRowStride0;

        /// <summary>
        /// Row strides for the second image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeRowStride1;

        /// <summary>
        /// Row strides for the third image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeRowStride2;

        /// <summary>
        /// Row strides for the fourth image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planeRowStride3;

        /// <summary>
        /// Pixel strides for the first image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planePpixelStride0;

        /// <summary>
        /// Pixel strides for the second image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planePixelStride1;

        /// <summary>
        /// Pixel strides for the third image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planePixelStride2;

        /// <summary>
        /// Pixel strides for the fourth image plane.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int m_planePixelStride3;
    }

    /// <summary>
    /// TangoCameraMetadata contains information about the image specific to the camera
    /// frame capture that produced it.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TangoCameraMetadata
    {
        /// <summary>
        /// Camera timestamp in nanoseconds.
        /// </summary>
        [MarshalAs(UnmanagedType.I8)]
        public Int64 m_timestampNs;

        /// <summary>
        /// Camera frame number.
        /// </summary>
        [MarshalAs(UnmanagedType.I8)]
        public Int64 m_frameNumber;

        /// <summary>
        /// Camer exposure time in nanoseconds.
        /// </summary>
        [MarshalAs(UnmanagedType.I8)]
        public Int64 m_exposureDurationNs;
    }

    /// <summary>
    /// DEPRECATED - Please use TangoImage instead.
    ///
    /// The TangoImageBuffer contains information about a byte buffer holding image data.
    ///
    /// This data is populated by the service when it returns an image.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TangoImageBuffer
    {
        /// <summary>
        /// The width of the image data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 width;

        /// <summary>
        /// The height of the image data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 height;

        /// <summary>
        /// The number of pixels per scanline of the image data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 stride;

        /// <summary>
        /// The timestamp of this image.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;

        /// <summary>
        /// The frame number of this image.
        /// </summary>
        [MarshalAs(UnmanagedType.I8)]
        public Int64 frame_number;

        /// <summary>
        /// The pixel format of the data.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoImageFormatType format;

        /// <summary>
        /// Pixels in the format of this image buffer.
        /// </summary>
        public IntPtr data;
    }

    /// <summary>
    /// DEPRECATED: A <c>TangoXYZij</c> object contains information returned from the depth sensor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoXYZij
    {
        /// <summary>
        /// An integer denoting the version of the structure.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int version;

        /// <summary>
        /// Time of capture of the depth data for this struct (in seconds).
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;

        /// <summary>
        /// The number of points in the xyz array.
        ///
        /// This is variable with result and is returned in (x,y,z) triplets populated (e.g. 2 points populated
        /// returned means 6 floats, or 6*4 bytes used).
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int xyz_count;

        /// <summary>
        /// An array of packed coordinate triplets, x,y,z as floating point values.
        ///
        /// With the unit in landscape orientation, screen facing the user: +Z points in the direction of the
        /// camera's optical axis, and is measured perpendicular to the plane of the camera. +X points toward the
        /// user's right, and +Y points toward the bottom of the screen. The origin is the focal centre of the color
        /// camera. The output is in units of meters.
        /// </summary>
        [MarshalAs(UnmanagedType.LPArray)]
        public IntPtr xyz;

        /// <summary>
        /// The dimensions of the <c>ij</c> index buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int ij_rows;

        /// <summary>
        /// The dimensions of the <c>ij</c> index buffer.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int ij_cols;

        /// <summary>
        /// A 2D buffer, of size <c>ij_rows</c> x <c>ij_cols</c> in raster ordering that contains the index of
        /// a point in the <c>xyz</c> array that was generated at this <c>ij</c> location.
        ///
        /// A value of -1 denotes there was no corresponding point generated at that position. This buffer can be used
        /// to find neighboring points in the point cloud.
        ///
        /// For more information, see our developer overview on depth perception .
        /// </summary>
        public IntPtr ij;

        /// <summary>
        /// TangoImageBuffer is reserved for future use.
        /// </summary>
        public IntPtr color_image;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Tango.TangoXYZij"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents the current <see cref="Tango.TangoXYZij"/>.
        /// </returns>
        public override string ToString()
        {
            return string.Format("timestamp : {0}\nxyz_count : {1}\nij_rows : {2}\nij_cols : {3}",
                                 timestamp, xyz_count, ij_rows, ij_cols);
        }
    }

    /// <summary>
    /// The TangoEvent structure signals important sensor and tracking events.
    ///
    /// Each event comes with a timestamp, a type, and a key-value pair describing
    /// the event.  The type is an enumeration which generally classifies the event
    /// type. The key is a text string describing the event.  The description holds
    /// parameters specific to the event.
    ///
    /// Possible descriptions (as "key:value") are:
    /// - "TangoServiceException:X" - The service has encountered an exception, and
    /// a text description is given in X.
    /// - "FisheyeOverExposed:X" - the fisheye image is over exposed with average
    /// pixel value X.
    /// - "FisheyeUnderExposed:X" - the fisheye image is under exposed with average
    /// pixel value X.
    /// - "ColorOverExposed:X" - the color image is over exposed with average pixel
    /// value X.
    /// - "ColorUnderExposed:X" - the color image is under exposed with average
    /// pixel value X.
    /// - "TooFewFeaturesTracked:X" - too few features were tracked in the fisheye
    /// image.  The number of features tracked is X.
    /// - "Unknown".
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoEvent
    {
        /// <summary>
        /// Timestamp, in seconds, of the event.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;

        /// <summary>
        /// Type of event.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoEventType type;

        /// <summary>
        /// Description of the event key.
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string event_key;

        /// <summary>
        /// Description of the event value.
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string event_value;
    }

    /// <summary>
    /// The TangoCameraIntrinsics struct contains intrinsic parameters for a camera.
    ///
    /// Given a 3D point (X, Y, Z) in camera coordinates, the corresponding
    /// pixel coordinates (x, y) are:
    ///
    /// <code>
    /// x = X / Z * fx * rd / ru + cx
    /// y = X / Z * fy * rd / ru + cy
    /// </code>
    ///
    /// The normalized radial distance <c>ru</c> is given by:
    ///
    /// <code>
    /// ru = sqrt((X^2 + Y^2) / (Z^2))
    /// </code>
    ///
    /// The distorted radial distance rd depends on the distortion model used.
    ///
    /// For <code>TangoCalibrationType.TANGO_CALIBRATION_POLYNOMIAL_3_PARAMETERS</code>, <c>rd</c> is a
    /// polynomial that depends on the 3 distortion coefficients k1, k2 and k3:
    ///
    /// <code>
    /// rd = ru + k1 * ru^3 + k2 * ru^5 + k3 * ru^7
    /// </code>
    ///
    /// For <code>TangoCalibrationType.TANGO_CALIBRATION_EQUIDISTANT</code>, <c>rd</c> depends on the single
    /// distortion coefficient w:
    ///
    /// <code>
    /// rd = 1 / w * arctan(2 * ru * tan(w / 2))
    /// </code>
    ///
    /// For more information, see our page on
    /// <a href ="/project-tango/overview/intrinsics-extrinsics">Camera Intrinsics and Extrinsics</a>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoCameraIntrinsics
    {
        /// <summary>
        /// ID of the camera which the intrinsics reference.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoCameraId camera_id;

        /// <summary>
        /// The type of distortion model used. This determines the meaning of the
        /// distortion coefficients.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoCalibrationType calibration_type;

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 width;

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public UInt32 height;

        /// <summary>
        /// Focal length, x axis, in pixels.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double fx;

        /// <summary>
        /// Focal length, y axis, in pixels.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double fy;

        /// <summary>
        /// Principal point x coordinate on the image, in pixels.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double cx;

        /// <summary>
        /// Principal point y coordinate on the image, in pixels.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double cy;

        /// <summary>
        /// Distortion coefficient 0.  Meaning of this value depends on the distortion model specified by
        /// <c>calibration_type</c>.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double distortion0;

        /// <summary>
        /// Distortion coefficient 1.  Meaning of this value depends on the distortion model specified by
        /// <c>calibration_type</c>.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double distortion1;

        /// <summary>
        /// Distortion coefficient 2.  Meaning of this value depends on the distortion model specified by
        /// <c>calibration_type</c>.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double distortion2;

        /// <summary>
        /// Distortion coefficient 3.  Meaning of this value depends on the distortion model specified by
        /// <c>calibration_type</c>.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double distortion3;

        /// <summary>
        /// Distortion coefficient 4.  Meaning of this value depends on the distortion model specified by
        /// <c>calibration_type</c>.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double distortion4;
    }

    /// <summary>
    /// The TangoPoseData struct contains 6DOF pose information.
    ///
    /// The device pose is given using Android conventions.  See the Android
    /// <a href ="http://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-coords">Sensor
    /// Overview</a> page for more information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoPoseData
    {
        /// <summary>
        /// An integer denoting the version of the structure.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int version;

        /// <summary>
        /// Timestamp of the time that this pose estimate corresponds to.
        /// </summary>
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;

        /// <summary>
        /// Orientation, as a quaternion, of the pose of the target frame
        /// with reference to the base frame.
        /// Specified as (x,y,z,w) where RotationAngle is in radians:
        /// <code>
        ///   x = RotationAxis.x * sin(RotationAngle / 2)
        ///   y = RotationAxis.y * sin(RotationAngle / 2)
        ///   z = RotationAxis.z * sin(RotationAngle / 2)
        ///   w = cos(RotationAngle / 2)
        /// </code>
        /// </summary>
        public DVector4 orientation;

        /// <summary>
        /// Translation, ordered x, y, z, of the pose of the target frame
        /// with reference to the base frame.
        /// </summary>
        public DVector3 translation;

        /// <summary>
        /// The status of the pose, according to the pose lifecycle.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoPoseStatusType status_code;

        /// <summary>
        /// The pair of coordinate frames for this pose.
        ///
        /// We retrieve a pose for a target coordinate frame (such as the Tango device) against a base
        /// coordinate frame (such as a learned area).
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public TangoCoordinateFramePair framePair;

        /// <summary>
        /// Unused.  Integer levels are determined by service.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int confidence;

        /// <summary>
        /// Unused.  Reserved for metric accuracy.
        /// </summary>
        [MarshalAs(UnmanagedType.R4)]
        public float accuracy;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.TangoPoseData"/> class.
        /// </summary>
        public TangoPoseData()
        {
            version = 0;
            timestamp = 0.0;
            orientation = new DVector4();
            translation = new DVector3();
            status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN;
            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            confidence = 0;
        }

        /// <summary>
        /// Deep copy from poseToCopy into this.
        /// </summary>
        /// <param name="poseToCopy">Pose to copy.</param>
        public void DeepCopy(TangoPoseData poseToCopy)
        {
            this.version = poseToCopy.version;
            this.timestamp = poseToCopy.timestamp;
            this.status_code = poseToCopy.status_code;
            this.framePair.baseFrame = poseToCopy.framePair.baseFrame;
            this.framePair.targetFrame = poseToCopy.framePair.targetFrame;
            this.confidence = poseToCopy.confidence;
            for (int i = 0; i < 4; ++i)
            {
                this.orientation[i] = poseToCopy.orientation[i];
            }

            for (int i = 0; i < 3; ++i)
            {
                this.translation[i] = poseToCopy.translation[i];
            }
        }

        /// <summary>
        /// Convert this pose into matrix form.
        /// </summary>
        /// <returns>Matrix4x4 that represents this pose.</returns>
        public Matrix4x4 ToMatrix4x4()
        {
            Vector3 pos = new Vector3((float)translation[0], (float)translation[1], (float)translation[2]);
            Quaternion rot = new Quaternion((float)orientation[0], (float)orientation[1], (float)orientation[2],
                                            (float)orientation[3]);
            return Matrix4x4.TRS(pos, rot, Vector3.one);
        }

        /// <summary>
        /// Convert Tango pose to string.
        /// </summary>
        /// <returns>Formatted string.</returns>
        public override string ToString()
        {
            return string.Format("Translation: {0}, {1}, {2}; Orientation: {0}, {1}, {2}, {3}",
                                 translation[0].ToString("F3"),
                                 translation[1].ToString("F3"),
                                 translation[2].ToString("F3"),
                                 orientation[0].ToString("F3"),
                                 orientation[1].ToString("F3"),
                                 orientation[2].ToString("F3"),
                                 orientation[3].ToString("F3"));
        }
    }

    /// <summary>
    /// The TangoUnityImageData contains information about a byte buffer holding image data.
    /// </summary>
    public class TangoUnityImageData
    {
        /// <summary>
        /// The width of the image data.
        /// </summary>
        public UInt32 width;

        /// <summary>
        /// The height of the image data.
        /// </summary>
        public UInt32 height;

        /// <summary>
        /// The number of pixels per scanline of the image data.
        /// </summary>
        public UInt32 stride;

        /// <summary>
        /// The timestamp of this image.
        /// </summary>
        public double timestamp;

        /// <summary>
        /// The frame number of this image.
        /// </summary>
        public Int64 frame_number;

        /// <summary>
        /// Pixel format of the data.
        /// </summary>
        public TangoEnums.TangoImageFormatType format;

        /// <summary>
        /// Pixels in the format of this image buffer.
        /// </summary>
        public byte[] data;
    }

    /// <summary>
    /// Point cloud from the depth sensor.  The <c>m_points</c> array is not fully filled out, there are only
    /// <c>m_numPoints</c> points stored there.
    /// </summary>
    public class TangoPointCloudData
    {
        /// <summary>
        /// Time of capture of the point cloud (in seconds).
        /// </summary>
        public double m_timestamp;

        /// <summary>
        /// The number of valid points in <c>m_points</c>.
        /// </summary>
        public int m_numPoints;

        /// <summary>
        /// An array of XYZ,C values.  If you want individual Vector3 objects, use the indexer.
        ///
        /// XYZ is a coordinate in meters.  C is a confidence value in the range [0, 1],
        /// where 1 corresponds to full confidence.
        /// </summary>
        public float[] m_points;

        /// <summary>
        /// Gets the point at the specified index as a Vector3.
        /// </summary>
        /// <param name="index">Index of the point.</param>
        /// <returns>Vector3 at the specified index.</returns>
        public Vector3 this[int index]
        {
            get
            {
                Vector3 val;
                val.x = m_points[(index * 4) + 0];
                val.y = m_points[(index * 4) + 1];
                val.z = m_points[(index * 4) + 2];
                return val;
            }
        }
    }

    /// <summary>
    /// DEPRECATED: Like <c>TangoXYZij</c>, but more Unity friendly.
    /// </summary>
    public class TangoUnityDepth
    {
        /// <summary>
        /// Max point array size is currently defined by the largest single mesh
        /// supported by Unity. This array is multiplied by 3 to account for the
        /// x/y/z components.
        /// </summary>
        public static readonly int MAX_POINTS_ARRAY_SIZE = Common.MAX_NUM_POINTS * 3;

        /// <summary>
        /// Max IJ array size is currently defined by the largest single mesh
        /// supported by Unity. This number is multiplied by 2 to account for the
        /// i/j components.
        /// </summary>
        public static readonly int MAX_IJ_ARRAY_SIZE = Common.MAX_NUM_POINTS * 2;

        /// <summary>
        ///
        /// An integer denoting the version of the structure.
        /// </summary>
        public int m_version;

        /// <summary>
        /// The number of points in the m_points array.
        ///
        /// Because the points array always contains 3D points, this is m_points.Count / 3.
        /// </summary>
        public int m_pointCount;

        /// <summary>
        /// An array of packed coordinate triplets, x,y,z as floating point values.
        /// </summary>
        public float[] m_points;

        /// <summary>
        /// Time of capture of the depth data for this struct (in seconds).
        /// </summary>
        public double m_timestamp;

        /// <summary>
        /// The dimensions of the <c>ij</c> index buffer.
        /// </summary>
        public int m_ijRows;

        /// <summary>
        /// The dimensions of the <c>ij</c> index buffer.
        /// </summary>
        public int m_ijColumns;

        /// <summary>
        /// A 2D buffer, of size <c>ij_rows</c> x <c>ij_cols</c> in raster ordering that contains
        /// the index of a point in the <c>xyz</c> array that was generated at this <c>ij</c>
        /// location.
        ///
        /// A value of -1 denotes there was no corresponding point generated at that position. This buffer can be used
        /// to find neighboring points in the point cloud.
        ///
        /// For more information, see our
        /// <a href ="/project-tango/overview/depth-perception#xyzij">developer
        /// overview on depth perception</a>.
        /// </summary>
        public int[] m_ij;

        /// <summary>
        /// Initializes an empty instance of the <see cref="Tango.TangoUnityDepth"/> class, with no points.
        /// </summary>
        public TangoUnityDepth()
        {
            m_points = new float[MAX_POINTS_ARRAY_SIZE];
            m_ij = new int[MAX_IJ_ARRAY_SIZE];
            m_version = -1;
            m_timestamp = 0.0;
            m_pointCount = m_ijRows = m_ijColumns = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.TangoUnityDepth"/> class from a
        /// <see cref="PointCloud"/> instance.
        /// </summary>
        /// <param name="pointCloud">Point cloud.</param>
        public TangoUnityDepth(TangoPointCloudData pointCloud)
        {
            m_points = new float[MAX_POINTS_ARRAY_SIZE];
            m_ij = new int[MAX_IJ_ARRAY_SIZE];

            m_timestamp = pointCloud.m_timestamp;
            m_pointCount = pointCloud.m_numPoints;
            for (int it = 0; it < pointCloud.m_points.Length / 4; ++it)
            {
                m_points[(it * 3) + 0] = pointCloud.m_points[(it * 4) + 0];
                m_points[(it * 3) + 1] = pointCloud.m_points[(it * 4) + 1];
                m_points[(it * 3) + 2] = pointCloud.m_points[(it * 4) + 2];
            }

            m_ijRows = m_ijColumns = 0;
            for (int it = 0; it < m_ij.Length; ++it)
            {
                m_ij[it] = -1;
            }
        }
    }
}