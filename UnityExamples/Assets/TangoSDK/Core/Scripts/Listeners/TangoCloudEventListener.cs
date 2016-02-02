//-----------------------------------------------------------------------
// <copyright file="TangoCloudEventListener.cs" company="Google">
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
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango cloud events.
    /// </summary>
    /// <param name="key">Tango cloud event key that happened.</param>
    /// <param name="value">Tango cloud event value that happened.</param>
    /// @cond PRIVATE
    internal delegate void OnTangoCloudEventAvailableEventHandler(int key, int value);

    /// @endcond
    /// <summary>
    /// Calls back tango cloud event listeners.
    /// </summary>
    internal class TangoCloudEventListener
    {
        private OnTangoCloudEventAvailableEventHandler m_onTangoCloudEventAvailable;

        private List<KeyValuePair<int, int>> m_events = new List<KeyValuePair<int, int>>();

        private System.Object m_lockObject = new System.Object();
        private bool m_isDirty = false;
        
        /// <summary>
        /// Sends back a Tango cloud event if there is new data.
        /// </summary>
        internal void SendIfTangoCloudEventAvailable()
        {
            if (m_isDirty && m_onTangoCloudEventAvailable != null)
            {
                lock (m_lockObject)
                {
                    foreach (var cloudEvent in m_events)
                    {
                        m_onTangoCloudEventAvailable(cloudEvent.Key, cloudEvent.Value);
                    }

                    m_events.Clear();
                }

                m_isDirty = false;
            }
        }
        
        /// <summary>
        /// Register a Unity main thread handler for Tango cloud events.
        /// </summary>
        /// <param name="handler">Cloud Event handler to register.</param>
        internal void RegisterOnTangoCloudEventAvailable(OnTangoCloudEventAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoCloudEventAvailable += handler;
            }
        }
        
        /// <summary>
        /// Unregister a Unity main thread handler for the Tango cloud event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnTangoCloudEventAvailable(OnTangoCloudEventAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoCloudEventAvailable -= handler;
            }
        }
        
        /// <summary>
        /// Handle the callback from UnitySendMessage on TangoApplication when a new event is issued.
        /// </summary>
        /// <param name="key">Tango cloud event key.</param>
        /// <param name="value">Tango cloud event value.</param>
        internal void OnCloudEventAvailable(int key, int value)
        {
                lock (m_lockObject)
                {
                    m_events.Add(new KeyValuePair<int, int>(key, value));
                    m_isDirty = true;
                }
            }
    }
}
