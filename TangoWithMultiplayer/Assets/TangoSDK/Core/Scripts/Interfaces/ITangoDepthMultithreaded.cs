//-----------------------------------------------------------------------
// <copyright file="ITangoDepthMultithreaded.cs" company="Google">
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
using System.Collections;
using UnityEngine;

/// <summary>
/// Tango depth interface where the handler will be invoked from multiple threads.
/// 
/// Use this if you want to get the depth as soon as it is available with minimal processing.  The handler will be
/// invoked as soon as depth is available, even if that is in another thread.  The handler will be passed the raw
/// <c>TangoXYZij</c> instead of the more friendly TangoUnityDepth.  You must make sure your handler is thread-safe.
/// </summary>
public interface ITangoDepthMultithreaded
{
    /// <summary>
    /// This is called each time new depth data is available.
    /// 
    /// On the Tango tablet, the depth callback occurs at 5 Hz.
    /// </summary>
    /// <param name="tangoDepth">Tango depth.</param>
    void OnTangoDepthMultithreadedAvailable(Tango.TangoXYZij tangoDepth);
}