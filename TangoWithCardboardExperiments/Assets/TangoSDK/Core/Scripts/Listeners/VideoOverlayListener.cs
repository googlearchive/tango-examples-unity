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
    /// Experimental API only, subject to change.  Delegate for Tango image events.
    /// </summary>
    /// <param name="cameraId">The camera for the image.</param>
    internal delegate void OnExperimentalTangoImageAvailableEventHandler(Tango.TangoEnums.TangoCameraId cameraId);

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
    /// </summary>
    internal class VideoOverlayListener 
    {
        private VideoOverlayProvider.TangoService_onImageAvailable m_onImageAvailable;
        private VideoOverlayProvider.TangoService_onUnityFrameAvailable m_onUnityFrameAvailable;

        private TangoEnums.TangoCameraId m_previousCameraId;
        private TangoUnityImageData m_previousImageBuffer;
        private bool m_shouldSendTextureIdMethodEvent = false;
        private bool m_shouldSendByteBufferMethodEvent = false;
        private object m_lockObject = new object();

        /// <summary>
        /// Called when a new Tango image is available.
        /// </summary>
        private OnTangoImageAvailableEventHandler m_onTangoImageAvailable;

        /// <summary>
        /// Called when a new Tange image is available (experimental version).
        /// </summary>
        private OnExperimentalTangoImageAvailableEventHandler m_onExperimentalTangoImageAvailable;

        /// <summary>
        /// Called when a new Tango image is available on the thread the image came from.
        /// </summary>
        private OnTangoImageMultithreadedAvailableEventHandler m_onTangoImageMultithreadedAvailable;

        /// <summary>
        /// Register to get Tango image events for the texture ID is updated.
        /// 
        /// NOTE: Tango image events happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        /// <param name="cameraId">Camera identifier to get events for.</param>
        /// <param name="videoOverlayTexture">The video overlay texture to use.</param> 
        internal virtual void SetCallbackTextureIdMethod(Tango.TangoEnums.TangoCameraId cameraId, 
                                                         YUVTexture videoOverlayTexture)
        {
            if (videoOverlayTexture != null)
            {
                m_onUnityFrameAvailable = 
                    new Tango.VideoOverlayProvider.TangoService_onUnityFrameAvailable(_OnExperimentalUnityFrameAvailable);
                VideoOverlayProvider.ExperimentalConnectTexture(cameraId,
                                                                videoOverlayTexture,
                                                                m_onUnityFrameAvailable);

                Debug.Log("VideoOverlayListener.SetCallback() : Experimental Overlay listener hooked up");
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
        /// <param name="cameraId">Camera identifier to get events for.</param>
        internal virtual void SetCallbackByteBufferMethod(Tango.TangoEnums.TangoCameraId cameraId)
        {
            m_previousImageBuffer = new TangoUnityImageData();
            m_onImageAvailable = new Tango.VideoOverlayProvider.TangoService_onImageAvailable(_OnImageAvailable);
            Tango.VideoOverlayProvider.SetCallback(cameraId, m_onImageAvailable);
        }

        /// <summary>
        /// Raise a Tango image event if there is new data.
        /// </summary>
        internal void SendIfVideoOverlayAvailable()
        {
            lock (m_lockObject)
            {
                if (m_onExperimentalTangoImageAvailable != null && m_shouldSendTextureIdMethodEvent)
                {
                    m_onExperimentalTangoImageAvailable(m_previousCameraId);
                    m_shouldSendTextureIdMethodEvent = false;
                }

                if (m_onTangoImageAvailable != null && m_shouldSendByteBufferMethodEvent)
                {
                    m_onTangoImageAvailable(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, m_previousImageBuffer);
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
        /// Register a Unity main thread handler for the Tango image event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterOnExperimentalTangoImageAvailable(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onExperimentalTangoImageAvailable += handler;
            }
        }
        
        /// <summary>
        /// Unregister a Unity main thread handler for the Tango image event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnExperimentalTangoImageAvailable(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onExperimentalTangoImageAvailable -= handler;
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
            if (m_onTangoImageMultithreadedAvailable != null)
            {
                m_onTangoImageMultithreadedAvailable(cameraId, imageBuffer);
            }

            lock (m_lockObject)
            {
                m_previousCameraId = cameraId;

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
        private void _OnExperimentalUnityFrameAvailable(IntPtr callbackContext, Tango.TangoEnums.TangoCameraId cameraId)
        {
            lock (m_lockObject)
            {
                m_previousCameraId = cameraId;
                m_shouldSendTextureIdMethodEvent = true;
            }
        }
    }
}