//-----------------------------------------------------------------------
// <copyright file="Common.cs" company="Google">
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
    using System.Runtime.InteropServices;
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    /// <summary>
    /// This struct holds common global functionality used by
    /// this SDK.
    /// </summary>
    public struct Common
    {
        /// <summary>
        /// Name of the Tango C-API library.
        /// </summary>
        internal const string TANGO_CLIENT_API_DLL = "tango_client_api";

        /// <summary>
        /// Name of the Tango Unity library.
        /// </summary>
        internal const string TANGO_UNITY_DLL = "tango_unity_lib";

        /// <summary>
        /// ADF Load/Save permission intent string.
        /// </summary>
        internal const string TANGO_ADF_LOAD_SAVE_PERMISSIONS = "ADF_LOAD_SAVE_PERMISSION";

        /// <summary>
        /// The Android camera permission string.
        /// </summary>
        internal const string ANDROID_CAMERA_PERMISSION = "android.permission.CAMERA";

        /// <summary>
        /// The android permission for accessing fine location.
        /// </summary>
        internal const string ANDROID_ACCESS_FINE_LOCATION_PERMISSION = "android.permission.ACCESS_FINE_LOCATION";

        /// <summary>
        /// Code used to identify the result came from the ADF Load/Save permission request.
        /// </summary>
        internal const int TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE = 43;

        /// <summary>
        /// Code used to identify the result came from the ADF import activity.
        /// </summary>
        internal const int TANGO_ADF_IMPORT_REQUEST_CODE = 44;

        /// <summary>
        /// Code used to identify the result came from the ADF export activity.
        /// </summary>
        internal const int TANGO_ADF_EXPORT_REQUEST_CODE = 45;

        /// <summary>
        /// Code used to identify the result came from requesting Android permissions.
        /// </summary>
        internal const int ANDROID_PERMISSION_REQUEST_CODE = 46;

        /// <summary>
        /// Max number of vertices the Point Cloud supports.
        /// </summary>
        internal const int MAX_NUM_POINTS = 65534;

        /// <summary>
        /// The length of an area description ID string.
        /// </summary>
        internal const int UUID_LENGTH = 37;

        /// <summary>
        /// Number of seconds per nanoseconds.
        /// </summary>
        internal const double SECS_PER_NANOSECS = 1E-9;

        /// <summary>
        /// Return values from Android activities.
        /// </summary>
        public enum AndroidResult
        {
            SUCCESS = -1,
            CANCELED = 0,
            DENIED = 1
        }

        /// <summary>
        /// Codes returned by Tango API functions.
        /// </summary>
        public struct ErrorType
        {
            /// <summary>
            /// Camera access not allowed.
            /// </summary>
            public static readonly int TANGO_NO_CAMERA_PERMISSION = -5;

            /// <summary>
            /// ADF access not allowed.
            /// </summary>
            public static readonly int TANGO_NO_ADF_PERMISSION = -4;

            /// <summary>
            /// Motion tracking not allowed.
            /// </summary>
            public static readonly int TANGO_NO_MOTION_TRACKING_PERMISSION = -3;

            /// <summary>
            /// General invalid state.
            /// </summary>
            public static readonly int TANGO_INVALID = -2;

            /// <summary>
            /// General error state.
            /// </summary>
            public static readonly int TANGO_ERROR = -1;

            /// <summary>
            /// No error, success.
            /// </summary>
            public static readonly int TANGO_SUCCESS = 0;
        }

        /// <summary>
        /// Metadata keys supported by Tango APIs.
        /// </summary>
        public struct MetaDataKeyType
        {
            public const string KEY_UUID = "id";
            public const string KEY_NAME = "name";
            public const string KEY_DATE = "date_ms_since_epoch";
            public const string KEY_TRANSFORMATION = "transformation";
        }
    }
}
