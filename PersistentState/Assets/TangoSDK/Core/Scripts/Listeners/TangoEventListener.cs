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
/// automatically register for onEventAvailable
/// callbacks from the Tango Service.
/// </summary>
public abstract class TangoEventListener : MonoBehaviour
{
    public TangoEvents.TangoService_onEventAvailable m_onEventAvaialableCallback;

    /// <summary>
    /// Sets the callback.
    /// </summary>
    public virtual void SetCallback()
    {
		m_onEventAvaialableCallback = new TangoEvents.TangoService_onEventAvailable(_onEventAvailable);
		TangoEvents.SetCallback(m_onEventAvaialableCallback);
		Debug.Log("------------------------Tango event callback set!");
    }

    /// <summary>
    /// Handle the callback sent by the Tango Service
    /// when a new event is issued.
    /// </summary>
    /// <param name="callbackContext">Callback context.</param>
    /// <param name="tangoEvent">Tango event.</param>
    protected abstract void _onEventAvailable(IntPtr callbackContext, TangoEvent tangoEvent);
}