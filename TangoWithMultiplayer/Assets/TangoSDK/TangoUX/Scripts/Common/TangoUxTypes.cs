//-----------------------------------------------------------------------
// <copyright file="TangoUxTypes.cs" company="Google">
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
using System;
using System.Runtime.InteropServices;
using UnityEngine;

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
                                                         Justification = "Types file.")]

namespace Tango
{
    /// <summary>
    /// Represents a Tango UX Exception Event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UxExceptionEvent
    {
        /// <summary>
        /// The type for this UX Exception Event.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoUxEnums.UxExceptionEventType type;

        /// <summary>
        /// The event value for this UX Exception Event.
        /// </summary>
        [MarshalAs(UnmanagedType.R4)]
        public float value;

        /// <summary>
        /// The status for this UX Exception Event.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public TangoUxEnums.UxExceptionEventStatus status;
    }
}
