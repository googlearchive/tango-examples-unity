/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Represents the ordered point cloud data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoXYZij
    {
        [MarshalAs(UnmanagedType.I4)]
        public int version;
        
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;
        
        [MarshalAs(UnmanagedType.I4)]
        public int xyz_count;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.SysUInt)]
        public IntPtr[] xyz;
        
        [MarshalAs(UnmanagedType.I4)]
        public int ij_rows;
        
        [MarshalAs(UnmanagedType.I4)]
        public int ij_cols;
        
        public IntPtr ij;

        // Reserved for future use.
        public IntPtr color_image;
        
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Tango.TangoXYZij"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Tango.TangoXYZij"/>.</returns>
        public override string ToString()
        {
            return ("timestamp : " + timestamp + "\n" +
                    "xyz_count : " + xyz_count + "\n" + 
                    "ij_rows : " + ij_rows + "\n" + 
                    "ij_cols : " + ij_cols);
        }
    }

    /// <summary>
    /// Tango event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoEvent
    {
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;
        
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoEventType type;
        
        [MarshalAs(UnmanagedType.LPStr)]
        public string event_key;

        [MarshalAs(UnmanagedType.LPStr)]
        public string event_value;
    }

    /// <summary>
    /// Tango coordinate frame pair.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TangoCoordinateFramePair
    {
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoCoordinateFrameType baseFrame;
        
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoCoordinateFrameType targetFrame;
    }

    /// <summary>
    /// Tango image buffer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoImageBuffer
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
        /// Pixels in HAL_PIXEL_FORMAT_YV12 format. Y samples of width x height are
        /// first, followed by V samples, with half the stride and half the lines of
        /// the Y data, followed by a U samples with the same dimensions as the V
        /// sample. This is stored in the API as a dynamic byte array (uint8_t*).
        /// </summary>
        public IntPtr data;
    }

    /// <summary>
    /// /// The TangoCameraIntrinsics struct contains intrinsic parameters for a camera.
    /// For image coordinates, the obervations, [u, v]^T in pixels.
    /// Normalized image plane coordinates refer to:
    ///
    /// x = (u - cx) / fx
    ///
    /// y = (v - cy) / fy
    ///
    /// Distortion model type is as given by calibration_type.  For example, for the
    /// color camera, TANGO_CALIBRATION_POLYNOMIAL_3_PARAMETERS means that the
    /// distortion parameters are in distortion[] as {k1, k2 ,k3} where
    ///
    /// x_corr_px = x_px (1 + k1 * r2 + k2 * r4 + k3 * r6)
    /// y_corr_px = y_px (1 + k1 * r2 + k2 * r4 + k3 * r6)
    ///
    /// where r2, r4, r6 are the 2nd, 4th, and 6th powers of the r, where r is the
    /// distance (normalized image plane coordinates) of (x,y) to (cx,cy), and
    /// for a pixel at point (x_px, y_px) in pixel coordinates, the corrected output
    /// position would be (x_corr, y_corr).
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
        /// Calibration model type that they distorion parameters reference.
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
    }
    
    /// <summary>
    /// Data representing a pose from the Tango Service.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TangoPoseData
    {
        [MarshalAs(UnmanagedType.I4)]
        public int version;
        
        [MarshalAs(UnmanagedType.R8)]
        public double timestamp;
        
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.R8)]
        public double[] orientation;
        
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R8)]
        public double[] translation;
        
        [MarshalAs(UnmanagedType.I4)]
        public TangoEnums.TangoPoseStatusType status_code;
        
        [MarshalAs(UnmanagedType.Struct)]
        public TangoCoordinateFramePair framePair;
        
        // Unused.  Integer levels are determined by service.
        [MarshalAs(UnmanagedType.I4)]
        public int confidence;

        // Unused.  Reserved for metric accuracy.
        [MarshalAs(UnmanagedType.R4)]
        public float accuracy;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.TangoPoseData"/> class.
        /// </summary>
        public TangoPoseData()
        {
            version = 0;
            timestamp = 0.0;
            orientation = new double[4];
            translation = new double[3];
            status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN;
            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            confidence = 0;
        }
    }

    /// <summary>
    /// Used in the Unity SDK to hold information about the UUID
    /// to avoid too many conversions when needing to access the information.
    /// </summary>
    public class UUIDUnityHolder
    {
        private UUID uuidObject;
        private string uuidName;
        public Metadata uuidMetaData;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.UUIDUnityHolder"/> class.
        /// </summary>
        public UUIDUnityHolder()
        {
            uuidObject = new UUID();
            uuidMetaData = new Metadata();
            uuidObject.data = IntPtr.Zero;
            uuidName = string.Empty;
        }

        /// <summary>
        /// Prepares the UUID meta data by the calling uuidMetaData object's 
        /// method - PopulateMetaDataKeyValues().
        /// </summary>
        public void PrepareUUIDMetaData()
        {
           
            uuidMetaData.PopulateMetaDataKeyValues();
        }

        /// <summary>
        /// Allocates memory for the IntPtr of the UUID data to be filled out.
        /// Uses Marshal.AllocHGlobal to initialize the IntPtr.
        /// </summary>
        public void AllocateDataBuffer()
        {
            uuidObject.data = Marshal.AllocHGlobal(Common.UUID_LENGTH);
        }

        /// <summary>
        /// Copies the data contained by <c>uuidData</c> into our UUID object
        /// data IntPtr.
        /// </summary>
        /// <param name="uuidData">The data marshalled by the UUID list object for this UUID object.</param>
        public void SetDataUUID(byte[] uuidData)
        {
            if(uuidObject.data == IntPtr.Zero)
            {
                AllocateDataBuffer();
            }
            Marshal.Copy(uuidData, 0, uuidObject.data, Common.UUID_LENGTH);
            SetDataUUID(System.Text.Encoding.UTF8.GetString(uuidData));
        }

        /// <summary>
        /// Copies the data contained by <c>uuidData</c> into our UUID object
        /// data IntPtr.
        /// </summary>
        /// <param name="uuidData">The UTF-8 encoded string for this UUID object.</param>
        public void SetDataUUID(string uuidString)
        {
            uuidName = uuidString;
        }

        /// <summary>
        /// Returns raw IntPtr to UUID data.
        /// </summary>
        /// <returns>The raw data UUID IntPtr.</returns>
        public IntPtr GetRawDataUUID()
        {
            return uuidObject.data;
        }

        /// <summary>
        /// Returns a human readable string in UTF-8 format of the UUID data.
        /// </summary>
        /// <returns>The UTF-8 string for the UUID.</returns>
        public string GetStringDataUUID()
        {
            return uuidName;
        }

        /// <summary>
        /// Determines whether or not the UUID object that we have is valid.
        /// </summary>
        /// <returns><c>true</c> if this instance contains a valid UUID object; otherwise, <c>false</c>.</returns>
        public bool IsObjectValid()
        {
            return uuidObject != null && (uuidObject.data != IntPtr.Zero || !string.IsNullOrEmpty(uuidName));
        }
    }

    /// <summary>
    /// Unique Identifier for an Area Description File.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class UUID
    {
        [MarshalAs(UnmanagedType.I4)]
        public IntPtr data;
    }

    /// <summary>
    /// List of all UUIDs on device.
    /// </summary>
    public class UUID_list
    {
        private UUIDUnityHolder[] UUIDs;
        private int count;

        /// <summary>
        /// Count of all Area Description Files (Read only).
        /// </summary>
        public int Count
        {
            get {return count;}
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.UUID_list"/> class.
        /// </summary>
        public UUID_list()
        {
            UUIDs = null;
        }
        
        /// <summary>
        /// Populates the UUID list.
        /// </summary>
        /// <param name="uuidNames">UUID names.</param>
        public void PopulateUUIDList(string uuidNames)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            string[] splitNames = uuidNames.Split(',');
            UUIDs = new UUIDUnityHolder[splitNames.Length];
            count = splitNames.Length;
            for(int i = 0; i < count; ++i)
            {
                if(UUIDs[i] == null)
                {
                    UUIDs[i] = new Tango.UUIDUnityHolder();
                }
                //Following three calls should be done in the same order always.
                UUIDs[i].SetDataUUID(System.Text.Encoding.UTF8.GetString(encoder.GetBytes(splitNames[i])));
                PoseProvider.GetAreaDescriptionMetaData(UUIDs[i]);
                UUIDs[i].PrepareUUIDMetaData();
            }
        }
        
        /// <summary>
        /// Returns the latest ADF UUID found in the list
        /// </summary>
        /// <returns>UUIDUnityHolder object that contains the last ADF saved.</returns>
        public UUIDUnityHolder GetLatestADFUUID()
        {
            if(UUIDs == null || (UUIDs != null && count <= 0))
            {
                return null;
            }
            return UUIDs[count - 1];
        }

        /// <summary>
        /// Query specific ADF.
        /// </summary>
        /// <returns>UUIDUnityHolder object that contains the last ADF saved.</returns>
        /// <param name="index">Index.</param>
        public UUIDUnityHolder GetADFAtIndex(int index)
        {
            if(UUIDs == null || (index < 0 || index >= count))
            {
                return null;
            }
            return UUIDs[index];
        }

        /// <summary>
        /// Gets the UUID as string.
        /// </summary>
        /// <returns>The UUID as string.</returns>
        /// <param name="index">Index.</param>
        public string GetUUIDAsString(int index)
        {
            if(UUIDs == null || (index < 0 || index >= count))
            {
                return null;
            }
            return UUIDs[index].GetStringDataUUID();
        }

        /// <summary>
        /// Determines whether this instance has valid UUID entries.
        /// </summary>
        /// <returns><c>true</c> if this instance has at least one or more UUIDs; otherwise, <c>false</c>.</returns>
        public bool HasEntries()
        {
            return count > 0;
        }
    }
    
    /// <summary>
    /// Metadata_entry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class Metadata
    {
        private System.Collections.Generic.Dictionary<string,string> m_KeyValuePairs = new System.Collections.Generic.Dictionary<string, string>();
        public IntPtr meta_data_pointer;
       
        /// <summary>
        /// Populates the meta data key values pairs.
        /// </summary>
        public void PopulateMetaDataKeyValues()
        {
            PoseProvider.PopulateAreaDescriptionMetaDataKeyValues(meta_data_pointer,ref m_KeyValuePairs);
        }

        /// <summary>
        /// Returns the dictionary object with the Metadata's Key Value pairs.
        /// PopulateMetaDataKeyValues() should be called before calling this.
        /// </summary>
        /// <returns>The meta data key values.</returns>
        public System.Collections.Generic.Dictionary<string,string> GetMetaDataKeyValues()
        {
            return m_KeyValuePairs;
        }

    }

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
        /// The pixel format of the data.
        /// </summary>
        public TangoEnums.TangoImageFormatType format;
        
        /// <summary>
        /// Pixels in HAL_PIXEL_FORMAT_YV12 format. Y samples of width x height are
        /// first, followed by V samples, with half the stride and half the lines of
        /// the Y data, followed by a U samples with the same dimensions as the V
        /// sample. This is stored in the API as a dynamic byte array (uint8_t*).
        /// </summary>
        public byte[] data;
    }
  
    /// <summary>
    /// Tango depth that is more Unity friendly.
    /// </summary>
    public class TangoUnityDepth
    {
        public int m_version;
        public int m_pointCount;
        public Vector3[] m_vertices;
        public double m_timestamp;
        public int m_ijRows;
        public int m_ijColumns;
        public Vector2[] m_ij;

        public TangoUnityDepth()
        {
            m_vertices = new Vector3[61440];
            m_ij = new Vector2[61440];
            m_version = -1;
            m_timestamp = 0.0;
            m_pointCount = m_ijRows = m_ijColumns = 0;
        }
    }
}