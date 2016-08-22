//-----------------------------------------------------------------------
// <copyright file="ITangoLifecycle.cs" company="Google">
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
/// Tango lifecycle interface.
/// </summary>
public interface ITangoLifecycle
{
    /// <summary>
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    void OnTangoPermissions(bool permissionsGranted);

    /// <summary>
    /// This is called when successfully connected to the Tango service.
    /// </summary>
    void OnTangoServiceConnected();

    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    void OnTangoServiceDisconnected();
}