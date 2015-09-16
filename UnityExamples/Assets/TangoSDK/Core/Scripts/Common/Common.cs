//-----------------------------------------------------------------------
// <copyright file="Common.cs" company="Google">
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
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tango
{
    /// <summary>
    /// This struct holds common global functionality used by
    /// this SDK.
    /// </summary>
    public struct Common
    {
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

        /// <summary>
        /// Return values from Android actvities.
        /// </summary>
        public enum AndroidResult
        {
            SUCCESS = -1,
            CANCELED = 0,
            DENIED = 1
        }

        /// <summary>
        /// DEPRECATED: Empty string.
        /// </summary>
        public const string TANGO_PERMISSION_STRING = "";

        /// <summary>
        /// DEPRECATED: Tango permissions error.
        /// </summary>
        public const string TANGO_NO_PERMISSIONS_ERROR = "This application requires all Tango permissions to run. Please restart the application and grant Tango permissions.";

        //// \cond
        //// Collection of deprecated fields that should be removed
        public const float UI_LABEL_START_X = 15.0f;
        public const float UI_LABEL_START_Y = 15.0f;
        public const float UI_LABEL_SIZE_X = 1920.0f;
        public const float UI_LABEL_SIZE_Y = 35.0f;
        public const float UI_LABEL_GAP_Y = 3.0f;
        public const float UI_BUTTON_SIZE_X = 125.0f;
        public const float UI_BUTTON_SIZE_Y = 65.0f;
        public const float UI_BUTTON_GAP_X = 5.0f;
        public const float UI_CAMERA_BUTTON_OFFSET = UI_BUTTON_SIZE_X + UI_BUTTON_GAP_X; 
        public const float UI_LABEL_OFFSET = UI_LABEL_GAP_Y + UI_LABEL_SIZE_Y;
        public const float UI_FPS_LABEL_START_Y = UI_LABEL_START_Y + UI_LABEL_OFFSET;
        public const float UI_EVENT_LABEL_START_Y = UI_FPS_LABEL_START_Y + UI_LABEL_OFFSET;
        public const float UI_POSE_LABEL_START_Y = UI_EVENT_LABEL_START_Y + UI_LABEL_OFFSET;
        public const float UI_DEPTH_LABLE_START_Y = UI_POSE_LABEL_START_Y + UI_LABEL_OFFSET;
        public const string UI_FLOAT_FORMAT = "F3";
        public const string UI_FONT_SIZE = "<size=25>";
        
        public const float UI_TANGO_VERSION_X = UI_LABEL_START_X;
        public const float UI_TANGO_VERSION_Y = UI_LABEL_START_Y;
        public const float UI_TANGO_APP_SPECIFIC_START_X = UI_TANGO_VERSION_X;
        public const float UI_TANGO_APP_SPECIFIC_START_Y = UI_TANGO_VERSION_Y + (UI_LABEL_OFFSET * 2);
        
        public const string UX_SERVICE_VERSION = "Service version: {0}";
        public const string UX_TANGO_SERVICE_VERSION = "Tango service version: {0}";
        public const string UX_TANGO_SYSTEM_EVENT = "Tango system event: {0}";
        public const string UX_TARGET_TO_BASE_FRAME = "Target->{0}, Base->{1}:";
        public const string UX_STATUS = "\tstatus: {0}, count: {1}, delta time(ms): {2}, position (m): [{3}], orientation: [{4}]";
        public const float SECOND_TO_MILLISECOND = 1000.0f;
        //// \endcond

        /// <summary>
        /// Name of the Tango C-API library.
        /// </summary>
        internal const string TANGO_UNITY_DLL = "tango_client_api";

        /// <summary>
        /// Motion Tracking permission intent string.
        /// </summary>
        internal const string TANGO_MOTION_TRACKING_PERMISSIONS = "MOTION_TRACKING_PERMISSION";

        /// <summary>
        /// ADF Load/Save permission intent string.
        /// </summary>
        internal const string TANGO_ADF_LOAD_SAVE_PERMISSIONS = "ADF_LOAD_SAVE_PERMISSION";

        /// <summary>
        /// Code used to identify the result came from the Motion Tracking permission request.
        /// </summary>
        internal const int TANGO_MOTION_TRACKING_PERMISSIONS_REQUEST_CODE = 42;

        /// <summary>
        /// Code used to identify the result came from the ADF Load/Save permission request.
        /// </summary>
        internal const int TANGO_ADF_LOAD_SAVE_PERMISSIONS_REQUEST_CODE = 43;

        /// <summary>
        /// Max number of vertices the Point Cloud supports.
        /// </summary>
        internal const int UNITY_MAX_SUPPORTED_VERTS_PER_MESH = 65534;

        /// <summary>
        /// The length of an area description ID string.
        /// </summary>
        internal const int UUID_LENGTH = 37;

        //// Backing for deprecated properties below.
#if (UNITY_EDITOR)
        private static bool m_mirroring = true; 
#elif (UNITY_ANDROID) 
        private static bool m_mirroring = false; 
#else 
        private static bool m_mirroring = false;
#endif
        private static Resolution m_depthFrameResolution;
        private static int m_depthBufferSize;

        /// <summary>
        /// DEPRECATED: Property for mirroring.
        /// </summary>
        /// <value> Bool - sets mirroring.</value>
        public static bool Mirroring
        {
            get { return m_mirroring; }
            set { m_mirroring = value; }
        }

        /// <summary>
        /// DEPRECATED: Property for the current depth frame resolution.
        /// </summary>
        /// <value> Resolution - Sets depth frame resolution reference.</value>
        public static Resolution DepthFrameResolution
        {
            get { return m_depthFrameResolution; }
            set { m_depthFrameResolution = value; }
        }

        /// <summary>
        /// DEPRECATED: Property for the depth buffer size.
        /// </summary>
        /// <value> Bool - Sets the size of the depth buffer.</value>
        public static int DepthBufferSize
        {
            get { return m_depthBufferSize; }
            set { m_depthBufferSize = value; }
        }

        /// <summary>
        /// DEPRECATED: Get the world rotation.
        /// </summary>
        /// <returns> Quaternion representing the world rotation.</returns>
        public static Quaternion GetWorldRotation()
        {
            return OrientationManager.GetWorldRotation();
        } 
        
        /// <summary>
        /// DEPRECATED: Get the screen size if the screen was in landscape mode.
        /// 
        /// This looks at Screen.width and Screen.height.
        /// </summary>
        /// <returns>Vector2 containing the screen size.</returns>
        public static Vector2 GetWindowResolution()
        {
            Vector2 screenSize;
            if (Screen.width > Screen.height)
            {
                screenSize = new Vector2(Screen.width, Screen.height);
            }
            else
            {
                screenSize = new Vector2(Screen.height, Screen.width);
            }
            return screenSize;
        }

        /// <summary>
        /// DEPRECATED: Get the aspect ratio of the screen in landscape mode.
        /// </summary>
        /// <returns>Aspect ratio.</returns>
        public static float GetWindowResolutionAspect()
        {
            Vector2 resolution = GetWindowResolution();
            return resolution.x / resolution.y;
        }

        /// <summary>
        /// DEPRECATED: Misspelled version of GetWindowResolutionAspect.
        /// </summary>
        /// <returns>Aspect ratio.</returns>
        public static float GetWindowResoltionAspect()
        {
            Vector2 resolution = GetWindowResolution();
            return resolution.x / resolution.y;
        }
    }
}