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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Tango;

namespace Tango
{
    
    /// <summary>
    /// Wraps separate textures for Y, U, and V planes.
    /// </summary>
    public class YUVTexture
    {
        /// <summary>
        /// The m_video overlay texture y.
        /// Columns     1280/4 [bytes packed in RGBA channels]
        /// Rows        720
        /// </summary>
        public Texture2D m_videoOverlayTextureY;

        /// <summary>
        /// The m_video overlay texture cb.
        /// Columns     640/4 [bytes packed in RGBA channels]
        /// Rows        360
        /// </summary>
        public Texture2D m_videoOverlayTextureCb;

        /// <summary>
        /// The m_video overlay texture cr.
        /// Columns     640 * 2 / 4 [bytes packed in RGBA channels]
        /// Rows        360
        /// </summary>
        public Texture2D m_videoOverlayTextureCr;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.YUVTexture"/> class.
        /// NOTE : Texture resolutions will be reset by the API. The sizes passed
        /// into the constructor are not guaranteed to persist when running on device.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="format">Format.</param>
        /// <param name="mipmap">If set to <c>true</c> mipmap.</param>
        public YUVTexture(int yPlaneWidth, int yPlaneHeight,
                          int uvPlaneWidth, int uvPlaneHeight,
                          TextureFormat format, bool mipmap)
        {
            m_videoOverlayTextureY = new Texture2D(yPlaneWidth, yPlaneHeight, format, mipmap);
            m_videoOverlayTextureCb = new Texture2D(uvPlaneWidth, uvPlaneHeight, format, mipmap);
            m_videoOverlayTextureCr = new Texture2D(uvPlaneWidth, uvPlaneHeight, format, mipmap);
        }

        /// <summary>
        /// Resizes all yuv texture planes.
        /// </summary>
        /// <param name="yPlaneWidth">Y plane width.</param>
        /// <param name="yPlaneHeight">Y plane height.</param>
        /// <param name="uvPlaneWidth">Uv plane width.</param>
        /// <param name="uvPlaneHeight">Uv plane height.</param>
        public void ResizeAll(int yPlaneWidth, int yPlaneHeight,
                              int uvPlaneWidth, int uvPlaneHeight)
        {
            m_videoOverlayTextureY.Resize(yPlaneWidth, yPlaneHeight);
            m_videoOverlayTextureCb.Resize(uvPlaneWidth, uvPlaneHeight);
            m_videoOverlayTextureCr.Resize(uvPlaneWidth, uvPlaneHeight);
        }
    }

    /// <summary>
    /// Video Overlay Provider class provide video functions
    /// to get frame textures.
    /// </summary>
    public class VideoOverlayProvider
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TangoService_onImageAvailable(IntPtr callbackContext, Tango.TangoEnums.TangoCameraId cameraId, [In,Out] TangoImageBuffer image);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TangoService_onUnityFrameAvailable(IntPtr callbackContext, Tango.TangoEnums.TangoCameraId cameraId);
        
        private static readonly string CLASS_NAME = "VideoOverlayProvider";
        private static IntPtr callbackContext;
        
        /// <summary>
        /// Connects the texture.
        /// </summary>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="textureId">Texture identifier.</param>
        public static void ConnectTexture(TangoEnums.TangoCameraId cameraId, int textureId)
        {
            int returnValue = VideoOverlayAPI.TangoService_connectTextureId(cameraId, textureId);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.ConnectTexture() Texture was not connected to camera!");
            }
        }
        
        public static void ExperimentalConnectTexture(TangoEnums.TangoCameraId cameraId, YUVTexture textures, TangoService_onUnityFrameAvailable onUnityFrameAvailable)
        {
            int returnValue = VideoOverlayAPI.TangoService_Experimental_connectTextureIdUnity(cameraId, 
                                                                                              (uint)textures.m_videoOverlayTextureY.GetNativeTextureID(), 
                                                                                              (uint)textures.m_videoOverlayTextureCb.GetNativeTextureID(), 
                                                                                              (uint)textures.m_videoOverlayTextureCr.GetNativeTextureID(), 
                                                                                              callbackContext, 
                                                                                              onUnityFrameAvailable);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.ConnectTexture() Texture was not connected to camera!");
            }
        }
        
        /// <summary>
        /// Renders the latest frame.
        /// </summary>
        /// <returns>The latest frame timestamp.</returns>
        /// <param name="cameraId">Camera identifier.</param>
        public static double RenderLatestFrame(TangoEnums.TangoCameraId cameraId)
        {
            double timestamp = 0.0;
            int returnValue = VideoOverlayAPI.TangoService_updateTexture(cameraId, ref timestamp);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.UpdateTexture() Texture was not updated by camera!");
            }

            return timestamp;
        }
        
        /// <summary>
        /// Get the camera/sensor intrinsics.
        /// </summary>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="intrinsics">Camera intrinsics data.</param>
        public static void GetIntrinsics(TangoEnums.TangoCameraId cameraId, [Out] TangoCameraIntrinsics intrinsics)
        {
            int returnValue = VideoOverlayAPI.TangoService_getCameraIntrinsics(cameraId, intrinsics);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("IntrinsicsProviderAPI.TangoService_getCameraIntrinsics() failed!");
            }
        }
        
        /// <summary>
        /// Sets the callback for notifications when image data is ready.
        /// </summary>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="onImageAvailable">On image available callback handler.</param>
        public static void SetCallback(TangoEnums.TangoCameraId cameraId, TangoService_onImageAvailable onImageAvailable)
        {
            int returnValue = VideoOverlayAPI.TangoService_connectOnFrameAvailable(cameraId, callbackContext, onImageAvailable);
            if(returnValue == Tango.Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was set.");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was not set!");
            }
        }
        
        #region NATIVE_FUNCTIONS
        /// <summary>
        /// Video overlay native function import.
        /// </summary>
        private struct VideoOverlayAPI
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectTextureId(TangoEnums.TangoCameraId cameraId, int textureHandle);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnFrameAvailable(TangoEnums.TangoCameraId cameraId,
                                                                          IntPtr context,
                                                                          [In,Out] TangoService_onImageAvailable onImageAvailable);
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_updateTexture(TangoEnums.TangoCameraId cameraId, ref double timestamp);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_getCameraIntrinsics(TangoEnums.TangoCameraId cameraId, [Out] TangoCameraIntrinsics intrinsics);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_Experimental_connectTextureIdUnity(TangoEnums.TangoCameraId id, 
                                                                                     uint texture_y,
                                                                                     uint texture_Cb,
                                                                                     uint texture_Cr,
                                                                                     IntPtr context, 
                                                                                     TangoService_onUnityFrameAvailable onUnityFrameAvailable);
            
            #else
            public static int TangoService_connectTextureId(TangoEnums.TangoCameraId cameraId, int textureHandle)
            {
                return Tango.Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_updateTexture(TangoEnums.TangoCameraId cameraId, ref double timestamp)
            {
                return Tango.Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_getCameraIntrinsics(TangoEnums.TangoCameraId cameraId, [Out] TangoCameraIntrinsics intrinsics)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_connectOnFrameAvailable(TangoEnums.TangoCameraId cameraId,
                                                                   IntPtr context,
                                                                   [In,Out] TangoService_onImageAvailable onImageAvailable)
            {
                return Tango.Common.ErrorType.TANGO_SUCCESS;
            }
            
            public static int TangoService_Experimental_connectTextureIdUnity(TangoEnums.TangoCameraId id, 
                                                                              uint texture_y,
                                                                              uint texture_Cb,
                                                                              uint texture_Cr,
                                                                              IntPtr context, 
                                                                              TangoService_onUnityFrameAvailable onUnityFrameAvailable)
            {
                return Tango.Common.ErrorType.TANGO_SUCCESS;
            }
            #endif
            #endregion
        }
    }
}