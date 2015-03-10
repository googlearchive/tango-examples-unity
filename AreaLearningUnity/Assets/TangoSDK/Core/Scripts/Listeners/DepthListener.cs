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
/// automatically register for onDepthAvailable
/// callbacks from the Tango Service.
/// </summary>
public abstract class DepthListener : MonoBehaviour
{
    private Tango.DepthProvider.TangoService_onDepthAvailable m_onDepthAvailableCallback;
    
    /// <summary>
    /// Register this class to receive the OnDepthAvailable callback.
    /// </summary>
    public virtual void SetCallback()
    {
        m_onDepthAvailableCallback = new Tango.DepthProvider.TangoService_onDepthAvailable(_OnDepthAvailable);
		Tango.DepthProvider.SetCallback(m_onDepthAvailableCallback);
    }

    /// <summary>
    /// Callback that gets called when depth is available
    /// from the Tango Service.
    /// </summary>
    /// <param name="callbackContext">Callback context.</param>
    /// <param name="xyzij">Xyzij.</param>
    protected abstract void _OnDepthAvailable(IntPtr callbackContext, TangoXYZij xyzij);
}