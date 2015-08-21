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
using System.Collections;
using UnityEngine;

public delegate void onUxExceptionEventHandler (Tango.UxExceptionEvent tangoUxEvent);

/// <summary>
/// Tando User Experience exception listener.
/// </summary>
public class UxExceptionEventListener : AndroidJavaProxy
{
    private static UxExceptionEventListener m_instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="UxExceptionEventListener"/> class.
    /// </summary>
    private UxExceptionEventListener (): base("com.google.atap.tango.ux.UxExceptionEventListener")
    {
    }

    private event onUxExceptionEventHandler m_onUxExceptionEvent;

    /// <summary>
    /// Gets the get instance.
    /// </summary>
    /// <value>The get instance.</value>
    public static UxExceptionEventListener GetInstance {
        get {
            if (m_instance == null) {
                m_instance = new UxExceptionEventListener ();
            }
            return m_instance;
        }
    }

    /// <summary>
    /// Registers ux exception events.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void RegisterOnUxExceptionEventHandler (onUxExceptionEventHandler handler)
    {
        if (handler != null) {
            m_onUxExceptionEvent += handler;
        }
    }

    /// <summary>
    /// Unregisters the on too few points.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnUxExceptionEventHandler (onUxExceptionEventHandler handler)
    {
        if (handler != null) {
            m_onUxExceptionEvent -= handler;
        }
    }

    /// <summary>
    /// Called when a Ux Exception Event is dispatched.
    /// </summary>
    /// <param name="tangoUxEvent">A AndroidJavaObject containing information about the exception</param>
    void onUxExceptionEvent(AndroidJavaObject tangoUxEvent)
    {
        if (m_onUxExceptionEvent != null) {
            Tango.UxExceptionEvent uxEvent = new Tango.UxExceptionEvent();
            uxEvent.type = (Tango.TangoUxEnums.UxExceptionEventType) tangoUxEvent.Call<int> ("getType");
            uxEvent.value = tangoUxEvent.Call<float> ("getValue");
            uxEvent.status = (Tango.TangoUxEnums.UxExceptionEventStatus) tangoUxEvent.Call<int> ("getStatus");
            m_onUxExceptionEvent (uxEvent);
        }
    }
}
