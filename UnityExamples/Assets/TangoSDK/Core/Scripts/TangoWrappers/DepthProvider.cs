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
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Wraps depth related Tango Service functionality.
    /// </summary>
    public class DepthProvider
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TangoService_onDepthAvailable(IntPtr callbackContext, [In,Out] TangoXYZij xyzij);

        /// <summary>
        /// Sets the callback that is called when new depth
        /// points have been sampled by the Tango Service.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public static void SetCallback(TangoService_onDepthAvailable callback)
        {
            int returnValue = DepthAPI.TangoService_connectOnXYZijAvailable(callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
				Debug.Log("DepthProvider.SetCallback() Callback was not set!");
            }
            else
            {
                Debug.Log("DepthProvider.SetCallback() OnDepth callback was set!");
            }
        }

        /// <summary>
        /// Wraps depth functionality from Tango Service.
        /// </summary>
        private struct DepthAPI
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnXYZijAvailable(TangoService_onDepthAvailable onDepthAvailalble);

 #else
            public static int TangoService_connectOnXYZijAvailable(TangoService_onDepthAvailable onDepthAvailalble)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
    }
}
