//-----------------------------------------------------------------------
// <copyright file="ITangoCloudEvent.cs" company="Google">
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
/// Cloud Event notification interface.
/// </summary>
internal interface ITangoCloudEvent
{
    /// <summary>
    /// This is called each time a Tango cloud event happens.
    /// </summary>
    /// <param name="key">Tango cloud event key.</param>
    /// <param name="value">Tango cloud event value.</param>
    void OnTangoCloudEventAvailableEventHandler(int key, int value);
}
