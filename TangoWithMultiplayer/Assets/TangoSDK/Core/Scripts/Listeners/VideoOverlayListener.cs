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
    /// <param name="imageBuffer">The image from the camera.</param>
    internal delegate void OnTangoImageMultithreadedAvailableEventHandler(TangoEnums.TangoCameraId cameraId, 
                                                                          TangoImageBuffer imageBuffer);

    /// <summary>
    /// Marshals Tango image data between the C callbacks in one thread and
    /// the main Unity thread.
    /// 
    /// Only supports the color camera.
    /// </summary>
    internal class VideoOverlayListener 
    {
        /// <summary>
        /// The ID of the color camera.
        /// </summary>
        private const TangoEnums.TangoCameraId COLOR_CAMERA_ID = TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR;

        private VideoOverlayProvider.TangoService_onImageAvailable m_onImageAvailable;
        private VideoOverlayProvider.TangoService_onTextureAvailable m_onTextureAvailable;
        private VideoOverlayProvider.TangoService_onTextureAvailable m_onYUVTextureAvailable;

        private TangoUnityImageData m_previousImageBuffer;
        private bool m_shouldSendTextureMethodEvent = false;
        private bool m_shouldSendByteBufferMethodEvent = false;

        /// <summary>
        /// DEPRECATED. Will be removed in a future SDK.
        /// </summary>
        private bool m_shouldSendYUVTextureIdMethodEvent = false;

        private object m_lockObject = new object();

        /// <summary>
        /// Called when a new Tango image is available.
        /// </summary>
        private OnTangoImageAvailableEventHandler m_onTangoImageAvailable;

        /// <summary>
        /// Called when a new Tango texture is available.
        /// </summary>
        private OnTangoCameraTextureAvailableEventHandler m_onTangoCameraTextureAvailable;

        /// <summary>
        /// DEPRECATED: Called when a new Tango YUV texture is available (experimental version).
        /// </summary>
        private OnTangoCameraTextureAvailableEventHandler m_onTangoYUVTextureAvailable;

        /// <summary>
        /// Called when a new Tango image is available on the thread the image came from.
        /// </summary>
        private OnTangoImageMultithreadedAvailableEventHandler m_onTangoImageMultithreadedAvailable;

        /// <summary>
        /// Register to get Tango texture events when the camera is updated.
        /// 
        /// NOTE: Tango texture events happen on a different thread than the main Unity thread.
        /// </summary>
        internal void SetCallbackTextureMethod()
        {
            m_onTextureAvailable = new VideoOverlayProvider.TangoService_onTextureAvailable(_OnTangoCameraTextureAvailable);
            VideoOverlayProvider.SetCallback(COLOR_CAMERA_ID, m_onTextureAvailable);
            Debug.Log("VideoOverlayListener.SetCallback() : Texture listener hooked up");
        }

        /// <summary>
        /// DEPRECATED: Register to get Tango texture events for the texture ID is updated.
        /// 
        /// NOTE: Tango texture events happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        /// <param name="videoOverlayTexture">The video overlay texture to use.</param> 
        internal void SetCallbackYUVTextureIdMethod(YUVTexture videoOverlayTexture)
        {
            if (videoOverlayTexture != null)
            {
                m_onYUVTextureAvailable = 
                    new VideoOverlayProvider.TangoService_onTextureAvailable(_OnTangoYUVTextureAvailable);
                VideoOverlayProvider.ExperimentalConnectTexture(
                    COLOR_CAMERA_ID, videoOverlayTexture, m_onYUVTextureAvailable);

                Debug.Log("VideoOverlayListener.SetCallback() : YUVTexture listener hooked up");
            }
            else
            {
                Debug.Log("VideoOverlayListener.SetCallback() : No Texture2D found!");
            }
        }

        /// <summary>
        /// Register to get Tango image events for getting the texture byte buffer callback.
        /// 
        /// NOTE: Tango image events happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal void SetCallbackByteBufferMethod()
        {
            m_previousImageBuffer = new TangoUnityImageData();
            m_onImageAvailable = new VideoOverlayProvider.TangoService_onImageAvailable(_OnImageAvailable);
            VideoOverlayProvider.SetCallback(COLOR_CAMERA_ID, m_onImageAvailable);
        }

        /// <summary>
        /// Raise a Tango image event if there is new data.
        /// </summary>
        internal void SendIfVideoOverlayAvailable()
        {
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
                        TangoImageBuffer emulatedImageBuffer = _GetEmulatedTangoImageBuffer(m_previousImageBuffer, pinnedColorBuffer);
                        m_onTangoImageMultithreadedAvailable(COLOR_CAMERA_ID, emulatedImageBuffer);
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
        internal void RegisterOnTangoImageAvailable(OnTangoImageAvailableEventHandler handler)
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
        internal void UnregisterOnTangoImageAvailable(OnTangoImageAvailableEventHandler handler)
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
        internal void RegisterOnTangoCameraTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
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
        internal void UnregisterOnTangoCameraTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
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
        internal void RegisterOnTangoYUVTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
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
        internal void UnregisterOnTangoYUVTextureAvailable(OnTangoCameraTextureAvailableEventHandler handler)
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
        internal void RegisterOnTangoImageMultithreadedAvailable(OnTangoImageMultithreadedAvailableEventHandler handler)
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
        internal void UnregisterOnTangoImageMultithreadedAvailable(OnTangoImageMultithreadedAvailableEventHandler handler)
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
        /// <param name="imageBuffer">Image buffer.</param>
        private void _OnImageAvailable(IntPtr callbackContext, TangoEnums.TangoCameraId cameraId,
                                       TangoImageBuffer imageBuffer)
        {
            if (cameraId != COLOR_CAMERA_ID)
            {
                return;
            }

            if (m_onTangoImageMultithreadedAvailable != null)
            {
                m_onTangoImageMultithreadedAvailable(cameraId, imageBuffer);
            }

            lock (m_lockObject)
            {
                if (m_previousImageBuffer.data == null)
                {
                    m_previousImageBuffer.data = new byte[(imageBuffer.width * imageBuffer.height * 3) / 2];
                }

                m_previousImageBuffer.width = imageBuffer.width;
                m_previousImageBuffer.height = imageBuffer.height;
                m_previousImageBuffer.stride = imageBuffer.stride;
                m_previousImageBuffer.timestamp = imageBuffer.timestamp;
                m_previousImageBuffer.format = imageBuffer.format;
                m_previousImageBuffer.frame_number = imageBuffer.frame_number;

                Marshal.Copy(imageBuffer.data, m_previousImageBuffer.data, 0, m_previousImageBuffer.data.Length);

                m_shouldSendByteBufferMethodEvent = true;
            }
        }

        /// <summary>
        /// Handle the callback set by the Tango Service when a new image is available.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="cameraId">Camera identifier.</param>
        private void _OnTangoCameraTextureAvailable(IntPtr callbackContext, TangoEnums.TangoCameraId cameraId)
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
        private void _OnTangoYUVTextureAvailable(IntPtr callbackContext, TangoEnums.TangoCameraId cameraId)
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
        private void _FillEmulatedColorCameraData(TangoUnityImageData colorCameraData)
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
        private TangoImageBuffer _GetEmulatedTangoImageBuffer(TangoUnityImageData colorImageData, GCHandle pinnedColorBuffer)
        {
            TangoImageBuffer imageBuffer = new TangoImageBuffer();
            imageBuffer.data = pinnedColorBuffer.AddrOfPinnedObject();
            imageBuffer.width = colorImageData.width;
            imageBuffer.height = colorImageData.height;
            imageBuffer.stride = colorImageData.stride;
            imageBuffer.format = colorImageData.format;
            imageBuffer.timestamp = colorImageData.timestamp;
            imageBuffer.frame_number = colorImageData.frame_number;
            return imageBuffer;
        }
#endif
    }
}