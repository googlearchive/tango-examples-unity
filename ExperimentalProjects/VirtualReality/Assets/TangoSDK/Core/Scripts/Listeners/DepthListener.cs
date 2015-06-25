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
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// On tango depth available event handler.
    /// </summary>
    /// <param name="tangoDepth">TangoUnityDepth object for the available depth frame.</param>
    public delegate void OnTangoDepthAvailableEventHandler(TangoUnityDepth tangoDepth);

    /// <summary>
    /// Abstract base class that can be used to
    /// automatically register for onDepthAvailable
    /// callbacks from the Tango Service.
    /// </summary>
    public class DepthListener
    {
        private Tango.DepthProvider.TangoService_onDepthAvailable m_onDepthAvailableCallback;

        /// <summary>
        /// Occurs when m_on tango depth available.
        /// </summary>
        private event OnTangoDepthAvailableEventHandler m_OnTangoDepthAvailable;

        private bool m_isDirty = false;
        private TangoUnityDepth m_tangoDepth;
        private System.Object m_lockObject = new System.Object();
        private float[] m_depthPoints;

        /// <summary>
        /// Register this class to receive the OnDepthAvailable callback.
        /// </summary>
        public virtual void SetCallback()
        {
            m_tangoDepth = new TangoUnityDepth();
            m_onDepthAvailableCallback = new Tango.DepthProvider.TangoService_onDepthAvailable(_OnDepthAvailable);
    		Tango.DepthProvider.SetCallback(m_onDepthAvailableCallback);
        }

        /// <summary>
        /// Sends the depth if available.
        /// </summary>
        public void SendDepthIfAvailable()
        {
            if (m_isDirty && m_OnTangoDepthAvailable != null)
            {
                lock (m_lockObject)
                {
                    m_OnTangoDepthAvailable(m_tangoDepth);
                }
                m_isDirty = false;
            }
        }

        /// <summary>
        /// Registers the on tango depth available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_OnTangoDepthAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters the on tango depth available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_OnTangoDepthAvailable -= handler;
            }
        }

        /// <summary>
        /// Callback that gets called when depth is available
        /// from the Tango Service.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="xyzij">Xyzij.</param>
        protected void _OnDepthAvailable(IntPtr callbackContext, TangoXYZij xyzij)
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