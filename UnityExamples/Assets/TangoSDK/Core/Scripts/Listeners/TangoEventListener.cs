//-----------------------------------------------------------------------
// <copyright file="TangoEventListener.cs" company="Google">
//
// Copyright 2015 Google Inc. All Rights Reserved.
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
using System;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// IDelegate for Tango event events.
    /// </summary>
    /// <param name="tangoEvent">Tango event that happened.</param>
    internal delegate void OnTangoEventAvailableEventHandler(TangoEvent tangoEvent);

    /// <summary>
    /// Marshals Tango event data between the C callbacks in one thread and the main Unity thread.
    /// </summary>
    internal class TangoEventListener
    {
        private TangoEvents.TangoService_onEventAvailable m_onEventAvaialableCallback;
        private OnTangoEventAvailableEventHandler m_onTangoEventAvailable;
        private TangoEvent m_previousEvent;
        private bool m_isDirty;

        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// NOTE: Tango event callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal virtual void SetCallback()
        {
            m_onEventAvaialableCallback = new TangoEvents.TangoService_onEventAvailable(_onEventAvailable);
            TangoEvents.SetCallback(m_onEventAvaialableCallback);
            m_previousEvent = new TangoEvent();
            m_isDirty = false;
        }

        /// <summary>
        /// Raise a Tango event if there is new data.
        /// </summary>
        internal void SendIfTangoEventAvailable()
        {
            if (m_isDirty)
            {
                if (m_onTangoEventAvailable != null)
                {
                    m_onTangoEventAvailable(m_previousEvent);
                }

                m_isDirty = true;
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for Tango events.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterOnTangoEventAvailable(OnTangoEventAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoEventAvailable += handler;
            }
        }

        /// <summary>
        /// Unregister a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnTangoEventAvailable(OnTangoEventAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoEventAvailable -= handler;
            }
        }

        /// <summary>
        /// DEPRECATED: Handle the callback sent by the Tango Service
        /// when a new event is issued.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="tangoEvent">Tango event.</param>
        protected void _onEventAvailable(IntPtr callbackContext, TangoEvent tangoEvent)
        {
            if (tangoEvent != null)
            {
                m_previousEvent.timestamp = tangoEvent.timestamp;
                m_previousEvent.type = tangoEvent.type;
                m_previousEvent.event_key = tangoEvent.event_key;
                m_previousEvent.event_value = tangoEvent.event_value;
                m_isDirty = true;
            }
        }
    }
}