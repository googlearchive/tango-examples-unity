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

namespace Tango
{
    public delegate void OnTangoEventAvailableEventHandler(TangoEvent tangoEvent);

    /// <summary>
    /// Abstract base class that can be used to
    /// automatically register for onEventAvailable
    /// callbacks from the Tango Service.
    /// </summary>
    public class TangoEventListener
    {
        private TangoEvents.TangoService_onEventAvailable m_onEventAvaialableCallback;
        private OnTangoEventAvailableEventHandler m_onTangoEventAvailable;
        private TangoEvent m_previousEvent;
        private bool m_isDirty;

        /// <summary>
        /// Sets the callback.
        /// </summary>
        public virtual void SetCallback()
        {
    		m_onEventAvaialableCallback = new TangoEvents.TangoService_onEventAvailable(_onEventAvailable);
    		TangoEvents.SetCallback(m_onEventAvaialableCallback);
            m_previousEvent = new TangoEvent();
            m_isDirty = false;
        }

		/// <summary>
		/// Sends if tango event available.
		/// </summary>
		/// <param name="usingUXLibrary">If set to <c>true</c> using UX library.</param>
        public void SendIfTangoEventAvailable(bool usingUXLibrary)
		{
			if(m_isDirty)
			{
				if(usingUXLibrary)
				{
					AndroidHelper.ParseTangoEvent(m_previousEvent.timestamp,
					                              (int)m_previousEvent.type,
					                              m_previousEvent.event_key,
					                              m_previousEvent.event_value);
				}

	            if(m_onTangoEventAvailable != null)
	            {
	                m_onTangoEventAvailable(m_previousEvent);
				}

				m_isDirty = true;
			}
        }

        /// <summary>
        /// Registers the on tango event available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoEventAvailable(OnTangoEventAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoEventAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters the on tango event available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoEventAvailable(OnTangoEventAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoEventAvailable -= handler;
            }
        }

        /// <summary>
        /// Handle the callback sent by the Tango Service
        /// when a new event is issued.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="tangoEvent">Tango event.</param>
        protected void _onEventAvailable(IntPtr callbackContext, TangoEvent tangoEvent)
        {
            if(tangoEvent != null)
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