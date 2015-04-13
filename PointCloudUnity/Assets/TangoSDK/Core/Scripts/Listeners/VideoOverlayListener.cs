
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
using System.Collections;
using System;
using System.Runtime.InteropServices;

namespace Tango
{
    public delegate void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId, 
                                                           TangoUnityImageData imageBuffer);
    public delegate void OnExperimentalTangoImageAvailableEventHandler(Tango.TangoEnums.TangoCameraId cameraId);

    /// <summary>
    /// Video overlay listener.
    /// </summary>
    public class VideoOverlayListener 
    {
    	private VideoOverlayProvider.TangoService_onImageAvailable m_onImageAvailable;
        private VideoOverlayProvider.TangoService_onUnityFrameAvailable m_onUnityFrameAvailable;

        private event OnTangoImageAvailableEventHandler m_onTangoImageAvailable;
        private event OnExperimentalTangoImageAvailableEventHandler m_onExperimentalTangoImageAvailable;

		private TangoEnums.TangoCameraId m_previousCameraId;
		private TangoUnityImageData m_previousImageBuffer;
		private bool m_shouldSendEvent = false;
        private bool m_usingExperimentalOverlay = false;

    	/// <summary>
    	/// Sets the callback for image updates.
    	/// </summary>
    	/// <param name="cameraId">Camera identifier.</param>
    	public virtual void SetCallback(Tango.TangoEnums.TangoCameraId cameraId, bool useExperimentalOverlay, Texture2D videoOverlayTexture)
    	{
            m_usingExperimentalOverlay = useExperimentalOverlay;
            if(!useExperimentalOverlay)
            {
                m_previousImageBuffer = new TangoUnityImageData();
        		m_onImageAvailable = new Tango.VideoOverlayProvider.TangoService_onImageAvailable(_OnImageAvailable);
        		Tango.VideoOverlayProvider.SetCallback(cameraId, m_onImageAvailable);
            }
            else
            {
                if(videoOverlayTexture != null)
                {
                    m_onUnityFrameAvailable = new Tango.VideoOverlayProvider.TangoService_onUnityFrameAvailable(_OnExperimentalUnityFrameAvailable);
                    VideoOverlayProvider.ExperimentalConnectTexture(cameraId, 
                                                                    videoOverlayTexture.GetNativeTextureID(), 
                                                                    m_onUnityFrameAvailable);

                    Debug.Log("VideoOverlayListener.SetCallback() : Experimental Overlay listener hooked up");
                }
                else
                {
                    Debug.Log("VideoOverlayListener.SetCallback() : No Texture2D found!");
                }
            }
    	}

        /// <summary>
        /// Sends event if video overlay is available.
        /// </summary>
		public void SendIfVideoOverlayAvailable()
		{
            if(m_usingExperimentalOverlay)
            {
                if(m_onExperimentalTangoImageAvailable != null && m_shouldSendEvent)
                {
                    m_onExperimentalTangoImageAvailable(m_previousCameraId);
                    m_shouldSendEvent = false;
                }
            }
            else
            {
    			if(m_onTangoImageAvailable != null && m_shouldSendEvent)
    			{
    				m_onTangoImageAvailable(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, m_previousImageBuffer);
                    m_shouldSendEvent = false;
    			}
            }
		}

        /// <summary>
        /// Registers the on tango image available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnTangoImageAvailable(OnTangoImageAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoImageAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters the on tango image available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnTangoImageAvailable(OnTangoImageAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onTangoImageAvailable -= handler;
            }
        }
        
        /// <summary>
        /// Registers the on tango image available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void RegisterOnExperimentalTangoImageAvailable(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onExperimentalTangoImageAvailable += handler;
            }
        }
        
        /// <summary>
        /// Unregisters the on tango image available.
        /// </summary>
        /// <param name="handler">Handler.</param>
        public void UnregisterOnExperimentalTangoImageAvailable(OnExperimentalTangoImageAvailableEventHandler handler)
        {
            if(handler != null)
            {
                m_onExperimentalTangoImageAvailable -= handler;
            }
        }
    	
    	/// <summary>
    	/// Handle the callback sent by the Tango Service
    	/// when a new image is sampled.
    	/// </summary>
    	/// <param name="cameraId">Camera identifier.</param>
    	/// <param name="callbackContext">Callback context.</param>
    	/// <param name="imageBuffer">Image buffer.</param>
    	protected void _OnImageAvailable(IntPtr callbackContext,
    	                                 TangoEnums.TangoCameraId cameraId, 
    	                                 TangoImageBuffer imageBuffer)
        {
			m_previousCameraId = cameraId;

            if(m_previousImageBuffer.data == null)
            {
                m_previousImageBuffer.data = new byte[imageBuffer.width * imageBuffer.height * 2];
            }

            m_previousImageBuffer.width = imageBuffer.width;
            m_previousImageBuffer.height = imageBuffer.height;
            m_previousImageBuffer.stride = imageBuffer.stride;
            m_previousImageBuffer.timestamp = imageBuffer.timestamp;
            m_previousImageBuffer.format = imageBuffer.format;
            m_previousImageBuffer.frame_number = imageBuffer.frame_number;

            Marshal.Copy(imageBuffer.data, m_previousImageBuffer.data, 0, m_previousImageBuffer.data.Length);

			m_shouldSendEvent = true;
        }

        public void _OnExperimentalUnityFrameAvailable(IntPtr callbackContext, Tango.TangoEnums.TangoCameraId cameraId)
        {
            m_previousCameraId = cameraId;
            m_shouldSendEvent = true;
        }
    }
}