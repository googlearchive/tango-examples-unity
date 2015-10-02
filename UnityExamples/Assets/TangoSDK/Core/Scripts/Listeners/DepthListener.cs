//-----------------------------------------------------------------------
// <copyright file="DepthListener.cs" company="Google">
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
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Delegate for Tango depth events.
    /// </summary>
    /// <param name="tangoDepth">TangoUnityDepth object for the available depth frame.</param>
    internal delegate void OnTangoDepthAvailableEventHandler(TangoUnityDepth tangoDepth);

    /// <summary>
    /// Marshals Tango depth data between the C callbacks in one thread and
    /// the main Unity thread.
    /// </summary>
    internal class DepthListener
    {
        private Tango.DepthProvider.TangoService_onDepthAvailable m_onDepthAvailableCallback;

        /// <summary>
        /// Called when a new Tango depth is available.
        /// </summary>
        private event OnTangoDepthAvailableEventHandler OnTangoDepthAvailable;

        private bool m_isDirty = false;
        private TangoUnityDepth m_tangoDepth;
        private System.Object m_lockObject = new System.Object();
        private float[] m_depthPoints;

        /// <summary>
        /// Register to get Tango depth callbacks.
        /// 
        /// NOTE: Tango depth callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal virtual void SetCallback()
        {
            m_tangoDepth = new TangoUnityDepth();
            m_onDepthAvailableCallback = new Tango.DepthProvider.TangoService_onDepthAvailable(_OnDepthAvailable);
            Tango.DepthProvider.SetCallback(m_onDepthAvailableCallback);
        }

        /// <summary>
        /// Raise a Tango depth event if there is new data.
        /// </summary>
        internal void SendDepthIfAvailable()
        {
            if (m_isDirty && OnTangoDepthAvailable != null)
            {
                lock (m_lockObject)
                {
                    OnTangoDepthAvailable(m_tangoDepth);
                }
                m_isDirty = false;
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoDepthAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                OnTangoDepthAvailable -= handler;
            }
        }

        /// <summary>
        /// Callback that gets called when depth is available from the Tango Service.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="xyzij">Xyzij.</param>
        private void _OnDepthAvailable(IntPtr callbackContext, TangoXYZij xyzij)
        {
            // Fill in the data to draw the point cloud.
            if (xyzij != null)
            {
                lock (m_lockObject)
                {
                    // copy single members
                    m_tangoDepth.m_version = xyzij.version;
                    m_tangoDepth.m_timestamp = xyzij.timestamp;
                    m_tangoDepth.m_ijColumns = xyzij.ij_cols;
                    m_tangoDepth.m_ijRows = xyzij.ij_rows;
                    m_tangoDepth.m_pointCount = xyzij.xyz_count;

                    // deep copy arrays
                    
                    // Fill in the data to draw the point cloud.
                    if (xyzij != null)
                    {
                        int numberOfActivePoints = xyzij.xyz_count * 3;

                        // copy new points
                        if (numberOfActivePoints > 0)
                        {
                            Marshal.Copy(xyzij.xyz[0], m_tangoDepth.m_points, 0, numberOfActivePoints);
                            m_isDirty = true;
                        }
                    }
                }
            }
        }
    }
}