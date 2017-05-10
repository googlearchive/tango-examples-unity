//-----------------------------------------------------------------------
// <copyright file="ITangoVideoOverlayMultithreaded.cs" company="Google">
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
/// Tango video overlay interface where the handler will be invoked from multiple threads.
///
/// Use this if you want to get the image callbacks as soon as they are available.  The handler will be invoked as
/// soon as the event happens, even if that is in another thread.  You must make sure your handler is thread-safe.
/// </summary>
public interface ITangoVideoOverlayMultithreaded
{
    /// <summary>
    /// This will be called when a new frame is available from the camera.
    ///
    /// The first scan-line of the color image is reserved for metadata instead of image pixels.
    /// </summary>
    /// <param name="cameraId">Camera identifier.</param>
    /// <param name="image">Image buffer.</param>
    /// <param name="cameraMetadata">Camera metadata.</param>
    void OnTangoImageMultithreadedAvailable(Tango.TangoEnums.TangoCameraId cameraId,
                                            Tango.TangoImage image, Tango.TangoCameraMetadata cameraMetadata);
}
