//-----------------------------------------------------------------------
// <copyright file="AreaDescriptionEventListener.cs" company="Google">
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
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango Area Description import events.
    /// 
    /// The UUID is empty when it's an export call.
    /// </summary>
    /// <param name="isSuccessful">If the import operation is successful.</param>
    /// <param name="areaDescription">The imported Area Description.</param>
    internal delegate void OnAreaDescriptionImportEventHandler(bool isSuccessful, AreaDescription areaDescription);

    /// <summary>
    /// Delegate for Tango Area Description export events.
    /// </summary>
    /// <param name="isSuccessful">If the export operation is successful.</param>
    internal delegate void OnAreaDescriptionExportEventHandler(bool isSuccessful);

    /// <summary>
    /// The Area Description event is responsible for listening the callback from Area Description import and export.
    /// </summary>
    internal static class AreaDescriptionEventListener
    {
        /// <summary>
        /// The lock object used as a mutex.
        /// </summary>
        private static System.Object m_lockObject = new System.Object();

        /// <summary>
        /// If <c>true</c>, a callback has been set up, otherwise <c>false</c>.
        /// </summary>
        private static bool m_isCallbackSet;

        private static bool m_isImportFinished;
        private static bool m_isExportFinished;
        private static string m_eventString;
        private static bool m_isSuccessful;

        /// <summary>
        /// Called when import ADF file is finished.
        /// </summary>
        private static OnAreaDescriptionImportEventHandler m_onTangoAreaDescriptionImported;

        /// <summary>
        /// Called when export ADF file is finished.
        /// </summary>
        private static OnAreaDescriptionExportEventHandler m_onTangoAreaDescriptionExported;

        /// <summary>
        /// Initializes the <see cref="Tango.AreaDescriptionEventListener"/> class.
        /// </summary>
        static AreaDescriptionEventListener()
        {
            Reset();
        }

        /// <summary>
        /// Stop getting Area Description callbacks, clear all listeners.
        /// </summary>
        internal static void Reset()
        {
            m_isCallbackSet = false;
            AndroidHelper.UnregisterOnActivityResultEvent(_OnActivityResult);

            m_isImportFinished = false;
            m_isExportFinished = false;
            m_eventString = String.Empty;
            m_isSuccessful = false;
            m_onTangoAreaDescriptionImported = null;
            m_onTangoAreaDescriptionExported = null;
        }

        /// <summary>
        /// Setup getting Area Description callbacks from onActivityResult.
        /// </summary>
        internal static void SetCallback()
        {
            if (m_isCallbackSet)
            {
                Debug.Log("AreaDescriptionEventListener.SetCallback() called when callback is already set.");
                return;
            }

            Debug.Log("AreaDescriptionEventListener.SetCallback()");
            m_isCallbackSet = true;
            AndroidHelper.RegisterOnActivityResultEvent(_OnActivityResult);
        }

        /// <summary>
        /// Raise a Tango Area Description event if there is new data.
        /// </summary>
        internal static void SendIfAvailable()
        {
            if (!m_isCallbackSet)
            {
                return;
            }

            if (m_onTangoAreaDescriptionExported != null && m_onTangoAreaDescriptionImported != null)
            {
                lock (m_lockObject)
                {
                    if (m_isImportFinished)
                    {
                        m_onTangoAreaDescriptionImported(m_isSuccessful, AreaDescription.ForUUID(m_eventString));
                        m_isImportFinished = false;
                        m_eventString = string.Empty;
                    }

                    if (m_isExportFinished)
                    {
                        m_onTangoAreaDescriptionExported(m_isSuccessful);
                        m_isExportFinished = false;
                    }
                }
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango Area Description event.
        /// </summary>
        /// <param name="importHandler">Event handler for import function.</param>
        /// <param name="exportHandler">Event handler for export function.</param>
        internal static void Register(OnAreaDescriptionImportEventHandler importHandler,
                                      OnAreaDescriptionExportEventHandler exportHandler)
        {
            if (exportHandler != null)
            {
                m_onTangoAreaDescriptionExported += exportHandler;
            }

            if (importHandler != null)
            {
                m_onTangoAreaDescriptionImported += importHandler;
            }
        }

        /// <summary>
        /// Unregisters a Unity main thread handler for the Tango Area Description event.
        /// </summary>
        /// <param name="importHandler">Event handler for import function.</param>
        /// <param name="exportHandler">Event handler for export function.</param>
        internal static void Unregister(OnAreaDescriptionImportEventHandler importHandler,
                                        OnAreaDescriptionExportEventHandler exportHandler)
        {
            if (exportHandler != null)
            {
                m_onTangoAreaDescriptionExported -= exportHandler;
            }

            if (importHandler != null)
            {
                m_onTangoAreaDescriptionImported -= importHandler;
            }
        }

        /// <summary>
        /// EventHandler for Android's on activity result.
        /// </summary>
        /// <param name="requestCode">Request code.</param>
        /// <param name="resultCode">Result code.</param>
        /// <param name="data">Intent data that returned from the activity.</param>
        private static void _OnActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
        {
            if (requestCode == Tango.Common.TANGO_ADF_IMPORT_REQUEST_CODE)
            {
                lock (m_lockObject)
                {
                    m_isImportFinished = true;
                    m_isSuccessful = resultCode == (int)Common.AndroidResult.SUCCESS;

                    if (m_isSuccessful && data != null)
                    {
                        m_eventString = data.Call<string>("getStringExtra", "DESTINATION_UUID");
                    }
                    else
                    {
                        m_eventString = string.Empty;
                    }
                }
            }

            if (requestCode == Tango.Common.TANGO_ADF_EXPORT_REQUEST_CODE)
            {
                lock (m_lockObject)
                {
                    m_isExportFinished = true;
                    m_isSuccessful = resultCode == (int)Common.AndroidResult.SUCCESS;
                    m_eventString = string.Empty;
                }
            }
        }
    }
}