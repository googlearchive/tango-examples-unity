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
using System.Runtime.InteropServices;

namespace Tango
{
    public delegate void OnTangoDepthAvailableEventHandler(TangoUnityDepth tangoDepth);

    /// <summary>
    /// Abstract base class that can be used to
    /// automatically register for onDepthAvailable
    /// callbacks from the Tango Service.
    /// </summary>
    public class DepthListener
    {
        private Tango.DepthProvider.TangoService_onDepthAvailable m_onDepthAvailableCallback;
        private event OnTangoDepthAvailableEventHandler m_onTangoDepthAvailable;
        private bool m_isDirty = false;
        private TangoUnityDepth m_tangoDepth;

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
            if(m_isDirty && m_onTangoDepthAvailable != null)
            {
                m_onTangoDepthAvailable(m_tangoDepth);
                m_isDirty = false;
            }
        }

        /// <summary>
        /// Registers the on tango depth available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoDepthAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters the on tango depth available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoDepthAvailable -= handler;
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
                    int numberOfActiveVertices = xyzij.xyz_count;
                   
                    if(numberOfActiveVertices > 0)
                    {
                        float[] allPositions = new float[numberOfActiveVertices * 3];
                        Marshal.Copy(xyzij.xyz[0], allPositions, 0, allPositions.Length);
                        
                        for(int i = 0; i < numberOfActiveVertices; ++i)
                        {
                            if( i < m_tangoDepth.m_pointCount )
                            {
                                m_tangoDepth.m_vertices[i].x = allPositions[i * 3];
                                m_tangoDepth.m_vertices[i].y = allPositions[(i * 3) + 1];
                                m_tangoDepth.m_vertices[i].z = allPositions[(i * 3) + 2];
                            }
                            else
                            {
                                m_tangoDepth.m_vertices[i].x = m_tangoDepth.m_vertices[i].y = m_tangoDepth.m_vertices[i].z = 0.0f;
                            }
                        }
                        m_isDirty = true;
                    }
                }
            }
        }
    }
}