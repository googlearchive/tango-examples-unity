//-----------------------------------------------------------------------
// <copyright file="TangoUxEnums.cs" company="Google">
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
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Enumerations used by <c>TangoUx</c>.
    /// </summary>
    public class TangoUxEnums
    {
        /// <summary>
        /// Possible types for a UX Exception Event.
        /// </summary>
        public enum UxExceptionEventType
        {
            TYPE_OVER_EXPOSED,  /**< Camera is over exposed */
            TYPE_UNDER_EXPOSED, /**< Camera is under exposed */
            TYPE_MOVING_TOO_FAST, /**< Device is being moved too fast */
            TYPE_FEW_FEATURES,  /**< Too few features */
            TYPE_FEW_DEPTH_POINTS,  /**< Unable to detect any surface */
            TYPE_LYING_ON_SURFACE,  /**< Device is lying on a surface */
            TYPE_MOTION_TRACK_INVALID,  /**< Motion tracking is invalid */
            TYPE_TANGO_SERVICE_NOT_RESPONDING,  /**< Tango Service stopped responding */
            TYPE_INCOMPATIBLE_VM,  /**< Incompatible vm is found */
            TYPE_TANGO_UPDATE_NEEDED,  /**< Tango version update is needed */
            NA  /***<Not Available, not a real Ux Exception event type*/
        }

        /// <summary>
        /// Possible status for exceptions.
        /// </summary>
        public enum UxExceptionEventStatus
        {
            STATUS_RESOLVED,  /**< The exception was resolved */
            STATUS_DETECTED,  /**< The exception was detected */
            NA  /***<Not Available, not a real ux exception event status*/
        }

        /// <summary>
        /// Possible types for a UX Hold Posture.
        /// </summary>
        public enum UxHoldPostureType
        {
            NONE,  /**< No posture defined */
            FORWARD,  /**< Device should be pointed forward */
            UP,  /**< Device should be pointed upwards */
            DOWN  /**< Device should be pointed downwards */
        }
    }
}
