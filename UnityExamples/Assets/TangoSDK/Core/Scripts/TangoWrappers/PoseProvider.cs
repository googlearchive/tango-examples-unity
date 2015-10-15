//-----------------------------------------------------------------------
// <copyright file="PoseProvider.cs" company="Google">
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// C API wrapper for the Tango pose interface.
    /// </summary>
    public class PoseProvider
    {
        /// <summary>
        /// Tango pose C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="pose">Pose data.</param> 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onPoseAvailable(IntPtr callbackContext, [In, Out] TangoPoseData pose);
        
        private const float MOUSE_LOOK_SENSITIVITY = 100.0f;
        private const float TRANSLATION_SPEED = 2.0f;
        private static readonly string CLASS_NAME = "PoseProvider";

        // Keeps track of all the ADFs on the device.
        private static UUID_list m_adfList = new UUID_list();

#if UNITY_EDITOR
        /// <summary>
        /// The emulated pose position.  Used for Tango emulation on PC.
        /// </summary>
        private static Vector3 m_emulatedPosePosition;
        
        /// <summary>
        /// The emulated pose euler angles from forward.  Used for Tango emulation on PC.
        /// 
        /// This is not the pure rotation for Tango, when it is Identity, you are facing forward, not down.
        /// </summary>
        private static Vector3 m_emulatedPoseAnglesFromForward;
#endif

        /// <summary>
        /// Get a pose at a given timestamp from the base to the target frame.
        /// 
        /// All poses returned are marked as TANGO_POSE_VALID (in the status_code field on TangoPoseData ) even if
        /// they were marked as TANGO_POSE_INITIALIZING in the callback poses.
        /// 
        /// If no pose can be returned, the status_code of the returned pose will be TANGO_POSE_INVALID.
        /// </summary>
        /// <param name="poseData">The pose to return.</param>
        /// <param name="timeStamp">
        /// Time specified in seconds.
        /// 
        /// If not set to 0.0, GetPoseAtTime retrieves the interpolated pose closest to this timestamp. If set to 0.0,
        /// the most recent pose estimate for the target-base pair is returned. The time of the returned pose is
        /// contained in the pose output structure and may differ from the queried timestamp.
        /// </param>
        /// <param name="framePair">
        /// A pair of coordinate frames specifying the transformation to be queried for.
        /// 
        /// For example, typical device motion is given by a target frame of TANGO_COORDINATE_FRAME_DEVICE and a base
        /// frame of TANGO_COORDINATE_FRAME_START_OF_SERVICE .
        /// </param>
        public static void GetPoseAtTime([In, Out] TangoPoseData poseData, 
                                         double timeStamp, 
                                         TangoCoordinateFramePair framePair)
        {
            int returnValue = PoseProviderAPI.TangoService_getPoseAtTime(timeStamp, framePair, poseData);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".GetPoseAtTime() Could not get pose at time : " + timeStamp);
            }
        }
        
        /// <summary>
        /// Resets the motion tracking system.
        /// 
        /// This reinitializes the <code>TANGO_COORDINATE_FRAME_START_OF_SERVICE</code> coordinate frame to where the
        /// device is when you call this function; afterwards, if you ask for the pose with relation to start of
        /// service, it uses this as the new origin.  You can call this function at any time.
        ///
        /// If you are using Area Learning, the <code>TANGO_COORDINATE_FRAME_AREA_DESCRIPTION</code> coordinate frame
        /// is not affected by calling this function; however, the device needs to localize again before you can use
        /// the area description.
        /// </summary>
        public static void ResetMotionTracking()
        {
            PoseProviderAPI.TangoService_resetMotionTracking();
        }

#if UNITY_EDITOR
        /// <summary>
        /// DEPRECATED: Legacy function that gets mouse / keyboard PoseEmulation data.
        /// </summary>
        /// <param name="controllerPostion">Controller postion.</param>
        /// <param name="controllerRotation">Controller rotation.</param>
        public static void GetMouseEmulation(ref Vector3 controllerPostion, ref Quaternion controllerRotation)
        {
            Vector3 position = controllerPostion;
            Quaternion rotation;
            Vector3 directionForward, directionRight, directionUp;
            float rotationX;
            float rotationY;
            
            rotationX = controllerRotation.eulerAngles.x - Input.GetAxis("Mouse Y") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            rotationY = controllerRotation.eulerAngles.y + Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            Vector3 eulerAngles = new Vector3(rotationX,rotationY,0);
            controllerRotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            rotation = Quaternion.Euler(eulerAngles);
            
            directionForward = rotation * Vector3.forward;
            directionRight =  rotation * Vector3.right;
            directionUp = rotation * Vector3.up;
            position = position + Input.GetAxis("Vertical") * directionForward * TRANSLATION_SPEED * Time.deltaTime;
            position = position + Input.GetAxis("Horizontal") * directionRight * TRANSLATION_SPEED * Time.deltaTime;
            if(Input.GetKey(KeyCode.R)) // Go Up
            {
                position += directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }
            if(Input.GetKey(KeyCode.F))  // Go Down
            {
                position -= directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }
            
            controllerRotation = rotation;
            controllerPostion = position;
        }
#endif

        #region ADF Functionality
        /// <summary>
        /// Gets the full list of unique area description IDs available on a device.
        /// 
        /// This is updated by calling <code>RefreshADFList</code>.
        /// </summary>
        /// <returns>The cached ADF list.</returns>
        public static UUID_list GetCachedADFList()
        {
            return m_adfList;
        }

        /// <summary>
        /// Gets the latest area description ID available on a device.
        /// </summary>
        /// <returns>The most recent area description ID.</returns>
        public static UUIDUnityHolder GetLatestADFUUID()
        {
            if (m_adfList == null)
            {
                return null;
            }
            return m_adfList.GetLatestADFUUID();
        }

        /// <summary>
        /// Check if an area description ID is valid.
        /// </summary>
        /// <returns><c>true</c> if the ID is valid; otherwise, <c>false</c>.</returns>
        /// <param name="toCheck">Area description ID to check.</param>
        public static bool IsUUIDValid(UUIDUnityHolder toCheck)
        {
            return toCheck != null && toCheck.IsObjectValid();
        }

        /// <summary>
        /// Gets the area description ID at the specified index as a string.
        /// </summary>
        /// <returns>The area description ID as a string.</returns>
        /// <param name="index">The index of the area description ID.</param>
        public static string GetUUIDAsString(int index)
        {
            if (m_adfList == null)
            {
                return string.Empty;
            }
            return m_adfList.GetUUIDAsString(index);
        }
        
        /// <summary>
        /// Gets the area description ID at the specified index as a char array.
        /// </summary>
        /// <returns>The area description ID as a char array.</returns>
        /// <param name="index">The index of the area description ID.</param>
        public static char[] GetUUIDAsCharArray(int index)
        {
            string uuidString = GetUUIDAsString(index);
            if (String.IsNullOrEmpty(uuidString))
            {
                return null;
            }
            return uuidString.ToCharArray();
        }

        /// <summary>
        /// Update the list returned by <code>GetCachedADFList</code>.
        /// </summary>
        /// <returns>Returns TANGO_SUCCESS on success, or TANGO_ERROR on failure to retrieve the list.</returns>
        public static int RefreshADFList()
        {
            int returnValue = Common.ErrorType.TANGO_ERROR;
            IntPtr tempData = IntPtr.Zero;
            returnValue = PoseProviderAPI.TangoService_getAreaDescriptionUUIDList(ref tempData);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".RefreshADFList() Could not get ADF list from device.");
            }
            else
            {
                byte[] charBuffer = new byte[sizeof(char)];
                List<byte> dataHolder = new List<byte>();
                Marshal.Copy(tempData, charBuffer, 0, 1);
                while (charBuffer[0] != 0 && charBuffer[0] != '\n')
                {
                    dataHolder.Add(charBuffer[0]);
                    tempData = new IntPtr(tempData.ToInt64() + 1);
                    Marshal.Copy(tempData, charBuffer, 0, 1);
                }
                string uuidList = System.Text.Encoding.UTF8.GetString(dataHolder.ToArray());
                m_adfList.PopulateUUIDList(uuidList);
                if (!m_adfList.HasEntries())
                {
                    Debug.Log(CLASS_NAME + ".RefreshADFList() No area description files found on device.");
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Saves the area description, returning the unique ID associated with the saved map.
        /// 
        /// You can only save an area description while connected to the Tango Service and if you have enabled Area
        /// Learning mode. If you loaded an ADF before connecting, then calling this method appends any new learned
        /// areas to that ADF and returns the same UUID. If you did not load an ADF, this method creates a new ADF and
        /// a new UUID for that ADF.
        /// </summary>
        /// <returns>
        /// Returns TANGO_SUCCESS on success, and TANGO_ERROR if a failure occurred when saving, or if the service
        /// needs to be initialized, or TANGO_INVALID if uuid is NULL, or of incorrect length, or if Area Learning Mode
        /// was not set (see logcat for details).
        /// </returns>
        /// <param name="adfUnityHolder">Upon saving, the TangoUUID to refer to this ADF is returned here.</param>
        public static int SaveAreaDescription(UUIDUnityHolder adfUnityHolder)
        {
            if (adfUnityHolder == null)
            {
                Debug.Log(CLASS_NAME + ".SaveAreaDescription() Could not save area description. UUID Holder object specified is not initialized");
                return Common.ErrorType.TANGO_ERROR;
            }
            IntPtr idData = Marshal.AllocHGlobal(Common.UUID_LENGTH);
            int returnValue = PoseProviderAPI.TangoService_saveAreaDescription(idData);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SaveAreaDescripton() Could not save area description with ID: " + adfUnityHolder.GetStringDataUUID());
            }
            else
            {
                byte[] tempDataBuffer = new byte[Common.UUID_LENGTH];
                Marshal.Copy(idData, tempDataBuffer, 0, Common.UUID_LENGTH);
                adfUnityHolder.SetDataUUID(tempDataBuffer);
            }
            return returnValue;
        }

        /// <summary>
        /// Saves the metadata associated with a single area description unique ID.
        /// </summary>
        /// <returns>
        /// Returns TANGO_SUCCESS on successful save, or TANGO_ERROR on failure, or if the service needs to be
        /// initialized.
        /// </returns>
        /// <param name="adfUnityHolder">The metadata and associated UUID to save.</param>
        public static int SaveAreaDescriptionMetaData(UUIDUnityHolder adfUnityHolder)
        {
            if (adfUnityHolder == null)
            {
                Debug.Log(CLASS_NAME + ".SaveAreaDescription() Could not save area description. UUID Holder object specified is not initialized");
                return Common.ErrorType.TANGO_ERROR;
            }
            if (string.IsNullOrEmpty(adfUnityHolder.GetStringDataUUID()))
            {
                Debug.Log(CLASS_NAME + ".MetaData cannot be retrived for the area description as UUIDUnityHolder object was empty or null.");
                return Common.ErrorType.TANGO_ERROR;
            }
            if (adfUnityHolder.uuidMetaData.meta_data_pointer == IntPtr.Zero)
            {
                Debug.Log(CLASS_NAME + "metadata pointer is null, cannot save metadata to this ADF!");
                return Common.ErrorType.TANGO_ERROR;
            }
            Debug.Log("UUID being saved is: " + adfUnityHolder.GetStringDataUUID());
            int returnValue = PoseProviderAPI.TangoService_saveAreaDescriptionMetadata(adfUnityHolder.GetStringDataUUID(), adfUnityHolder.uuidMetaData.meta_data_pointer);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + "Could not save metadata to the ADF!");
            }
            return returnValue;
        }

        /// <summary>
        /// Export an area with the UUID from the default area storage location to the destination file directory with
        /// the UUID as its name.
        /// </summary>
        /// <returns>Returns TANGO_SUCCESS if the file was exported, or TANGO_ERROR if the export failed.</returns>
        /// <param name="uuid">The UUID of the area.</param>
        /// <param name="filePath">The destination file directory.</param>
        public static int ExportAreaDescriptionToFile(string uuid, string filePath)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                Debug.Log("Can't export an empty UUID. Please define one.");
                return Common.ErrorType.TANGO_ERROR;
            }
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Missing file path for exporting area description. Please define one.");
                return Common.ErrorType.TANGO_ERROR;
            }
            AndroidHelper.StartExportADFActivity(uuid, filePath);
            return Common.ErrorType.TANGO_SUCCESS;
        }

        /// <summary>
        /// Deprecated: Import an area description from a file path to the default area storage location. 
        /// 
        /// Please call ImportAreaDescriptionFromFile(string filePath) instead.
        /// The new area description will get a new ID, which will be stored in adfID.
        /// </summary>
        /// <returns><c>Common.ErrorType.TANGO_SUCCESS</c> if the UUID was imported successfully.</returns>
        /// <param name="adfID">Upon successful return, this will have the new ID.</param>
        /// <param name="filePath">File path of the area descrption to be imported.</param>
        public static int ImportAreaDescriptionFromFile(UUIDUnityHolder adfID, string filePath)
        {
            if (adfID == null)
            {
                Debug.Log(CLASS_NAME + ".ImportAreaDescription() Could not  import area description. UUID Holder object specified is not initialized");
                return Common.ErrorType.TANGO_ERROR;
            }
            IntPtr uuidHolder = Marshal.AllocHGlobal(Common.UUID_LENGTH);
            int returnValue = PoseProviderAPI.TangoService_importAreaDescription(filePath, uuidHolder);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".ImportAreaDescription() Could not import area description at path: " + filePath);
            }
            else
            {
                byte[] tempDataBuffer = new byte[Common.UUID_LENGTH];
                Marshal.Copy(uuidHolder, tempDataBuffer, 0, Common.UUID_LENGTH);
                adfID.SetDataUUID(tempDataBuffer);
            }
            return returnValue;
        }

        /// <summary>
        /// Import an area description from a file path to the default area storage location. 
        /// </summary>
        /// <returns><c>Common.ErrorType.TANGO_SUCCESS</c> if the UUID was imported successfully.</returns>
        /// <param name="filePath">File path of the area descrption to be imported.</param>
        public static int ImportAreaDescriptionFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Missing file path for exporting area description. Please define one.");
                return Common.ErrorType.TANGO_ERROR;
            }
            AndroidHelper.StartImportADFActivity(filePath);
            return Common.ErrorType.TANGO_SUCCESS;
        }

        /// <summary>
        /// Deletes an area description with the specified unique ID.
        /// </summary>
        /// <returns>
        /// Returns TANGO_SUCCESS if area description file with specified unique ID is found and can be removed.
        /// </returns>
        /// <param name="toDeleteUUID">The area description to delete.</param>
        public static int DeleteAreaDescription(string toDeleteUUID)
        {
            if (string.IsNullOrEmpty(toDeleteUUID))
            {
                Debug.Log(CLASS_NAME + ".DeleteAreaDescription() Could not delete area description, UUID was empty or null.");
                return Common.ErrorType.TANGO_ERROR;
            }
            int returnValue = PoseProviderAPI.TangoService_deleteAreaDescription(toDeleteUUID);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".DeleteAreaDescription() Could not delete area description, API returned invalid.");
            }
            return returnValue;
        }
        #endregion // ADF Functionality

        #region ADF Metadata Functionality
        /// <summary>
        /// Gets the metadata handle associated with a single area description unique ID.
        /// </summary>
        /// <returns>
        /// Returns TANGO_SUCCESS on successful load of metadata, or TANGO_ERROR if the service needs to be initialized
        /// or if the metadata could not be loaded.
        /// </returns>
        /// <param name="adfUnityHolder">
        /// The TangoUUID for which to load the metadata.  On success, this function sets the pointer to raw UUID
        /// metadata which can then be extracted using AreaDescriptionMetaData_get, AreaDescriptionMetaData_get, or
        /// PopulateAreaDescriptionMetaDataKeyValues.
        /// </param>
        public static int GetAreaDescriptionMetaData(UUIDUnityHolder adfUnityHolder)
        {
            if (string.IsNullOrEmpty(adfUnityHolder.GetStringDataUUID()))
            {
                Debug.Log(CLASS_NAME + ".MetaData cannot be retrived for the area description as UUIDUnityHolder object was empty or null.");
                return Common.ErrorType.TANGO_ERROR;
            }
            int returnValue = PoseProviderAPI.TangoService_getAreaDescriptionMetadata(adfUnityHolder.GetStringDataUUID(), ref adfUnityHolder.uuidMetaData.meta_data_pointer);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + "Meta Data could not be loaded");
            }
            Debug.Log("GetAreaDescription return value is: " + returnValue.ToString());
            return returnValue;
        }

        /// <summary>
        /// Populates the Metadata key/value pairs of a given metadataPointer.
        /// 
        /// metaDataPointer should be initialized to a valid Metadata by calling the getAreaDescriptionMetaData().
        /// </summary>
        /// <returns>TANGO_SUCCESS if successful, else TANGO_INVALID or TANGO_ERROR.</returns>
        /// <param name="metadataPointer">Metadata pointer.</param>
        /// <param name="keyValuePairs">Dictionary of key/value pairs stored in the metadata.</param>
        public static int PopulateAreaDescriptionMetaDataKeyValues(IntPtr metadataPointer, ref Dictionary<string, string> keyValuePairs)
        {
            IntPtr keyList = IntPtr.Zero;
            if (metadataPointer == IntPtr.Zero)
            {
                Debug.Log(CLASS_NAME + "metadata pointer is null, cannot save metadata to this ADF!");
                return Common.ErrorType.TANGO_ERROR;
            }
            int returnValue = PoseProviderAPI.TangoAreaDescriptionMetadata_listKeys(metadataPointer, ref keyList);
            if (returnValue != Tango.Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Could not read metadata keys list");
            }
            string metadataKeys = Marshal.PtrToStringAuto(keyList);
            string[] keys = metadataKeys.Split(new char[] { ',' });
            string[] values = new string[keys.Length];
            for (int i = 0; i < values.Length; i++)
            {
                uint valuesize = 0;
                IntPtr valuePointer = IntPtr.Zero;
                PoseProviderAPI.TangoAreaDescriptionMetadata_get(metadataPointer, keys[i], ref valuesize, ref valuePointer);
                byte[] valueByteArray = new byte[valuesize];
                Marshal.Copy(valuePointer, valueByteArray, 0, (int)valuesize);
                values[i] = System.Text.Encoding.UTF8.GetString(valueByteArray);
                Debug.Log("Key Values are- " + keys[i] + ": " + values[i]);
                keyValuePairs.Add(keys[i], values[i]);
            }
            return returnValue;
        }

        /// <summary>
        /// Get the value of a key from a metadata.
        /// </summary>
        /// <returns>TANGO_SUCCESS if successful, else TANGO_INVALID or TANGO_ERROR.</returns>
        /// <param name="key">Key to lookup.</param>
        /// <param name="value">On success, the value for that key.</param>
        /// <param name="adfUnityHolder">Area description + metadata holder.</param>
        public static int AreaDescriptionMetaData_get(String key, ref String value, UUIDUnityHolder adfUnityHolder)
        {
            if (string.IsNullOrEmpty(adfUnityHolder.GetStringDataUUID()))
            {
                Debug.Log(CLASS_NAME + ".MetaData cannot be retrived for the area description as UUIDUnityHolder object was empty or null.");
                return Common.ErrorType.TANGO_ERROR;
            }
            uint valuesize = 0;
            IntPtr valuePointer = IntPtr.Zero;
            int returnValue = PoseProviderAPI.TangoAreaDescriptionMetadata_get(adfUnityHolder.uuidMetaData.meta_data_pointer, key, ref valuesize, ref valuePointer);
            if (returnValue != Tango.Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Could not read metadata key, Error return value is: " + returnValue);
                return returnValue;
            }
            else
            {
                byte[] valueByteArray = new byte[valuesize];
                Marshal.Copy(valuePointer, valueByteArray, 0, (int)valuesize);
                value = System.Text.Encoding.UTF8.GetString(valueByteArray);
                return returnValue;
            }
        }

        /// <summary>
        /// Set the value of a key in a metadata.
        /// </summary>
        /// <returns>TANGO_SUCCESS if successful, else TANGO_INVALID or TANGO_ERROR.</returns>
        /// <param name="key">Key to set the value of.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="adfUnityHolder">Area description + metadata holder.</param>
        public static int AreaDescriptionMetaData_set(String key, String value, UUIDUnityHolder adfUnityHolder)
        {
            if (string.IsNullOrEmpty(adfUnityHolder.GetStringDataUUID()))
            {
                Debug.Log(CLASS_NAME + ".MetaData cannot be retrived for the area description as UUIDUnityHolder object was empty or null.");
                return Common.ErrorType.TANGO_ERROR;
            }

            int returnValue = PoseProviderAPI.TangoAreaDescriptionMetadata_set(adfUnityHolder.uuidMetaData.meta_data_pointer, key, (uint)value.Length, value);
            if (returnValue != Tango.Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Could not set Metadata Key, Error return value is: " + returnValue);
                return returnValue;
            }
            else
            {
                Debug.Log("Metadata Set succesful, Key set is: " + key + " Value set is: " + value);
                return returnValue;
            }
        }
        #endregion // ADF Metadata Functionality

        /// <summary>
        /// Set the C callback for the Tango pose interface.
        /// </summary>
        /// <param name="framePairs">Passed in to the C API.</param>
        /// <param name="callback">Callback.</param>
        internal static void SetCallback(TangoCoordinateFramePair[] framePairs, TangoService_onPoseAvailable callback)
        {
            int returnValue = PoseProviderAPI.TangoService_connectOnPoseAvailable(framePairs.Length, framePairs, callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was not set!");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".SetCallback() OnPose callback was set!");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Update the Tango emulation state for pose data.
        /// 
        /// Make this this is only called once per frame.
        /// </summary>
        internal static void UpdateTangoEmulation()
        {
            // Update the emulated rotation (do this first to make sure the position is rotated)
            //
            // Note: We need to use Input.GetAxis here because Unity3D does not provide a way to get the underlying
            // mouse delta.
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                m_emulatedPoseAnglesFromForward.y -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
                m_emulatedPoseAnglesFromForward.x += Input.GetAxis("Mouse Y") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            }
            else
            {
                m_emulatedPoseAnglesFromForward.z -= Input.GetAxis("Mouse X") * MOUSE_LOOK_SENSITIVITY * Time.deltaTime;
            }
            
            // Update the emulated position
            Quaternion emulatedPoseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(m_emulatedPoseAnglesFromForward);
            Vector3 directionRight = emulatedPoseRotation * new Vector3(1, 0, 0);
            Vector3 directionForward = emulatedPoseRotation * new Vector3(0, 0, -1);
            Vector3 directionUp = emulatedPoseRotation * new Vector3(0, 1, 0);
            
            if (Input.GetKey(KeyCode.W))
            {
                // Forward
                m_emulatedPosePosition += directionForward * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                // Backward
                m_emulatedPosePosition -= directionForward * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A))
            {
                // Left
                m_emulatedPosePosition -= directionRight * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                // Right
                m_emulatedPosePosition += directionRight * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.R))
            {
                // Up
                m_emulatedPosePosition += directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.F))
            {
                // Down
                m_emulatedPosePosition -= directionUp * TRANSLATION_SPEED * Time.deltaTime;
            }
        }

        /// <summary>
        /// INTERNAL USE: Get the most recent values for Tango emulation.
        /// </summary>
        /// <param name="posePosition">The new Tango emulation position.</param>
        /// <param name="poseRotation">The new Tango emulation rotation.</param>
        internal static void GetTangoEmulation(out Vector3 posePosition, out Quaternion poseRotation)
        {
            posePosition = m_emulatedPosePosition;
            poseRotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(m_emulatedPoseAnglesFromForward);
        }
#endif

        #region API_Functions
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct PoseProviderAPI
        { 
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnPoseAvailable(int count,
                                                                         TangoCoordinateFramePair[] framePairs,
                                                                         TangoService_onPoseAvailable onPoseAvailable);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_getPoseAtTime(double timestamp,
                                                                TangoCoordinateFramePair framePair,
                                                                [In, Out] TangoPoseData pose);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_setPoseListenerFrames(int count,
                                                                        ref TangoCoordinateFramePair frames);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern void TangoService_resetMotionTracking();

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_saveAreaDescription(IntPtr uuid);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_getAreaDescriptionUUIDList(ref IntPtr uuid_list);       
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_getAreaDescriptionMetadata([MarshalAs(UnmanagedType.LPStr)] string uuid,
                                                                             ref IntPtr metadata);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_saveAreaDescriptionMetadata([MarshalAs(UnmanagedType.LPStr)] string uuid,
                                                                               IntPtr metadata);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_importAreaDescription([MarshalAs(UnmanagedType.LPStr)] string source_file_path, IntPtr UUID);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_exportAreaDescription([MarshalAs(UnmanagedType.LPStr)] string UUID, 
                                                                        [MarshalAs(UnmanagedType.LPStr)] string dst_file_path);
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_deleteAreaDescription([MarshalAs(UnmanagedType.LPStr)] string UUID);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoAreaDescriptionMetadata_free(IntPtr metadata);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoAreaDescriptionMetadata_get(IntPtr metadata, [MarshalAs(UnmanagedType.LPStr)] string key, 
                                                                      ref UInt32 value_size, ref IntPtr value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoAreaDescriptionMetadata_set(IntPtr metadata, [MarshalAs(UnmanagedType.LPStr)] string key, 
                                                                      UInt32 value_size, [MarshalAs(UnmanagedType.LPStr)] string value);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoAreaDescriptionMetadata_listKeys(IntPtr metadata, ref IntPtr key_list);
#else
            public static int TangoService_connectOnPoseAvailable(int count,
                                                                  TangoCoordinateFramePair[] framePairs,
                                                                  TangoService_onPoseAvailable onPoseAvailable)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_getPoseAtTime(double timestamp,
                                                         TangoCoordinateFramePair framePair,
                                                         [In, Out] TangoPoseData pose)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_setPoseListenerFrames(int count,
                                                                 ref TangoCoordinateFramePair frames)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static void TangoService_resetMotionTracking()
            {
            }

            public static int TangoService_saveAreaDescription(IntPtr uuid)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_getAreaDescriptionUUIDList(ref IntPtr uuid_list)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_getAreaDescriptionMetadata([MarshalAs(UnmanagedType.LPStr)] string uuid,
                                                                             ref IntPtr metadata)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_saveAreaDescriptionMetadata([MarshalAs(UnmanagedType.LPStr)] string uuid,
                                                                               IntPtr metadata)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_importAreaDescription([MarshalAs(UnmanagedType.LPStr)] string source_file_path, IntPtr uuid)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_exportAreaDescription([MarshalAs(UnmanagedType.LPStr)] string uuid, 
                                                                 [MarshalAs(UnmanagedType.LPStr)] string dst_file_path)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_deleteAreaDescription([MarshalAs(UnmanagedType.LPStr)] string uuid)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoAreaDescriptionMetadata_free(IntPtr metadata)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoAreaDescriptionMetadata_get(IntPtr metadata, [MarshalAs(UnmanagedType.LPStr)] string key, 
                                                               ref UInt32 value_size, ref IntPtr value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoAreaDescriptionMetadata_set(IntPtr metadata, [MarshalAs(UnmanagedType.LPStr)] string key, 
                                                               UInt32 value_size, [MarshalAs(UnmanagedType.LPStr)] string value)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoAreaDescriptionMetadata_listKeys(IntPtr metadata, ref IntPtr key_list)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
        #endregion
    }
}
