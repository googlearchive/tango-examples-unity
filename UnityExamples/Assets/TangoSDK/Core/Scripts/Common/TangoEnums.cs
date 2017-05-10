//-----------------------------------------------------------------------
// <copyright file="TangoEnums.cs" company="Google">
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
    /// <summary>
    /// Enumerations used by the Tango Service.
    /// </summary>
    public class TangoEnums
    {
        /// <summary>
        /// Tango pose status lifecycle enumerations.
        ///
        /// Every pose has a state denoted by this enum, which provides information about the internal status of the
        /// position estimate. The application may use the status to decide what actions or rendering should be taken.
        /// A change in the status between poses and subsequent timestamps can denote lifecycle state changes. The
        /// status affects the rotation and position estimates. Other fields are considered valid (i.e. version or
        /// timestamp).
        /// </summary>
        public enum TangoPoseStatusType
        {
            /// <summary>
            /// Motion estimation is being initialized.
            /// </summary>
            TANGO_POSE_INITIALIZING,

            /// <summary>
            /// The pose of this estimate is valid.
            /// </summary>
            TANGO_POSE_VALID,

            /// <summary>
            /// The pose of this estimate is not valid.
            /// </summary>
            TANGO_POSE_INVALID,

            /// <summary>
            /// Could not estimate pose at this time.
            /// </summary>
            TANGO_POSE_UNKNOWN,

            /// <summary>
            /// Not Available, not a real <c>TangoPoseStatusType</c>.
            /// </summary>
            NA
        }

        /// <summary>
        /// Tango coordinate frame enumerations.
        /// </summary>
        public enum TangoCoordinateFrameType
        {
            /// <summary>
            /// Coordinate system for the entire Earth.
            ///
            /// See WGS84: [http://en.wikipedia.org/wiki/World_Geodetic_System].
            /// </summary>
            TANGO_COORDINATE_FRAME_GLOBAL_WGS84 = 0,

            /// <summary>
            /// Origin within a saved area description.
            /// </summary>
            TANGO_COORDINATE_FRAME_AREA_DESCRIPTION,

            /// <summary>
            /// Origin when the device started tracking.
            /// </summary>
            TANGO_COORDINATE_FRAME_START_OF_SERVICE,

            /// <summary>
            /// Immediately previous device pose.
            /// </summary>
            TANGO_COORDINATE_FRAME_PREVIOUS_DEVICE_POSE,

            /// <summary>
            /// Device coordinate frame.
            /// </summary>
            TANGO_COORDINATE_FRAME_DEVICE,

            /// <summary>
            /// Inertial Measurement Unit.
            /// </summary>
            TANGO_COORDINATE_FRAME_IMU,

            /// <summary>
            /// Display coordinate frame.
            /// </summary>
            TANGO_COORDINATE_FRAME_DISPLAY,

            /// <summary>
            /// Color camera.
            /// </summary>
            TANGO_COORDINATE_FRAME_CAMERA_COLOR,

            /// <summary>
            /// Depth camera.
            /// </summary>
            TANGO_COORDINATE_FRAME_CAMERA_DEPTH,

            /// <summary>
            /// Fisheye camera.
            /// </summary>
            TANGO_COORDINATE_FRAME_CAMERA_FISHEYE,

            /// <summary>
            /// An invalid frame.
            /// </summary>
            TANGO_COORDINATE_FRAME_INVALID,

            /// <summary>
            /// Maximum allowed.
            /// </summary>
            TANGO_MAX_COORDINATE_FRAME_TYPE
        }

        /// <summary>
        /// Tango Camera enumerations.
        /// </summary>
        public enum TangoCameraId
        {
            /// <summary>
            /// Back-facing color camera.
            /// </summary>
            TANGO_CAMERA_COLOR = 0,

            /// <summary>
            /// Back-facing camera producing IR-sensitive images.
            /// </summary>
            TANGO_CAMERA_RGBIR,

            /// <summary>
            /// Back-facing fisheye wide-angle camera.
            /// </summary>
            TANGO_CAMERA_FISHEYE,

            /// <summary>
            /// Depth camera.
            /// </summary>
            TANGO_CAMERA_DEPTH,

            /// <summary>
            /// Maximum camera allowable.
            /// </summary>
            TANGO_MAX_CAMERA_ID
        }

        /// <summary>
        /// Tango Event types.
        /// </summary>
        public enum TangoEventType
        {
            /// <summary>
            /// Unclassified Event Type.
            /// </summary>
            TANGO_EVENT_UNKNOWN = 0,

            /// <summary>
            /// General callbacks not otherwise categorized.
            /// </summary>
            TANGO_EVENT_GENERAL,

            /// <summary>
            /// Fisheye Camera Event.
            /// </summary>
            TANGO_EVENT_FISHEYE_CAMERA,

            /// <summary>
            /// Color Camera Event.
            /// </summary>
            TANGO_EVENT_COLOR_CAMERA,

            /// <summary>
            /// IMU Event.
            /// </summary>
            TANGO_EVENT_IMU,

            /// <summary>
            /// Feature Tracking Event.
            /// </summary>
            TANGO_EVENT_FEATURE_TRACKING,

            /// <summary>
            /// Area Learning Event.
            /// </summary>
            TANGO_EVENT_AREA_LEARNING,
        }

        /// <summary>
        /// Tango runtime configuration enumerations.
        /// </summary>
        public enum TangoConfigType
        {
            /// <summary>
            /// Default, motion tracking only.
            /// </summary>
            TANGO_CONFIG_DEFAULT = 0,

            /// <summary>
            /// Current config.
            /// </summary>
            TANGO_CONFIG_CURRENT,

            /// <summary>
            /// Motion tracking.
            /// </summary>
            TANGO_CONFIG_MOTION_TRACKING,

            /// <summary>
            /// Area learning.
            /// </summary>
            TANGO_CONFIG_AREA_LEARNING,

            /// <summary>
            /// Runtime configuration parameters.
            /// </summary>
            TANGO_CONFIG_RUNTIME,

            /// <summary>
            /// Maximum number allowable.
            /// </summary>
            TANGO_MAX_CONFIG_TYPE
        }

        /// <summary>
        /// Tango Camera Calibration types.
        ///
        /// See TangoCameraIntrinsics for a detailed description.
        /// </summary>
        public enum TangoCalibrationType
        {
            TANGO_CALIBRATION_UNKNOWN,

            /// <summary>
            /// An f-theta fisheye lens model.
            /// See <![CDATA[http://scholar.google.com/scholar?cluster=13508836606423559694&hl=en&as_sdt=2005&sciodt=0,5]]>.
            /// </summary>
            TANGO_CALIBRATION_EQUIDISTANT,

            /// <summary>
            /// Brown's model, with the parameter vector representing Brown's model
            ///  distortion as [k1, k2].
            /// </summary>
            TANGO_CALIBRATION_POLYNOMIAL_2_PARAMETERS,

            /// <summary>
            /// Brown's model, with the parameter vector representing Brown's model
            ///  distortion as [k1, k2, k3].
            /// </summary>
            TANGO_CALIBRATION_POLYNOMIAL_3_PARAMETERS,

            /// <summary>
            /// Brown's model, with the parameter vector representing Brown's model
            ///  distortion as [k1, k2, p1, p2, k3].
            /// </summary>
            TANGO_CALIBRATION_POLYNOMIAL_5_PARAMETERS,
        }

        /// <summary>
        /// Tango Image Formats.
        /// Equivalent to those found in Android core/system/include/system/graphics.h.
        /// </summary>
        public enum TangoImageFormatType
        {
            /// <summary>
            /// YCrCb 4:2:0 Planar.
            /// </summary>
            TANGO_HAL_PIXEL_FORMAT_YV12 = 0x32315659
        }

        /// <summary>
        /// Depth camera rates.
        /// </summary>
        public enum TangoDepthCameraRate
        {
            /// <summary>
            /// Disable the depth camera entirely.
            /// </summary>
            DISABLED,

            /// <summary>
            /// The maximum depth camera rate supported.  This is 5 on the Tango tablet.
            /// </summary>
            MAXIMUM,
        }
            
        /// <summary>
        /// Type of Tango pose.
        /// </summary>
        public enum TangoPoseType
        {
            /// <summary>
            /// The pose based solely on motion tracking.
            /// </summary>
            MOTION_TRACKING_POSE,

            /// <summary>
            /// The pose based on a local area description file.
            /// </summary>
            LOCAL_AREA_DESCRIPTION_POSE,

            /// <summary>
            /// The pose based on cloud-based area description.
            /// </summary>
            CLOUD_AREA_DESCRIPTION_POSE
        }
    }
}
