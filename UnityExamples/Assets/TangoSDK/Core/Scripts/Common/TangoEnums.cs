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
using System.Collections;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Enumerations used by the Tango Service.
    /// </summary>
    public class TangoEnums
    {
        /// <summary>
        /// Possible states for the motion tracking
        /// </summary>
        public enum TangoPoseStatusType
        {
            TANGO_POSE_INITIALIZING,  /**< Motion estimation is being initialized */
            TANGO_POSE_VALID,             /**< The pose of this estimate is valid */
            TANGO_POSE_INVALID,           /**< The pose of this estimate is not valid */
            TANGO_POSE_UNKNOWN,            /**< Could not estimate pose at this time */
            NA                             /***<Not Available, not a real tango pose status type*/
        }
        
        /// <summary>
        /// Coordinate frames provided by the Tango Service.
        /// </summary>
        public enum TangoCoordinateFrameType
        {
            /** Coordinate system for the entire Earth.
            *  See WGS84: http://en.wikipedia.org/wiki/World_Geodetic_System
            */
            TANGO_COORDINATE_FRAME_GLOBAL_WGS84 = 0,
            /** Origin within a saved area description */
            TANGO_COORDINATE_FRAME_AREA_DESCRIPTION,
            /** Origin when the device started tracking */
            TANGO_COORDINATE_FRAME_START_OF_SERVICE,
            /** Immediately previous device pose */
            TANGO_COORDINATE_FRAME_PREVIOUS_DEVICE_POSE,
            TANGO_COORDINATE_FRAME_DEVICE,         /**< Device coordinate frame */
            TANGO_COORDINATE_FRAME_IMU,            /**< Inertial Measurement Unit */
            TANGO_COORDINATE_FRAME_DISPLAY,        /**< Display */
            TANGO_COORDINATE_FRAME_CAMERA_COLOR,   /**< Color camera */
            TANGO_COORDINATE_FRAME_CAMERA_DEPTH,   /**< Depth camera */
            TANGO_COORDINATE_FRAME_CAMERA_FISHEYE, /**< Fisheye camera */
            TANGO_COORDINATE_FRAME_INVALID,
            TANGO_MAX_COORDINATE_FRAME_TYPE /**< Maximum allowed */
        }
        
        /// <summary>
        /// Enumeration containing the ID used for each
        /// Tango camera.
        /// </summary>
        public enum TangoCameraId
        {
            TANGO_CAMERA_COLOR = 0, /**< Back-facing color camera */
            TANGO_CAMERA_RGBIR,      /**< Back-facing camera producing IR-sensitive images */
            TANGO_CAMERA_FISHEYE,   /**< Back-facing fisheye wide-angle camera */
            TANGO_CAMERA_DEPTH,     /**< Depth camera */
            TANGO_MAX_CAMERA_ID     /**< Maximum camera allowable */
        }

        /// <summary>
        /// Enumeration containing events provided by the
        /// Tango Service.
        /// </summary>
        public enum TangoEventType
        {
            TANGO_EVENT_UNKNOWN = 0,         /**< Unclassified Event Type */
            TANGO_EVENT_GENERAL,             /**< General callbacks not otherwise categorized */
            TANGO_EVENT_FISHEYE_CAMERA,       /**< Fisheye Camera Event */
            TANGO_EVENT_COLOR_CAMERA,         /**< Color Camera Event */
            TANGO_EVENT_IMU,                  /**< IMU Event */
            TANGO_EVENT_FEATURE_TRACKING,     /**< Feature Tracking Event */
        }    

        public enum TangoConfigType
        {
            TANGO_CONFIG_DEFAULT = 0,     /**< Default, motion tracking only. */
            TANGO_CONFIG_CURRENT,         /**< Current */
            TANGO_CONFIG_MOTION_TRACKING, /**< Motion tracking */
            TANGO_CONFIG_AREA_LEARNING,   /**< Area learning */
            TANGO_MAX_CONFIG_TYPE         /**< Maximum number allowable.  */
        }

        public enum TangoCalibrationType
        {
            TANGO_CALIBRATION_UNKNOWN,
            /**< f-theta fisheye lens model. See
               http://scholar.google.com/scholar?cluster=13508836606423559694&hl=en&as_sdt=2005&sciodt=0,5
             */
            TANGO_CALIBRATION_EQUIDISTANT,
            TANGO_CALIBRATION_POLYNOMIAL_2_PARAMETERS,
            /**< Tsai's k1,k2,k3 Model. See
               http://scholar.google.com/scholar?cluster=3512800631607394002&hl=en&as_sdt=0,5&sciodt=0,5
             */
            TANGO_CALIBRATION_POLYNOMIAL_3_PARAMETERS,
            TANGO_CALIBRATION_POLYNOMIAL_5_PARAMETERS,

        }
        
        /// Tango Image Formats
        /// Equivalent to those found in Android core/system/include/system/graphics.h.
        public enum TangoImageFormatType
        {
            TANGO_HAL_PIXEL_FORMAT_YV12 = 0x32315659  // YCrCb 4:2:0 Planar
        }
    }
}