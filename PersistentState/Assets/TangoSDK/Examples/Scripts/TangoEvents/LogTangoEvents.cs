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
using UnityEngine;
using System;
using System.Collections;
using Tango;

/// <summary>
/// Log tango events to console.
/// </summary>
public class LogTangoEvents : TangoEventListener
{
    private string m_lastTangoEventIssued;

	// Use this for initialization
	void Start ()
	{
        m_lastTangoEventIssued = string.Empty;
	}
	
    protected override void _onEventAvailable(IntPtr callbackContext, TangoEvent tangoEvent)
    {
        Debug.Log("Tango event fired : " + tangoEvent.event_value);
    }
}
