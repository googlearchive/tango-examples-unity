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
    internal static class TangoEventListener
    {
        /// <summary>
        /// The lock object used as a mutex.
        /// </summary>
        private static System.Object m_lockObject = new System.Object();

        private static TangoEventProvider.APIOnEventAvailable m_onEventAvailableCallback;
        private static OnTangoEventAvailableEventHandler m_onTangoEventAvailable;
        private static OnTangoEventAvailableEventHandler m_onTangoEventMultithreadedAvailable;
        private static TangoEvent m_tangoEvent;
        private static bool m_isDirty;

        /// <summary>
        /// Initializes the <see cref="Tango.TangoEventListener"/> class.
        /// </summary>
        static TangoEventListener()
        {
            Reset();
        }

        /// <summary>
        /// Stop getting Tango event callbacks.
        /// </summary>
        internal static void Reset()
        {
            // Avoid calling into tango_client_api before the correct library is loaded.
            if (m_onEventAvailableCallback != null)
            {
                TangoEventProvider.ClearCallback();
            }

            m_onEventAvailableCallback = null;
            m_onTangoEventAvailable = null;
            m_onTangoEventMultithreadedAvailable = null;
            m_tangoEvent = new TangoEvent();
            m_isDirty = false;
        }

        /// <summary>
        /// Register to get Tango event callbacks.
        /// 
        /// NOTE: Tango event callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal static void SetCallback()
        {
            if (m_onEventAvailableCallback != null)
            {
                Debug.Log("TangoEventListener.SetCallback() called when callback is already set.");
                return;
            }

            Debug.Log("TangoEventListener.SetCallback()");
            m_onEventAvailableCallback = new TangoEventProvider.APIOnEventAvailable(_OnEventAvailable);
            TangoEventProvider.SetCallback(m_onEventAvailableCallback);
        }

        /// <summary>
        /// Raise a Tango event if there is new data.
        /// </summary>
        internal static void SendIfAvailable()
        {
            if (m_onEventAvailableCallback == null)
            {
                return;
            }

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
        internal static void RegisterOnTangoEventAvailable(OnTangoEventAvailableEventHandler handler)
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
        internal static void UnregisterOnTangoEventAvailable(OnTangoEventAvailableEventHandler handler)
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
        internal static void RegisterOnTangoEventMultithreadedAvailable(OnTangoEventAvailableEventHandler handler)
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
        internal static void UnregisterOnTangoEventMultithreadedAvailable(OnTangoEventAvailableEventHandler handler)
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
        [AOT.MonoPInvokeCallback(typeof(TangoEventProvider.APIOnEventAvailable))]
        private static void _OnEventAvailable(IntPtr callbackContext, TangoEvent tangoEvent)
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