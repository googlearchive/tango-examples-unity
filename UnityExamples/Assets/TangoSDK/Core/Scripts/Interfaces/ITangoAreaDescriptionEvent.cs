//-----------------------------------------------------------------------
// <copyright file="ITangoAreaDescriptionEvent.cs" company="Google">
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
/// Tango Area Description event interface.
/// </summary>
public interface ITangoAreaDescriptionEvent
{
    /// <summary>
    /// This is called when the Area Description export operation completes.
    /// </summary>
    /// <param name="isSuccessful">If the export operation completed successfully.</param>
    void OnAreaDescriptionExported(bool isSuccessful);

    /// <summary>
    /// This is called when the Area Description import operation completes.
    /// 
    /// Please note that the Tango Service can only load Area Description file from internal storage.
    /// </summary>
    /// <param name="isSuccessful">If the import operation completed successfully.</param>
    /// <param name="areaDescription">The imported Area Description.</param>
    void OnAreaDescriptionImported(bool isSuccessful, Tango.AreaDescription areaDescription);
}
