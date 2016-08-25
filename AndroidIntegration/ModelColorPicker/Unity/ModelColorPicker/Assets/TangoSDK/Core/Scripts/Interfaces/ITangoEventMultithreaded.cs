//-----------------------------------------------------------------------
// <copyright file="ITangoEventMultithreaded.cs" company="Google">
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
/// Tango event interface where the handler will be invoked from multiple threads.
/// 
/// Use this if you want to get the events as soon as they happen.  The handler will be invoked as soon as the
/// event happens, even if that is in another thread.  You must make sure your handler is thread-safe.
/// </summary>
public interface ITangoEventMultithreaded
{
    /// <summary>
    /// This is called each time a Tango event happens.
    /// </summary>
    /// <param name="tangoEvent">Tango event.</param>
    void OnTangoEventMultithreadedAvailableEventHandler(Tango.TangoEvent tangoEvent);
}
