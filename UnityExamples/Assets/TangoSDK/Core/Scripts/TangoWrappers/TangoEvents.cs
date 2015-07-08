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
    /// Wraps the interface from Tango Service to register
    /// for callbacks that are fired on new events.
    /// </summary>
    public class TangoEvents
    {
        // Signature used by the onTangoEvent callback.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TangoService_onEventAvailable(IntPtr callbackContext, [In,Out] TangoEvent tangoEvent);
            
        /// <summary>
        /// Sets the callback that is called when a new tango
        /// event has been issued by the Tango Service.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public static void SetCallback(TangoService_onEventAvailable callback)
        {
            int returnValue = EventsAPI.TangoService_connectOnTangoEvent(callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
				Debug.Log("TangoEvents.SetCallback() Callback was not set!");
            }
            else
            {
				Debug.Log("TangoEvents.SetCallback() Callback was set!");
            }
        }

        private struct EventsAPI
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnTangoEvent(TangoService_onEventAvailable onEventAvaialable);
            #else
            public static int TangoService_connectOnTangoEvent(TangoService_onEventAvailable onEventAvaialable)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            #endif
        }
    }
}