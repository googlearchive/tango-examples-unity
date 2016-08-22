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
    internal class AreaDescriptionEventListener
    {
        private bool m_isImportFinished = false;
        private bool m_isExportFinished = false;
        private System.Object m_lockObject = new System.Object();
        private string m_eventString;
        private bool m_isSuccessful = false;

        /// <summary>
        /// AreaDescriptionEventListener constructor.
        /// 
        /// The activity result callback is registered when the listener is initialized.
        /// </summary>
        public AreaDescriptionEventListener()
        {
            AndroidHelper.RegisterOnActivityResultEvent(_androidOnActivityResult);
        }

        /// <summary>
        /// Called when import ADF file is finished.
        /// </summary>
        private event OnAreaDescriptionImportEventHandler OnTangoAreaDescriptionImported;

        /// <summary>
        /// Called when export ADF file is finished.
        /// </summary>
        private event OnAreaDescriptionExportEventHandler OnTangoAreaDescriptionExported;

        /// <summary>
        /// Raise a Tango Area Description event if there is new data.
        /// </summary>
        internal void SendEventIfAvailable()
        {
            if (OnTangoAreaDescriptionExported != null && OnTangoAreaDescriptionImported != null)
            {
                lock (m_lockObject)
                {
                    if (m_isImportFinished)
                    {
                        OnTangoAreaDescriptionImported(m_isSuccessful, AreaDescription.ForUUID(m_eventString));
                        m_isImportFinished = false;
                        m_eventString = string.Empty;
                    }

                    if (m_isExportFinished)
                    {
                        OnTangoAreaDescriptionExported(m_isSuccessful);
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
        internal void Register(OnAreaDescriptionImportEventHandler importHandler,
                               OnAreaDescriptionExportEventHandler exportHandler)
        {
            if (exportHandler != null)
            {
                OnTangoAreaDescriptionExported += exportHandler;
            }

            if (importHandler != null)
            {
                OnTangoAreaDescriptionImported += importHandler;
            }
        }

        /// <summary>
        /// Unregisters a Unity main thread handler for the Tango Area Description event.
        /// </summary>
        /// <param name="importHandler">Event handler for import function.</param>
        /// <param name="exportHandler">Event handler for export function.</param>
        internal void Unregister(OnAreaDescriptionImportEventHandler importHandler,
                                 OnAreaDescriptionExportEventHandler exportHandler)
        {
            if (exportHandler != null)
            {
                OnTangoAreaDescriptionExported -= exportHandler;
            }

            if (importHandler != null)
            {
                OnTangoAreaDescriptionImported -= importHandler;
            }
        }

        /// <summary>
        /// EventHandler for Android's on activity result.
        /// </summary>
        /// <param name="requestCode">Request code.</param>
        /// <param name="resultCode">Result code.</param>
        /// <param name="data">Intent data that returned from the activity.</param>
        private void _androidOnActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
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