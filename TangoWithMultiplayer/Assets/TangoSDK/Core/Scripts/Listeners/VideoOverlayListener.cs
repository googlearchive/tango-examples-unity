//-----------------------------------------------------------------------
// <copyright file="VideoOverlayListener.cs" company="Google">
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
    using System.Collections;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango image events.
    /// </summary>
    /// <param name="cameraId">The camera for the image.</param>
    /// <param name="imageBuffer">The image from the camera.</param>
    internal delegate void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId,
                                                             TangoUnityImageData imageBuffer);

    /// <summary>
    /// Delegate for Tango texture events.
    /// </summary>
    /// <param name="cameraId">The camera that has an updated texture.</param>
    internal delegate void OnTangoCameraTextureAvailableEventHandler(TangoEnums.TangoCameraId cameraId);

    /// <summary>
    /// Delegate for Tango image events that can be called on any thread.
    /// </summary>
    /// <param name="cameraId">The camera for the image.</param>
    /// <param name="image">The image from the camera.</param>
    /// <param name="cameraMetadata">The image's camera metadata.</param>
    internal delegate void OnTangoImageMultithreadedAvailableEventHandler(TangoEnums.TangoCameraId cameraId,
                                                                          TangoImage image,
                                                                          TangoCameraMetadata cameraMetadata);

    /// <summary>
    /// Marshals Tango image data between the C callbacks in one thread and
    /// the main Unity thread.
    ///
    /// Only supports the color camera.
    /// </summary>
    internal static class VideoOverlayListener
    {
        /// <summary>
        /// The ID of the color camera.
        /// </summary>
        private const TangoEnums.TangoCameraId COLOR_CAMERA_ID = TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR;

        /// <summary>
        /// The lock object used as a mutex.
        /// </summary>
        private static System.Object m_lockObject = new System.Object();

        private static VideoOverlayProvider.APIOnImageAvailable m_onImageAvailable;
        private static VideoOverlayProvider.APIOnTextureAvailable m_onTextureAvailable;
        private static VideoOverlayProvider.APIOnTextureAvailable m_onYUVTextureAvailable;

        private static TangoUnityImageData m_previousImageBuffer;
        private static bool m_shouldSendTextureMethodEvent;
        private static bool m_shouldSendByteBufferMethodEvent;

        /// <summary>
        /// DEPRECATED. Will be removed in a future SDK.
        /// </summary>
        private static bool m_shouldSendYUVTextureIdMethodEvent = false;

        /// <summary>
        /// Called when a new Tango image is available.
        /// </summary>
        private static OnTangoImageAvailableEventHandler m_onTangoImageAvailable;

        /// <summary>
        /// Called when a new Tango texture is available.
        /// </summary>
        private static OnTangoCameraTextureAvailableEventHandler m_onTangoCameraTextureAvailable;

        /// <summary>
        /// DEPRECATED: Called when a new Tango YUV texture is available (experimental version).
        /// </summary>
        private static OnTangoCameraTextureAvailableEventHandler m_onTangoYUVTextureAvailable;

        /// <summary>
        /// Called when a new TangoImage is available on the thread the image came from.
        /// </summary>
        private static OnTangoImageMultithreadedAvailableEventHandler m_onTangoImageMultithreadedAvailable;

        /// <summary>
        /// Initializes the <see cref="Tango.VideoOverlayListener"/> class.
        /// </summary>
        static VideoOverlayListener()
        {
            Reset();
        }

        /// <summary>
        /// Stop getting Tango image or texture callbacks.
        /// </summary>
        internal static void Reset()
        {
            // Avoid calling into tango_client_api before the correct library is loaded.
            if (m_onImageAvailable != null || m_onTextureAvailable != null || m_onYUVTextureAvailable != null)
            {
                VideoOverlayProvider.ClearCallback(COLOR_CAMERA_ID);
            }

            m_onImageAvailable = null;
            m_onTextureAvailable = null;
            m_onYUVTextureAvailable = null;
            m_previousImageBuffer = new TangoUnityImageData();
            m_shouldSendTextureMethodEvent = false;
            m_shouldSendByteBufferMethodEvent = false;
            m_shouldSendYUVTextureIdMethodEvent = false;
            m_onTangoImageAvailable = null;
            m_onTangoCameraTextureAvailable = null;
            m_onTangoYUVTextureAvailable = null;
            m_onTangoImageMultithreadedAvailable = null;
        }

        /// <summary>
        /// Clear just the Tango callbacks. External registered listeners are kept.
        /// </summary>
        internal static void ClearTangoCallbacks()
        {
            m_onTextureAvailable = null;
            m_onYUVTextureAvailable = null;
            m_onImageAvailable = null;
        }

        /// <summary>
        /// Register to get Tango texture events when the camera is updated.
        ///
        /// NOTE: Tango texture events happen on a different thread than the main Unity thread.
        /// </summary>
        internal static void SetCallbackTextureMethod()
        {
            if (m_onTextureAvailable != null)
            {
                Debug.Log("VideoOverlayProvider.SetCallbackTextureMethod() called when a callback is already set.");
                return;
            }

            Debug.Log("VideoOverlayProvider.SetCallbackTextureMethod()");
            m_onTextureAvailable = new VideoOverlayProvider.APIOnTextureAvailable(_OnTangoCameraTextureAvailable);
            VideoOverlayProvider.SetCallback(COLOR_CAMERA_ID, m_onTextureAvailable);
        }

        /// <summary>
        /// DEPRECATED: Register to get Tango texture events for the texture ID is updated.
        ///
        /// NOTE: Tango texture events happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        /// <param name="videoOverlayTexture">The video overlay texture to use.</param>
        internal static void SetCallbackYUVTextureIdMethod(YUVTexture videoOverlayTexture)
        {
            if (videoOverlayTexture != null)
            {
                if (m_onYUVTextureAvailable != null)
                {
                    Debug.Log("VideoOverlayProvider.SetCallbackYUVTextureIdMethod() called when a callback is already set.");
                    return;
                }

                Debug.Log("VideoOverlayProvider.SetCallbackYUVTextureIdMethod()");
                m_onYUVTextureAvailable =
                    new VideoOverlayProvider.APIOnTextureAvailable(_OnTangoYUVTextureAvailable);
                VideoOverlayProvider.ExperimentalConnectTexture(
                    COLOR_CAMERA_ID, videoOverlayTexture, m_onYUVTextureAvailable);
            }
            else
            {
                Debug.Log("VideoOverlayListener.SetCallbackYUVTextureIdMethod() : No Texture2D found!");
            }
        }

        /// <summary>
        /// Register to get Tango image events for getting the texture byte buffer callback.
        ///
        /// NOTE: Tango image events happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal static void SetCallbackByteBufferMethod()
        {
            if (m_onImageAvailable != null)
            {
                Debug.Log("VideoOverlayProvider.SetCallbackByteBufferMethod() called when a callback is already set.");
                return;
            }

            Debug.Log("VideoOverlayProvider.SetCallbackByteBufferMethod()");
            m_onImageAvailable = new VideoOverlayProvider.APIOnImageAvailable(_OnImageAvailable);
            VideoOverlayProvider.SetCallback(COLOR_CAMERA_ID, m_onImageAvailable);
        }

        /// <summary>
        /// Raise a Tango image event if there is new data.
        /// </summary>
        internal static void SendIfAvailable()
        {
            if (m_onImageAvailable == null && m_onTextureAvailable == null
                && m_onYUVTextureAvailable == null)
            {
                return;
            }

#if UNITY_EDITOR
            lock (m_lockObject)
            {
                if (VideoOverlayProvider.m_emulationIsDirty)
                {
                    VideoOverlayProvider.m_emulationIsDirty = false;

                    if (m_onTangoYUVTextureAvailable != null)
                    {
                        m_shouldSendYUVTextureIdMethodEvent = true;
                    }

                    if (m_onTangoCameraTextureAvailable != null)
                    {
                        m_shouldSendTextureMethodEvent = true;
                    }

                    if (m_onTangoImageAvailable != null || m_onTangoImageMultithreadedAvailable != null)
                    {
                        _FillEmulatedColorCameraData(m_previousImageBuffer);
                    }

                    if (m_onTangoImageMultithreadedAvailable != null)
                    {
                        GCHandle pinnedColorBuffer = GCHandle.Alloc(m_previousImageBuffer.data, GCHandleType.Pinned);
                        TangoImage emulatedImage;
                        TangoCameraMetadata emulatedCameraMetadata;
                        _GetEmulatedTangoImage(m_previousImageBuffer, pinnedColorBuffer, out emulatedImage,
                            out emulatedCameraMetadata);
                        m_onTangoImageMultithreadedAvailable(COLOR_CAMERA_ID, emulatedImage, emulatedCameraMetadata);
                    }

                    if (m_onTangoImageAvailable != null)
                    {
                        m_shouldSendByteBufferMethodEvent = true;
                    }
                }
            }
#endif

            lock (m_lockObject)
            {
                if (m_onTangoYUVTextureAvailable != null && m_shouldSendYUVTextureIdMethodEvent)
                {
                    m_onTangoYUVTextureAvailable(COLOR_CAMERA_ID);
                    m_shouldSendYUVTextureIdMethodEvent = false;
                }

                if (m_onTangoCameraTextureAvailable != null & m_shouldSendTextureMethodEvent)
                {
                    m_onTangoCameraTextureAvailable(COLOR_CAMERA_ID);
                    m_shouldSendTextureMethodEvent = false;
                }

                if (m_onTangoImageAvailable != null && m_shouldSendByteBufferMethodEvent)
                {
                    m_onTangoImageAvailable(COLOR_CAMERA_ID, m_previousImageBuffer);
                    m_shouldSendByteBufferMethodEvent = false;
                }
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango image event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnTangoImageAvailable(OnTangoImageAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoImageAvailable += handler;
            }
        }

        /// <summary>
        /// Unregister a Unity main thread handler for the Tango image event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnTangoImageAvailable(OnTangoImageAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoImageAvailable -= handler;
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango texture event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnTangoCameraTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoCameraTextureAvailable += handler;
            }
        }

        /// <summary>
        /// Unregister a Unity main thread handler for the Tango texture event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnTangoCameraTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoCameraTextureAvailable -= handler;
            }
        }

        /// <summary>
        /// DEPRECATED: Register a Unity main thread handler for the Tango texture event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnTangoYUVTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoYUVTextureAvailable += handler;
            }
        }

        /// <summary>
        /// DEPRECATED: Unregister a Unity main thread handler for the Tango texture event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnTangoYUVTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoYUVTextureAvailable -= handler;
            }
        }

        /// <summary>
        /// Register a multithread handler for the Tango image event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal static void RegisterOnTangoImageMultithreadedAvailable(OnTangoImageMultithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoImageMultithreadedAvailable += handler;
            }
        }

        /// <summary>
        /// Unregister a multithread handler for the Tango image event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal static void UnregisterOnTangoImageMultithreadedAvailable(OnTangoImageMultithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoImageMultithreadedAvailable -= handler;
            }
        }

        /// <summary>
        /// Handle the callback sent by the Tango Service when a new image is sampled.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="image">Image buffer.</param>
        /// <param name="cameraMetadata">Camera metadata for this image.</param>
        [AOT.MonoPInvokeCallback(typeof(VideoOverlayProvider.APIOnImageAvailable))]
        private static void _OnImageAvailable(
            IntPtr callbackContext, TangoEnums.TangoCameraId cameraId, ref TangoImage image,
            ref TangoCameraMetadata cameraMetadata)
        {
            if (cameraId != COLOR_CAMERA_ID)
            {
                return;
            }

            if (m_onTangoImageMultithreadedAvailable != null)
            {
                m_onTangoImageMultithreadedAvailable(cameraId, image, cameraMetadata);
            }

            lock (m_lockObject)
            {
                // This is correct because Tango is required to support YUV888 as Y plane, UV interleaved plane.
                int uvPlaneSize = image.m_planeSize2 + 1;
                if (m_previousImageBuffer.data == null)
                {
                    m_previousImageBuffer.data = new byte[image.m_planeSize0 + uvPlaneSize];
                }

                m_previousImageBuffer.width = image.m_width;
                m_previousImageBuffer.height = image.m_height;
                m_previousImageBuffer.stride = (uint)image.m_planeRowStride0;
                m_previousImageBuffer.timestamp = ((double)image.m_timestampNs) * Common.SECS_PER_NANOSECS;
                m_previousImageBuffer.format = image.m_format;
                m_previousImageBuffer.frame_number = cameraMetadata.m_frameNumber;

                //// Convert from YUV888 to NV21.
                //// Get Ys.
                Marshal.Copy(image.m_planeData0, m_previousImageBuffer.data, 0, image.m_planeSize0);

                //// Size of UV plane is image.plane_2_size + 1, since Tango guarantees that YUV888 has UV interleaved.
                Marshal.Copy(image.m_planeData2, m_previousImageBuffer.data, image.m_planeSize0, uvPlaneSize);

                m_shouldSendByteBufferMethodEvent = true;
            }
        }

        /// <summary>
        /// Handle the callback set by the Tango Service when a new image is available.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="cameraId">Camera identifier.</param>
        [AOT.MonoPInvokeCallback(typeof(VideoOverlayProvider.APIOnTextureAvailable))]
        private static void _OnTangoCameraTextureAvailable(IntPtr callbackContext, TangoEnums.TangoCameraId cameraId)
        {
            if (cameraId != COLOR_CAMERA_ID)
            {
                return;
            }

            lock (m_lockObject)
            {
                m_shouldSendTextureMethodEvent = true;
            }
        }

        /// <summary>
        /// DEPRECATED: Handle the callback set by the Tango Service when a new image is available.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="cameraId">Camera identifier.</param>
        [AOT.MonoPInvokeCallback(typeof(VideoOverlayProvider.APIOnTextureAvailable))]
        private static void _OnTangoYUVTextureAvailable(IntPtr callbackContext, TangoEnums.TangoCameraId cameraId)
        {
            if (cameraId != COLOR_CAMERA_ID)
            {
                return;
            }

            lock (m_lockObject)
            {
                m_shouldSendYUVTextureIdMethodEvent = true;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Fill out <c>colorCameraData</c> with emulated values from Tango.
        /// </summary>
        /// <param name="colorCameraData">The image data to fill out.</param>
        private static void _FillEmulatedColorCameraData(TangoUnityImageData colorCameraData)
        {
            VideoOverlayProvider.GetTangoEmulation(colorCameraData);
        }

        /// <summary>
        /// It's backwards, but fill tango image buffer data with already-emulated data.
        /// It is the responsibility of the caller to GC pin/free the colorImageData's data array.
        /// </summary>
        /// <returns>Emulated raw color buffer.</returns>
        /// <param name="colorImageData">Emulated color buffer data.</param>>
        /// <param name="pinnedColorBuffer">Pinned array of imageBuffer.data.</param>
        /// <param name="image"> TangoImage populated with emulator image data.</param>
        /// <param name="cameraMetadata">Camera metadata for emulated image.</param>
        private static void _GetEmulatedTangoImage(TangoUnityImageData colorImageData, GCHandle pinnedColorBuffer,
            out TangoImage image, out TangoCameraMetadata cameraMetadata)
        {
            image = new TangoImage();
            cameraMetadata = new TangoCameraMetadata();
            image.m_planeData0 = pinnedColorBuffer.AddrOfPinnedObject();
            image.m_planeData2 = new IntPtr(pinnedColorBuffer.AddrOfPinnedObject().ToInt64() + image.m_width * image.m_height);
            image.m_planeData1 = new IntPtr(image.m_planeData2.ToInt64() + 1);
            image.m_width = colorImageData.width;
            image.m_height = colorImageData.height;

            // The existing system assumes the whole image has the same row stride across all planes.
            int stride = (int) colorImageData.stride;
            image.m_planeRowStride0 = stride;
            image.m_planeRowStride1 = stride;
            image.m_planeRowStride2 = stride;
            image.m_planeRowStride3 = stride;

            image.m_format = colorImageData.format;
            image.m_timestampNs = Convert.ToInt64(colorImageData.timestamp / Common.SECS_PER_NANOSECS);
            cameraMetadata.m_frameNumber = colorImageData.frame_number;
            cameraMetadata.m_timestampNs = image.m_timestampNs;
        }
#endif
    }
}
