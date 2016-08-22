//-----------------------------------------------------------------------
// <copyright file="TangoEventListener.cs" company="Google">
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

namespace Tango
{
    using System;
    using UnityEngine;

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
        private OnTangoEventAvailableEventHandler m_onTangoEventMultithreadedAvailable;
        private TangoEvent m_tangoEvent;
        private System.Object m_lockObject = new System.Object();
        private bool m_isDirty;

        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// NOTE: Tango event callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal void SetCallback()
        {
            m_onEventAvaialableCallback = new TangoEvents.TangoService_onEventAvailable(_onEventAvailable);
            TangoEvents.SetCallback(m_onEventAvaialableCallback);
            m_tangoEvent = new TangoEvent();
            m_isDirty = false;
        }

        /// <summary>
        /// Raise a Tango event if there is new data.
        /// </summary>
        internal void SendIfTangoEventAvailable()
        {
            if (m_isDirty && m_onTangoEventAvailable != null)
            {
                lock (m_lockObject)
                {
                    m_onTangoEventAvailable(m_tangoEvent);
                }

                m_isDirty = false;
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
        /// Register a multithread handler for Tango events.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterOnTangoEventMultithreadedAvailable(OnTangoEventAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoEventMultithreadedAvailable += handler;
            }
        }

        /// <summary>
        /// Unregister a multithread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnTangoEventMultithreadedAvailable(OnTangoEventAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoEventMultithreadedAvailable -= handler;
            }
        }

        /// <summary>
        /// Handle the callback sent by the Tango Service when a new event is issued.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="tangoEvent">Tango event.</param>
        private void _onEventAvailable(IntPtr callbackContext, TangoEvent tangoEvent)
        {
            if (tangoEvent != null)
            {
                if (m_onTangoEventMultithreadedAvailable != null)
                {
                    m_onTangoEventMultithreadedAvailable(tangoEvent);
                }

                lock (m_lockObject)
                {
                    m_tangoEvent.timestamp = tangoEvent.timestamp;
                    m_tangoEvent.type = tangoEvent.type;
                    m_tangoEvent.event_key = tangoEvent.event_key;
                    m_tangoEvent.event_value = tangoEvent.event_value;
                    m_isDirty = true;
                }
            }
        }
    }
}