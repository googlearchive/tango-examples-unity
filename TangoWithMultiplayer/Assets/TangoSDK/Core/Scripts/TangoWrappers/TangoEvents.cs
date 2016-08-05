//-----------------------------------------------------------------------
// <copyright file="TangoEvents.cs" company="Google">
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
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// C API wrapper for the Tango events interface.
    /// </summary>
    internal class TangoEvents
    {
        /// <summary>
        /// Tango video overlay C callback function signature.
        /// </summary>
        /// <param name="callbackContext">The callback context.</param>
        /// <param name="tangoEvent">Tango event.</param> 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onEventAvailable(IntPtr callbackContext, [In, Out] TangoEvent tangoEvent);
            
        /// <summary>
        /// Attach an onTangoEvent callback. The callback is called each time a Tango event happens.
        /// </summary>
        /// <param name="callback">Callback method.</param>
        internal static void SetCallback(TangoService_onEventAvailable callback)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct EventsAPI
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
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