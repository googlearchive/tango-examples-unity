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
using UnityEngine;
using Tango;

/// <summary>
/// Abstract base class that can be used to
/// automatically register for onPoseAvailable
/// callbacks from the Tango Service.
/// </summary>
public abstract class PoseListener : MonoBehaviour
{
    public Tango.PoseProvider.TangoService_onPoseAvailable m_poseAvailableCallback;

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="PoseListener"/>
    /// is using auto reset.
    /// </summary>
    /// <value><c>true</c> if auto reset; otherwise, <c>false</c>.</value>
    public bool AutoReset
    {
        get;
        set;
    }

    /// <summary>
    /// Registers the callback.
    /// </summary>
    /// <param name="framePairs">Frame pairs.</param>
    public virtual void SetCallback(TangoCoordinateFramePair[] framePairs)
    {
        m_poseAvailableCallback = new Tango.PoseProvider.TangoService_onPoseAvailable(_OnPoseAvailable);
        Tango.PoseProvider.SetCallback(framePairs, m_poseAvailableCallback);
    }

    /// <summary>
    /// Handle the callback sent by the Tango Service
    /// when a new pose is sampled.
    /// </summary>
    /// <param name="callbackContext">Callback context.</param>
    /// <param name="pose">Pose.</param>
    protected abstract void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose);
}