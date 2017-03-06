//-----------------------------------------------------------------------
// <copyright file="AreaDescription.cs" company="Google">
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
    using System.Runtime.InteropServices;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// C API wrapper for the Tango area description interface.
    /// </summary>
    public sealed class AreaDescription
    {
        /// <summary>
        /// The UUID for this area description.
        /// </summary>
        public readonly string m_uuid;

        /// <summary>
        /// Byte count of an <c>Int64</c>.  What the C sizeof operator would return.
        /// </summary>
        private const int INT64_BYTE_COUNT = 8;

        /// <summary>
        /// Byte count of a double.  What the C sizeof operator would return.
        /// </summary>
        private const int DOUBLE_BYTE_COUNT = 8;

        /// <summary>
        /// Conversion factor between DateTime ticks and milliseconds.
        /// </summary>
        private const int DATETIME_TICKS_PER_MS = 10000;

#if UNITY_EDITOR
        /// <summary>
        /// Extension used to identify the fake area description files
        /// used to aid in area description interface tests in the Editor.
        ///
        /// Functions as a sanity check to avoid trying to load
        /// miscellaneous files (e.g. generated .DS_Store files on Macs).
        /// </summary>
        private const string EMULATED_ADF_EXTENSION = ".ead";

        /// <summary>
        /// Path to where the fake area description files useed to aid in
        /// area description interface tests in the Editor are to be kept.
        /// </summary>
        private static string EMULATED_ADF_SAVE_PATH;
#endif

        /// <summary>
        /// The date-time epoch for Tango Metadata.
        ///
        /// This is the same as the Unix epoch, 00:00:00 UTC on January 1st, 1970.
        /// </summary>
        private static readonly DateTime METADATA_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

#if UNITY_EDITOR
        /// <summary>
        /// Xml serializer used for reading and writing metadata
        /// for fake area descriptions in the Editor.
        /// </summary>
        private static System.Xml.Serialization.XmlSerializer metadataXmlSerializer =
            new System.Xml.Serialization.XmlSerializer(typeof(Metadata));
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.AreaDescription"/> class.
        ///
        /// Private to make sure people use <c>AreaDescription.ForUUID</c>
        /// </summary>
        /// <param name="uuid">UUID string for an Area Description.</param>
        private AreaDescription(string uuid)
        {
            m_uuid = uuid;
        }

        /// <summary>
        /// Get an area description by UUID.
        /// </summary>
        /// <returns>An area description for that UUID.  If no such area description exists, returns null.</returns>
        /// <param name="uuid">UUID to find.</param>
        public static AreaDescription ForUUID(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                Debug.Log("No UUID specified.\n" + Environment.StackTrace);
                return null;
            }

            string[] uuids = _GetUUIDList();
            if (uuids != null && !Array.Exists(uuids, p => p == uuid))
            {
                Debug.Log(string.Format("Could not find Area Description for uuid {0}\n" + Environment.StackTrace,
                                        uuid));
                return null;
            }

            return new AreaDescription(uuid);
        }

        /// <summary>
        /// Get a list of all the area descriptions on the device.
        /// </summary>
        /// <returns>A list of area descriptions, or <c>null</c> if the list could not be queried.</returns>
        public static AreaDescription[] GetList()
        {
            string[] uuids = _GetUUIDList();
            if (uuids == null || uuids.Length == 0)
            {
                return null;
            }

            AreaDescription[] adfs = new AreaDescription[uuids.Length];
            for (int it = 0; it < uuids.Length; ++it)
            {
                adfs[it] = new AreaDescription(uuids[it]);
            }

            return adfs;
        }

        /// <summary>
        /// Saves the current area description, returning the area description saved.
        ///
        /// You can only save an area description while connected to the Tango Service and if you have enabled Area
        /// Learning mode. If you loaded an area description before connecting, then calling this method appends any
        /// new learned areas to that area description and returns an area description with the same UUID. If you did
        /// not load an area description, this method creates a new area description and a new UUID for that area
        /// description.
        /// </summary>
        /// <returns>
        /// AreaDescription instance for the newly saved area description if saved successfully, <c>null</c> otherwise.
        ///
        /// See logcat for details of why a saved failed.
        /// </returns>
        public static AreaDescription SaveCurrent()
        {
#if UNITY_EDITOR
            if (!EmulatedAreaDescriptionHelper.m_usingEmulatedDescriptionFrames)
            {
                Debug.LogError("Error in Area Description save emulation:\nNo current emulated area description.");
                return null;
            }

            // If we don't have an existing UUID, this is a 'new' Area Description, so we'll have to create it.
            if (string.IsNullOrEmpty(EmulatedAreaDescriptionHelper.m_currentUUID))
            {
                // Just use a .net GUID for the UUID.
                string uuid = Guid.NewGuid().ToString();

                EmulatedAreaDescriptionHelper.m_currentUUID = uuid;

                try
                {
                    Directory.CreateDirectory(EMULATED_ADF_SAVE_PATH);

                    using (StreamWriter streamWriter =
                           new StreamWriter(File.Open(EMULATED_ADF_SAVE_PATH + uuid + EMULATED_ADF_EXTENSION,
                                                      FileMode.Create)))
                    {
                        Metadata metadata = new Metadata();
                        metadata.m_name = "Unnamed";
                        metadata.m_dateTime = DateTime.Now;
                        metadata.m_transformationPosition = new double[3];
                        metadata.m_transformationRotation = new double[] { 0, 0, 0, 1 };
                        metadataXmlSerializer.Serialize(streamWriter, metadata);
                    }
                }
                catch (IOException ioException)
                {
                    Debug.LogError("IO error in Area Description save/load emulation:\n"
                                   + ioException.Message);
                    return null;
                }

                return AreaDescription.ForUUID(uuid);
            }
            else
            {
                // Since we don't actually save any description of the area in emulation,
                // if we're using an existing UUID, we don't have to do anything but return it.
                return AreaDescription.ForUUID(EmulatedAreaDescriptionHelper.m_currentUUID);
            }
#else
            byte[] rawUUID = new byte[Common.UUID_LENGTH];
            if (AreaDescriptionAPI.TangoService_saveAreaDescription(rawUUID) != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Could not save area description.\n" + Environment.StackTrace);
                return null;
            }

            // Don't want to include the null terminator in the C# string.
            string uuid = Encoding.UTF8.GetString(rawUUID, 0, Common.UUID_LENGTH - 1);

            return AreaDescription.ForUUID(uuid);
#endif
        }

        /// <summary>
        /// Import an area description from a file path to the default area storage location.
        /// </summary>
        /// <returns><c>true</c> if the area description was imported successfully, <c>false</c> otherwise.</returns>
        /// <param name="filePath">File path of the area description to be imported.</param>
        public static bool ImportFromFile(string filePath)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Area description import and export are not supported in Unity Editor"
                             + " because editor area description files are all but meaningless"
                             + " (and mostly exist for the sole purpose of faster UI testing).");
            return false;
#else
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("No file path specified.\n" + Environment.StackTrace);
                return false;
            }

            AndroidHelper.StartImportADFActivity(filePath);
            return true;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Generates the emulated save path -- called on the main unity thread.
        /// </summary>
        public static void GenerateEmulatedSavePath()
        {
            EMULATED_ADF_SAVE_PATH = UnityEngine.Application.persistentDataPath
                + "/TangoEmulation/AreaDescriptions/";
        }
#endif

        /// <summary>
        /// Export an area description from the default area storage location to the destination file directory.
        ///
        /// The exported file will use the UUID as its file name.
        /// </summary>
        /// <returns>Returns <c>true</c> if the file was exported, or <c>false</c> if the export failed.</returns>
        /// <param name="filePath">Destination file directory.</param>
        public bool ExportToFile(string filePath)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Area description import and export are not supported in Unity Editor"
                             + " because editor area description files are all but meaningless"
                             + " (and mostly exist for the sole purpose of faster UI testing).");
            return false;
#else
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("No file path specified.\n" + Environment.StackTrace);
                return false;
            }

            AndroidHelper.StartExportADFActivity(m_uuid, filePath);

            return true;
#endif
        }

        /// <summary>
        /// Delete an area description.
        /// </summary>
        /// <returns>
        /// Returns true if the area description file is found and can be removed.
        /// </returns>
        public bool Delete()
        {
#if UNITY_EDITOR
            try
            {
                if (File.Exists(EMULATED_ADF_SAVE_PATH + m_uuid + EMULATED_ADF_EXTENSION))
                {
                    File.Delete(EMULATED_ADF_SAVE_PATH + m_uuid + EMULATED_ADF_EXTENSION);
                    return true;
                }
            }
            catch (IOException ioException)
            {
                Debug.LogError("IO error in Area Description save/load emulation:\n"
                               + ioException.Message);
            }

            return false;
#else
            int returnValue = AreaDescriptionAPI.TangoService_deleteAreaDescription(m_uuid);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Could not delete area description.\n" + Environment.StackTrace);
                return false;
            }

            return true;
#endif
        }

        /// <summary>
        /// Get the metadata for the Area Description.
        ///
        /// If you want to create a metadata for a not yet saved Area Description, you should instead use the default
        /// constructor.
        /// </summary>
        /// <returns>The metadata, or <c>null</c> if that Area Description does not exist.</returns>
        public Metadata GetMetadata()
        {
#if UNITY_EDITOR
            Metadata metadata = new Metadata();

            try
            {
                if (!File.Exists(EMULATED_ADF_SAVE_PATH + m_uuid + EMULATED_ADF_EXTENSION))
                {
                    return null;
                }

                using (StreamReader streamReader =
                       new StreamReader(File.OpenRead(EMULATED_ADF_SAVE_PATH + m_uuid + EMULATED_ADF_EXTENSION)))
                {
                    metadata = (Metadata)metadataXmlSerializer.Deserialize(streamReader);
                }
            }
            catch (IOException ioException)
            {
                Debug.LogError("IO error in Area Description save/load emulation:\n"
                               + ioException.Message);
                return null;
            }
            catch (System.Xml.XmlException xmlException)
            {
                Debug.LogError("XML error in Area Description save/load emulation"
                               + " (corrupt file contents?); returning blank metadata (for file: "
                               + EMULATED_ADF_SAVE_PATH + m_uuid + EMULATED_ADF_EXTENSION + ")\n"
                               + "Error: " + xmlException.Message);
            }

            // Make sure there are no fields left atypically uninitialized
            // in emulation even if some fields are missing from or corrupted in XML:
            if (metadata.m_name == null)
            {
                metadata.m_name = string.Empty;
            }

            if (metadata.m_transformationPosition == null)
            {
                metadata.m_transformationPosition = new double[3];
            }

            if (metadata.m_transformationRotation == null)
            {
                metadata.m_transformationRotation = new double[] { 0, 0, 0, 1 };
            }

            return metadata;
#else
            IntPtr rawMetadata = _GetMetadataPtr();
            if (rawMetadata == IntPtr.Zero)
            {
                return null;
            }

            Metadata newData = new Metadata();
            if (!_MetadataGetString(rawMetadata, "name", out newData.m_name))
            {
                newData.m_name = string.Empty;
            }

            Int64 dateMSSinceEpoch;
            if (!_MetadataGetInt64(rawMetadata, "date_ms_since_epoch", out dateMSSinceEpoch))
            {
                dateMSSinceEpoch = 0;
            }

            newData.m_dateTime = new DateTime(METADATA_EPOCH.Ticks + (dateMSSinceEpoch * DATETIME_TICKS_PER_MS));

            double[] tform;
            if (!_MetadataGetDoubleArray(rawMetadata, "transformation", 7, out tform))
            {
                tform[0] = tform[1] = tform[2] = tform[3] = tform[4] = tform[5] = 0;
                tform[6] = 1;
            }

            newData.m_transformationPosition = new double[3] { tform[0], tform[1], tform[2] };
            newData.m_transformationRotation = new double[4] { tform[3], tform[4], tform[5], tform[6] };

            _FreeMetadataPtr(rawMetadata);
            return newData;
#endif
        }

        /// <summary>
        /// Save the metadata for this area description.
        /// </summary>
        /// <returns>Returns <c>true</c> if the metadata was successfully saved, <c>false</c> otherwise.</returns>
        /// <param name="metadata">Metadata to save.</param>
        public bool SaveMetadata(Metadata metadata)
        {
#if UNITY_EDITOR
            try
            {
                using (StreamWriter streamWriter =
                       new StreamWriter(File.Open(EMULATED_ADF_SAVE_PATH + m_uuid + EMULATED_ADF_EXTENSION,
                                                  FileMode.Create)))
                {
                    metadataXmlSerializer.Serialize(streamWriter, metadata);
                }
            }
            catch (IOException ioException)
            {
                Debug.LogError("IO error in Area Description save/load emulation:\n"
                               + ioException.Message);
                return false;
            }

            return true;
#else
            IntPtr rawMetadata = _GetMetadataPtr();
            if (rawMetadata == IntPtr.Zero)
            {
                return false;
            }

            bool anyErrors = false;
            if (!_MetadataSetString(rawMetadata, "name", metadata.m_name))
            {
                anyErrors = true;
            }

            if (!_MetadataSetInt64(rawMetadata, "date_ms_since_epoch",
                                   (metadata.m_dateTime.Ticks - METADATA_EPOCH.Ticks) / DATETIME_TICKS_PER_MS))
            {
                anyErrors = true;
            }

            double[] tform = new double[7];
            tform[0] = metadata.m_transformationPosition[0];
            tform[1] = metadata.m_transformationPosition[1];
            tform[2] = metadata.m_transformationPosition[2];
            tform[3] = metadata.m_transformationRotation[0];
            tform[4] = metadata.m_transformationRotation[1];
            tform[5] = metadata.m_transformationRotation[2];
            tform[6] = metadata.m_transformationRotation[3];
            if (!_MetadataSetDoubleArray(rawMetadata, "transformation", tform))
            {
                anyErrors = true;
            }

            if (!anyErrors)
            {
                int returnValue = AreaDescriptionAPI.TangoService_saveAreaDescriptionMetadata(m_uuid, rawMetadata);
                if (returnValue != Common.ErrorType.TANGO_SUCCESS)
                {
                    Debug.Log("Could not save metadata values.\n" + Environment.StackTrace);
                    anyErrors = true;
                }
            }

            _FreeMetadataPtr(rawMetadata);
            return !anyErrors;
#endif
        }

        /// <summary>
        /// Get a list of all area description UUIDs on the device.
        /// </summary>
        /// <returns>List of string UUIDs.</returns>
        private static string[] _GetUUIDList()
        {
#if UNITY_EDITOR
            try
            {
                DirectoryInfo directory = new DirectoryInfo(EMULATED_ADF_SAVE_PATH);
                if (directory.Exists)
                {
                    FileInfo[] fileInfo = directory.GetFiles();
                    List<string> uuids = new List<String>();
                    for (int i = 0; i < fileInfo.Length; i++)
                    {
                        if (fileInfo[i].Extension == EMULATED_ADF_EXTENSION)
                        {
                            uuids.Add(Path.GetFileNameWithoutExtension(fileInfo[i].Name));
                        }
                    }

                    return uuids.ToArray();
                }
            }
            catch (IOException ioException)
            {
                Debug.LogError("IO error in Area Description save/load emulation:\n"
                               + ioException.Message);
            }

            return new string[0];
#else
            IntPtr rawListString = IntPtr.Zero;
            int returnValue = AreaDescriptionAPI.TangoService_getAreaDescriptionUUIDList(ref rawListString);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Could not get ADF list from device.\n" + Environment.StackTrace);
                return null;
            }

            string listString = _ReadUTF8String(rawListString);
            return listString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
#endif
        }

        /// <summary>
        /// Free a previously gotten Metadata pointer.
        /// </summary>
        /// <param name="metadataPtr">Metadata pointer to free.</param>
        private static void _FreeMetadataPtr(IntPtr metadataPtr)
        {
            if (metadataPtr != IntPtr.Zero)
            {
                AreaDescriptionAPI.TangoAreaDescriptionMetadata_free(metadataPtr);
            }
        }

        /// <summary>
        /// Get a string value from an area description metadata.
        /// </summary>
        /// <returns><c>true</c>, if a string value was found, <c>false</c> otherwise.</returns>
        /// <param name="rawMetadata">Area description metadata pointer.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">Output value for the specified key.</param>
        private static bool _MetadataGetString(IntPtr rawMetadata, string key, out string value)
        {
            value = String.Empty;

            IntPtr rawValue = IntPtr.Zero;
            uint rawValueSize = 0;
            if (AreaDescriptionAPI.TangoAreaDescriptionMetadata_get(rawMetadata, key, ref rawValueSize, ref rawValue)
                != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("key={0} Unable to get metadata value.\n" + Environment.StackTrace,
                                        key));
                return false;
            }

            byte[] rawValueArray = new byte[rawValueSize];
            Marshal.Copy(rawValue, rawValueArray, 0, (int)rawValueSize);
            try
            {
                value = Encoding.UTF8.GetString(rawValueArray);
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("key={0} Error during UTF-8 decoding {1}.\n" + Environment.StackTrace,
                                        key, e.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set a string value in an area description metadata.
        /// </summary>
        /// <returns><c>true</c>, if string value was set, <c>false</c> otherwise.</returns>
        /// <param name="rawMetadata">Area description metadata pointer.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">New value.</param>
        private static bool _MetadataSetString(IntPtr rawMetadata, string key, string value)
        {
            if (AreaDescriptionAPI.TangoAreaDescriptionMetadata_set(rawMetadata, key,
                                                                    (uint)Encoding.UTF8.GetByteCount(value), value)
                != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("key={0}, value={1} Unable to set metadata value.\n" + Environment.StackTrace,
                                        key, value));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get an <c>Int64</c> value from an area description metadata.
        /// </summary>
        /// <returns><c>true</c>, if an <c>Int64</c> value was found, <c>false</c> otherwise.</returns>
        /// <param name="rawMetadata">Area description metadata pointer.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">Output value for the specified key.</param>
        private static bool _MetadataGetInt64(IntPtr rawMetadata, string key, out Int64 value)
        {
            value = 0;

            IntPtr rawValue = IntPtr.Zero;
            uint rawValueSize = 0;
            if (AreaDescriptionAPI.TangoAreaDescriptionMetadata_get(rawMetadata, key, ref rawValueSize, ref rawValue)
                != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("key={0} Unable to get metadata value.\n" + Environment.StackTrace,
                                        key));
                return false;
            }

            if (rawValueSize != INT64_BYTE_COUNT)
            {
                // Unexpected size change
                Debug.Log(string.Format("key={0} Unexpected byte size {1}.\n" + Environment.StackTrace,
                                        key, rawValueSize));
                return false;
            }

            value = Marshal.ReadInt64(rawValue);
            return true;
        }

        /// <summary>
        /// Set an <c>Int64</c> value in an area description metadata.
        /// </summary>
        /// <returns><c>true</c>, if <c>Int64</c> value was set, <c>false</c> otherwise.</returns>
        /// <param name="rawMetadata">Area description metadata pointer.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">New value.</param>
        private static bool _MetadataSetInt64(IntPtr rawMetadata, string key, Int64 value)
        {
            if (AreaDescriptionAPI.TangoAreaDescriptionMetadata_set(rawMetadata, key, INT64_BYTE_COUNT, ref value)
                != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("key={0}, value={1} Unable to set metadata value.\n" + Environment.StackTrace,
                                        key, value));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a fixed-size double array from an area description metadata.
        /// </summary>
        /// <returns><c>true</c>, if a double array of the specified size was found, <c>false</c> otherwise.</returns>
        /// <param name="rawMetadata">Area description metadata pointer.</param>
        /// <param name="key">Key name.</param>
        /// <param name="expectedCount">Expected size of the array.</param>
        /// <param name="value">Output value for the specified key.</param>
        private static bool _MetadataGetDoubleArray(IntPtr rawMetadata, string key, int expectedCount,
                                                   out double[] value)
        {
            value = new double[expectedCount];

            IntPtr rawValue = IntPtr.Zero;
            uint rawValueSize = 0;
            if (AreaDescriptionAPI.TangoAreaDescriptionMetadata_get(rawMetadata, key, ref rawValueSize, ref rawValue)
                != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("key={0} Unable to get metadata value.\n" + Environment.StackTrace,
                                        value));
                return false;
            }

            if (rawValueSize != DOUBLE_BYTE_COUNT * expectedCount)
            {
                // Unexpected size change
                Debug.Log(string.Format("key={0} Unexpected byte size {1}.\n" + Environment.StackTrace,
                                        key, rawValueSize));
                return false;
            }

            Marshal.Copy(rawValue, value, 0, expectedCount);
            return true;
        }

        /// <summary>
        /// Set a fixed-size double array in an area description metadata.
        /// </summary>
        /// <returns><c>true</c>, if the double array was set, <c>false</c> otherwise.</returns>
        /// <param name="rawMetadata">Area description metadata pointer.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">New value.</param>
        private static bool _MetadataSetDoubleArray(IntPtr rawMetadata, string key, double[] value)
        {
            uint arrayByteCount = (uint)(DOUBLE_BYTE_COUNT * value.Length);
            if (AreaDescriptionAPI.TangoAreaDescriptionMetadata_set(rawMetadata, key, arrayByteCount, value)
                != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("key={0}, value={1} Unable to set metadata value.\n" + Environment.StackTrace,
                                        key, value));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert an <c>IntPtr</c> to a UTF-8 encoded C-string into a C# string.
        /// </summary>
        /// <returns>The string object.</returns>
        /// <param name="rawPtr">Pointer to a UTF-8 encoded C-string.</param>
        private static string _ReadUTF8String(IntPtr rawPtr)
        {
            int len = 0;
            while (Marshal.ReadByte(rawPtr, len) != 0)
            {
                ++len;
            }

            byte[] stringBytes = new byte[len];
            Marshal.Copy(rawPtr, stringBytes, 0, len);

            return Encoding.UTF8.GetString(stringBytes);
        }

        /// <summary>
        /// Get the low level metadata pointer for an area description.
        ///
        /// Make sure to free this with <c>_FreeMetadataPtr</c> when you are done with it.
        /// </summary>
        /// <returns>A metadata pointer or <c>IntPtr.Zero</c> if it could not be loaded.</returns>
        private IntPtr _GetMetadataPtr()
        {
            IntPtr value = IntPtr.Zero;
            int returnValue = AreaDescriptionAPI.TangoService_getAreaDescriptionMetadata(m_uuid, ref value);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(string.Format("Could not get metadata pointer for uuid {0}.\n" + Environment.StackTrace,
                                        m_uuid));
                return IntPtr.Zero;
            }

            return value;
        }

        /// <summary>
        /// Easy access to the metadata fields for an Area Description.
        ///
        /// If you want to look at a specific Area Description's metadata, get it with
        /// <c>AreaDescription.GetMetadata</c> and save it with <c>AreaDescription.SaveMetadata</c>.
        ///
        /// If you want to create a metadata for a not yet saved Area Description, use the default constructor to construct
        /// an empty metadata and save it after saving the Area Description.
        /// </summary>
        public sealed class Metadata
        {
            /// <summary>
            /// The human-readable name for this Area Description.
            ///
            /// Corresponds to the "name" metadata.
            /// </summary>
            public string m_name;

            /// <summary>
            /// The creation date of this Area Description.
            ///
            /// Corresponds to the "date_ms_since_epoch" metadata.
            /// </summary>
            public DateTime m_dateTime;

            /// <summary>
            /// The global coordinate system position of this Area Description.
            ///
            /// Corresponds to the X, Y, Z part of the "transformation" metadata.
            /// </summary>
            public double[] m_transformationPosition;

            /// <summary>
            /// The global coordinate system rotation of this Area Description.
            ///
            /// Corresponds to the QX, QY, QZ, QW part of the "transformation" metadata.
            /// </summary>
            public double[] m_transformationRotation;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private class AreaDescriptionAPI
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_getAreaDescriptionMetadata(string uuid, ref IntPtr metadata);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_getAreaDescriptionUUIDList(ref IntPtr uuid_list);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_saveAreaDescription(byte[] rawUUID);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_saveAreaDescriptionMetadata(
                [MarshalAs(UnmanagedType.LPStr)] string uuid, IntPtr metadata);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_deleteAreaDescription([MarshalAs(UnmanagedType.LPStr)] string uuid);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoAreaDescriptionMetadata_free(IntPtr metadata);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoAreaDescriptionMetadata_get(IntPtr metadata,
                                                                      [MarshalAs(UnmanagedType.LPStr)] string key,
                                                                      ref UInt32 value_size, ref IntPtr value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoAreaDescriptionMetadata_set(IntPtr metadata,
                                                                      [MarshalAs(UnmanagedType.LPStr)] string key,
                                                                      UInt32 value_size,
                                                                      [MarshalAs(UnmanagedType.LPStr)] string value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoAreaDescriptionMetadata_set(IntPtr metadata,
                                                                      [MarshalAs(UnmanagedType.LPStr)] string key,
                                                                      UInt32 value_size, ref Int64 value);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoAreaDescriptionMetadata_set(IntPtr metadata,
                                                                      [MarshalAs(UnmanagedType.LPStr)] string key,
                                                                      UInt32 value_size, double[] value);
#else
            public static int TangoService_getAreaDescriptionMetadata(string uuid, ref IntPtr metadata)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_getAreaDescriptionUUIDList(ref IntPtr uuid_list)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_saveAreaDescription(byte[] rawUUID)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_deleteAreaDescription(string uuid)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_saveAreaDescriptionMetadata(string uuid, IntPtr metadata)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoAreaDescriptionMetadata_free(IntPtr metadata)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoAreaDescriptionMetadata_get(IntPtr metadata, string key, ref UInt32 value_size,
                                                               ref IntPtr value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoAreaDescriptionMetadata_set(IntPtr metadata, string key, UInt32 value_size,
                                                               string value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoAreaDescriptionMetadata_set(IntPtr metadata, string key, UInt32 value_size,
                                                                    ref Int64 value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoAreaDescriptionMetadata_set(IntPtr metadata, string key, UInt32 value_size,
                                                               double[] value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
    }
}
