//-----------------------------------------------------------------------
// <copyright file="UxExceptionEventListener.cs" company="Google">
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
/// Tango User Experience exception listener.
/// </summary>
public class UxExceptionEventListener : AndroidJavaProxy
{
    private static UxExceptionEventListener m_instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="UxExceptionEventListener"/> class.
    /// </summary>
    private UxExceptionEventListener() : base("com.google.atap.tango.ux.UxExceptionEventListener")
    {
    }

    /// <summary>
    /// Delegate for UX Exception events.
    /// </summary>
    /// <param name="tangoUxEvent">The exception event from Tango.</param>
    public delegate void OnUxExceptionEventHandler(Tango.UxExceptionEvent tangoUxEvent);

    /// <summary>
    /// Occurs when a UX Exception event happens.
    /// </summary>
    private event OnUxExceptionEventHandler OnUxExceptionEvent;

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static UxExceptionEventListener GetInstance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new UxExceptionEventListener();
            }

            return m_instance;
        }
    }

    /// <summary>
    /// Registers UX Exception event handler.
    /// </summary>
    /// <param name="handler">Event handler.</param>
    public void RegisterOnUxExceptionEventHandler(OnUxExceptionEventHandler handler)
    {
        if (handler != null)
        {
            OnUxExceptionEvent += handler;
        }
    }

    /// <summary>
    /// Unregisters the on too few points.
    /// </summary>
    /// <param name="handler">Event handler.</param>
    public void UnregisterOnUxExceptionEventHandler(OnUxExceptionEventHandler handler)
    {
        if (handler != null)
        {
            OnUxExceptionEvent -= handler;
        }
    }

    /// <summary>
    /// Called when a UX Exception event is dispatched.
    /// </summary>
    /// <param name="tangoUxEvent">A AndroidJavaObject containing information about the exception.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Called from Java.")]
    private void onUxExceptionEvent(AndroidJavaObject tangoUxEvent)
    {
        if (OnUxExceptionEvent != null)
        {
            Tango.UxExceptionEvent uxEvent = new Tango.UxExceptionEvent();
            uxEvent.type = (Tango.TangoUxEnums.UxExceptionEventType)tangoUxEvent.Call<int>("getType");
            uxEvent.value = tangoUxEvent.Call<float>("getValue");
            uxEvent.status = (Tango.TangoUxEnums.UxExceptionEventStatus)tangoUxEvent.Call<int>("getStatus");
            OnUxExceptionEvent(uxEvent);
        }
    }
}
