//-----------------------------------------------------------------------
// <copyright file="DepthListener.cs" company="Google">
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
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName",
    Justification = "Files can start with an interface.")]

namespace Tango
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango point cloud events.
    /// </summary>
    /// <param name="pointCloud">The point cloud data from Tango.</param>
    internal delegate void OnPointCloudAvailableEventHandler(TangoPointCloudData pointCloud);

    /// <summary>
    /// Delegate for Tango point cloud events that can be called on any thread.
    /// </summary>
    /// <param name="pointCloud">The point cloud data from Tango.</param>
    internal delegate void OnPointCloudMultithreadedAvailableEventHandler(ref TangoPointCloudIntPtr pointCloud);

    /// <summary>
    /// DEPRECATED: Delegate for Tango depth events.
    /// </summary>
    /// <param name="tangoDepth">TangoUnityDepth object for the available depth frame.</param>
    internal delegate void OnTangoDepthAvailableEventHandler(TangoUnityDepth tangoDepth);

    /// <summary>
    /// DEPRECATED: Delegate for Tango depth event that can be called on any thread.
    /// </summary>
    /// <param name="tangoDepth"><c>TangoXYZij</c> object for the available depth frame.</param>
    internal delegate void OnTangoDepthMulithreadedAvailableEventHandler(TangoXYZij tangoDepth);

    /// <summary>
    /// The interface of instance wrapper for the static DepthListener class.
    /// </summary>
    internal interface IDepthListenerWrapper
    {
        /// <summary>
        /// Set an upper limit on the number of points in the point cloud.
        /// Hopefully a temporary workaround 'till this is implemented as an option C-side.
        /// </summary>
        /// <param name="maxDepthPoints">Max points.</param>
        void SetPointCloudLimit(int maxDepthPoints);
    }

    /// <summary>
    /// Marshals Tango depth data between the C callbacks in one thread and
    /// the main Unity thread.
    /// </summary>
    internal static class DepthListener
    {
        /// <summary>
        /// The lock object used as a mutex.
        /// </summary>
        private static System.Object m_lockObject = new System.Object();

        private static Tango.DepthProvider.APIOnPointCloudAvailable m_onPointCloudAvailableCallback;

        private static bool m_isDirty;
        private static TangoPointCloudData m_pointCloud;
        private static float[] m_xyzPoints;

        /// <summary>
        /// Maximum number of depth points DepthListener will pass on from the tango service.
        /// If value is 0, no limit is imposed.
        /// </summary>
        private static int m_maxNumReducedDepthPoints;

        /// <summary>
        /// Called when a new Tango depth is available.
        /// </summary>
        private static OnTangoDepthAvailableEventHandler m_onTangoDepthAvailable;

        /// <summary>
        /// Called when a new Tango depth is available on the thread the depth came from.
        /// </summary>
        private static OnTangoDepthMulithreadedAvailableEventHandler m_onTangoDepthMultithreadedAvailable;

        /// <summary>
        /// Called when a new PointCloud is available.
        /// </summary>
        private static OnPointCloudAvailableEventHandler m_onPointCloudAvailable;

        /// <summary>
        /// Called when a new PointCloud is available on the thread the point cloud came from.
        /// </summary>
        private static OnPointCloudMultithreadedAvailableEventHandler m_onPointCloudMultithreadedAvailable;

        /// <summary>
        /// Initializes the <see cref="Tango.DepthListener"/> class.
        /// </summary>
        static DepthListener()
        {
            Reset();
        }

        /// <summary>
        /// Stop getting Tango depth callbacks, clear all listeners.
        /// </summary>
        internal static void Reset()
        {
            // Avoid calling into tango_client_api before the correct library is loaded.
            if (m_onPointCloudAvailableCallback != null)
            {
                Tango.DepthProvider.ClearCallback();
            }

            m_onPointCloudAvailableCallback = null;
            m_isDirty = false;
            m_pointCloud = new TangoPointCloudData();
            m_pointCloud.m_points = new float[Common.MAX_NUM_POINTS * 4];
            m_xyzPoints = new float[Common.MAX_NUM_POINTS * 3];
            m_maxNumReducedDepthPoints = 0;
            m_onTangoDepthAvailable = null;
            m_onTangoDepthMultithreadedAvailable = null;
            m_onPointCloudAvailable = null;
            m_onPointCloudMultithreadedAvailable = null;
        }

        /// <summary>
        /// Register to get Tango depth callbacks.
        ///
        /// NOTE: Tango depth callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal static void SetCallback()
        {
            if (m_onPointCloudAvailableCallback != null)
            {
                Debug.Log("DepthListener.SetCallback() called when callback is already set.");
                return;
            }

            Debug.Log("DepthListener.SetCallback()");
            m_onPointCloudAvailableCallback = new DepthProvider.APIOnPointCloudAvailable(_OnPointCloudAvailable);
            Tango.DepthProvider.SetCallback(m_onPointCloudAvailableCallback);
        }

        /// <summary>
        /// Raise a Tango depth event if there is new data.
        /// </summary>
        internal static void SendIfAvailable()
        {
            if (m_onPointCloudAvailableCallback == null)
            {
                return;
            }

#if UNITY_EDITOR
            lock (m_lockObject)
            {
                if (DepthProvider.m_emulationIsDirty)
                {
                    DepthProvider.m_emulationIsDirty = false;

                    if (m_onTangoDepthAvailable != null || m_onTangoDepthMultithreadedAvailable != null
                        || m_onPointCloudAvailable != null | m_onPointCloudMultithreadedAvailable != null)
                    {
                        _FillEmulatedPointCloud(ref m_pointCloud);
                    }

                    if (m_onTangoDepthMultithreadedAvailable != null)
                    {
                        // Pretend to be making a call from unmanaged code.
                        TangoUnityDepth depth = new TangoUnityDepth(m_pointCloud);
                        GCHandle pinnedPoints = GCHandle.Alloc(depth.m_points, GCHandleType.Pinned);
                        TangoXYZij emulatedXyzij = _GetEmulatedRawXyzijData(depth, pinnedPoints);
                        m_onTangoDepthMultithreadedAvailable(emulatedXyzij);
                        pinnedPoints.Free();
                    }

                    if (m_onPointCloudMultithreadedAvailable != null)
                    {
                        // Pretend to be making a call from unmanaged code.
                        GCHandle pinnedPoints = GCHandle.Alloc(m_pointCloud.m_points, GCHandleType.Pinned);
                        TangoPointCloudIntPtr rawData = _GetEmulatedRawData(m_pointCloud, pinnedPoints);
                        m_onPointCloudMultithreadedAvailable(ref rawData);
                        pinnedPoints.Free();
                    }

                    if (m_onTangoDepthAvailable != null || m_onPointCloudAvailable != null)
                    {
                        m_isDirty = true;
                    }
                }
            }
#endif

            if (m_isDirty && (m_onTangoDepthAvailable != null || m_onPointCloudAvailable != null))
            {
                lock (m_lockObject)
                {
                    _ReducePointCloudPoints(m_pointCloud, m_maxNumReducedDepthPoints);

                    if (m_onTangoDepthAvailable != null)
                    {
                        m_onTangoDepthAvailable(new TangoUnityDepth(m_pointCloud));
                    }

                    if (m_onPointCloudAvailable != null)
                    {
                        m_onPointCloudAvailable(m_pointCloud);
                    }
                }

                m_isDirty = false;
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthAvailable -= handler;
            }
        }

        /// <summary>
        /// Register a multithread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnTangoDepthMultithreadedAvailable(OnTangoDepthMulithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthMultithreadedAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters a multithread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnTangoDepthMultithreadedAvailable(OnTangoDepthMulithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthMultithreadedAvailable -= handler;
            }
        }

        /// <summary>
        /// Registers a Unity main thread handler for the Tango point cloud event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnPointCloudAvailable(OnPointCloudAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onPointCloudAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters a Unity main thread handler for the Tango point cloud event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnPointCloudAvailable(OnPointCloudAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onPointCloudAvailable -= handler;
            }
        }

        /// <summary>
        /// Registers a Unity multithread handler for the Tango point cloud event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnPointCloudMultithreadedAvailable(
            OnPointCloudMultithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onPointCloudMultithreadedAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters a Unity multithread handler for the Tango point cloud event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void UnregisterOnPointCloudMultithreadedAvailable(
            OnPointCloudMultithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onPointCloudMultithreadedAvailable -= handler;
            }
        }

        /// <summary>
        /// Set an upper limit on the number of points in the point cloud.
        /// Hopefully a temporary workaround 'till this is implemented as an option C-side.
        /// </summary>
        /// <param name="maxPoints">Max points.</param>
        internal static void SetPointCloudLimit(int maxPoints)
        {
            m_maxNumReducedDepthPoints = maxPoints;
        }

        /// <summary>
        /// Callback that gets called when depth is available from the Tango Service.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="rawPointCloud">The depth data returned from Tango.</param>
        [AOT.MonoPInvokeCallback(typeof(DepthProvider.APIOnPointCloudAvailable))]
        private static void _OnPointCloudAvailable(IntPtr callbackContext, ref TangoPointCloudIntPtr rawPointCloud)
        {
            // Fill in the data to draw the point cloud.
            if (m_onPointCloudMultithreadedAvailable != null)
            {
                m_onPointCloudMultithreadedAvailable(ref rawPointCloud);
            }

            lock (m_lockObject)
            {
                // copy single members
                m_pointCloud.m_timestamp = rawPointCloud.m_timestamp;
                m_pointCloud.m_numPoints = rawPointCloud.m_numPoints;
                if (rawPointCloud.m_numPoints > 0)
                {
                    Marshal.Copy(rawPointCloud.m_points, m_pointCloud.m_points, 0,
                                 rawPointCloud.m_numPoints * 4);
                }

                m_isDirty = true;
            }

            // This must be done after the above Marshal.Copy so that it can efficiently reduce the array to just XYZ.
            if (m_onTangoDepthMultithreadedAvailable != null)
            {
                TangoXYZij xyzij = new TangoXYZij();
                xyzij.version = rawPointCloud.m_version;
                xyzij.timestamp = rawPointCloud.m_timestamp;
                xyzij.xyz_count = rawPointCloud.m_numPoints;
                xyzij.ij_rows = 0;
                xyzij.ij_cols = 0;
                xyzij.ij = IntPtr.Zero;
                xyzij.color_image = IntPtr.Zero;
                for (int it = 0; it < m_pointCloud.m_numPoints; ++it)
                {
                    m_xyzPoints[(it * 3) + 0] = m_pointCloud.m_points[(it * 4) + 0];
                    m_xyzPoints[(it * 3) + 1] = m_pointCloud.m_points[(it * 4) + 1];
                    m_xyzPoints[(it * 3) + 2] = m_pointCloud.m_points[(it * 4) + 2];
                }

                GCHandle pinnedXyzPoints = GCHandle.Alloc(m_xyzPoints, GCHandleType.Pinned);
                xyzij.xyz = pinnedXyzPoints.AddrOfPinnedObject();
                m_onTangoDepthMultithreadedAvailable(xyzij);
                pinnedXyzPoints.Free();
            }
        }

        /// <summary>
        /// Reduces depth points down to below a fixed number of points.
        ///
        /// TODO: Do this sort of thing in C code before before passing to Unity instead.
        /// </summary>
        /// <param name="pointCloud">Tango depth data to reduce.</param>
        /// <param name="maxNumPoints">Max points to reduce down to.</param>
        private static void _ReducePointCloudPoints(TangoPointCloudData pointCloud, int maxNumPoints)
        {
            if (maxNumPoints > 0 && pointCloud.m_numPoints > maxNumPoints)
            {
                // Here (maxNumPoints - 1) rather than maxPoints is just a quick and
                // dirty way to avoid any possibile edge-case accumulated FP error
                // in the sketchy code below.
                float keepFraction = (maxNumPoints - 1) / (float)pointCloud.m_numPoints;

                int keptPoints = 0;
                float keepCounter = 0;
                for (int i = 0; i < pointCloud.m_numPoints; i++)
                {
                    keepCounter += keepFraction;
                    if (keepCounter > 1)
                    {
                        pointCloud.m_points[keptPoints] = pointCloud.m_points[i];
                        keepCounter--;
                        keptPoints++;
                    }
                }

                pointCloud.m_numPoints = keptPoints;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Fill out <c>pointCloudData</c> with emulated values from Tango.
        /// </summary>
        /// <param name="pointCloudData">The point cloud data to fill out.</param>
        private static void _FillEmulatedPointCloud(ref TangoPointCloudData pointCloud)
        {
            List<Vector3> emulated = DepthProvider.GetTangoEmulation(out pointCloud.m_timestamp);

            pointCloud.m_numPoints = emulated.Count;
            for (int it = 0; it < emulated.Count; ++it)
            {
                pointCloud.m_points[(it * 4) + 0] = emulated[it].x;
                pointCloud.m_points[(it * 4) + 1] = emulated[it].y;
                pointCloud.m_points[(it * 4) + 2] = emulated[it].z;
                pointCloud.m_points[(it * 4) + 3] = 1;
            }
        }

        /// <summary>
        /// It's backwards, but fill an emulated TangoXYZij instance from an emulated TangoPointCloudData
        /// instance. It is the responsibility of the caller to GC pin/free the pointCloudData's m_points.
        /// </summary>
        /// <returns>Emulated raw xyzij data.</returns>
        /// <param name="depth">Emulated point cloud data.</param>>
        /// <param name="pinnedPoints">Pinned array of pointCloudData.m_points.</param>
        private static TangoXYZij _GetEmulatedRawXyzijData(TangoUnityDepth depth, GCHandle pinnedPoints)
        {
            TangoXYZij data = new TangoXYZij();
            data.xyz = pinnedPoints.AddrOfPinnedObject();
            data.xyz_count = depth.m_pointCount;
            data.ij_cols = 0;
            data.ij_rows = 0;
            data.ij = IntPtr.Zero;
            data.timestamp = depth.m_timestamp;
            return data;
        }

        /// <summary>
        /// It's backwards, but fill an emulated TangoPointCloudIntPtr instance from an emulated TangoPointCloudData
        /// instance. It is the responsibility of the caller to GC pin/free the pointCloudData's m_points.
        /// </summary>
        /// <returns>Emulated TangoPointCloudIntPtr instance.</returns>
        /// <param name="pointCloud">Emulated point cloud data.</param>>
        /// <param name="pinnedPoints">Pinned array of pointCloudData.m_points.</param>
        private static TangoPointCloudIntPtr _GetEmulatedRawData(TangoPointCloudData pointCloud, GCHandle pinnedPoints)
        {
            TangoPointCloudIntPtr raw;
            raw.m_version = 0;
            raw.m_timestamp = pointCloud.m_timestamp;
            raw.m_numPoints = pointCloud.m_numPoints;
            raw.m_points = pinnedPoints.AddrOfPinnedObject();
            return raw;
        }
#endif
    }

    /// <summary>
    /// Instance wrapper for the static DepthListener class.
    /// </summary>
    internal class DepthListenerWrapper : IDepthListenerWrapper
    {
        /// <summary>
        /// Set an upper limit on the number of points in the point cloud.
        /// Hopefully a temporary workaround 'till this is implemented as an option C-side.
        /// </summary>
        /// <param name="maxDepthPoints">Max points.</param>
        public void SetPointCloudLimit(int maxDepthPoints)
        {
            DepthListener.SetPointCloudLimit(maxDepthPoints);
        }
    }
}
