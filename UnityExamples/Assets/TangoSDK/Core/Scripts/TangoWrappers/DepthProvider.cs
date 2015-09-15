//-----------------------------------------------------------------------
// <copyright file="DepthProvider.cs" company="Google">
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
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// C API wrapper for the Tango depth interface.
    /// </summary>
    internal class DepthProvider
    {
        /// <summary>
        /// Tango depth C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="xyzij">Depth information.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onDepthAvailable(IntPtr callbackContext, [In, Out] TangoXYZij xyzij);

        /// <summary>
        /// Set the C callback for the Tango depth interface.
        /// </summary>
        /// <param name="callback">Callback.</param>
        internal static void SetCallback(TangoService_onDepthAvailable callback)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
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
