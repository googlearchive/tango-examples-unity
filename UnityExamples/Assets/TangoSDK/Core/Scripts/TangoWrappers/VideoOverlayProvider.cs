//-----------------------------------------------------------------------
// <copyright file="VideoOverlayProvider.cs" company="Google">
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
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Tango;
    using UnityEngine;

    /// <summary>
    /// C API wrapper for the Tango video overlay interface.
    /// </summary>
    public class VideoOverlayProvider
    {
        private static readonly string CLASS_NAME = "VideoOverlayProvider";
        private static IntPtr callbackContext = IntPtr.Zero;

        /// <summary>
        /// Tango video overlay C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="cameraId">Camera ID.</param>
        /// <param name="image">Image buffer.</param> 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onImageAvailable(IntPtr callbackContext, Tango.TangoEnums.TangoCameraId cameraId, [In, Out] TangoImageBuffer image);

        /// <summary>
        /// Experimental API only, subject to change.  Tango depth C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="cameraId">Camera ID.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onUnityFrameAvailable(IntPtr callbackContext, Tango.TangoEnums.TangoCameraId cameraId);

        /// <summary>
        /// Connect a Texture ID to a camera; the camera is selected by specifying a TangoCameraId.
        /// 
        /// Currently only TANGO_CAMERA_COLOR and TANGO_CAMERA_FISHEYE are supported. The texture must be the ID of a
        /// texture that has been allocated and initialized by the calling application.
        /// 
        /// The first scan-line of the color image is reserved for metadata instead of image pixels.
        /// </summary>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to. Only TANGO_CAMERA_COLOR and TANGO_CAMERA_FISHEYE are
        /// supported.
        /// </param>
        /// <param name="textureId">
        /// The texture ID of the texture to connect the camera to. Must be a valid texture in the applicaton.
        /// </param>
        public static void ConnectTexture(TangoEnums.TangoCameraId cameraId, int textureId)
        {
            int returnValue = VideoOverlayAPI.TangoService_connectTextureId(cameraId, textureId);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.ConnectTexture() Texture was not connected to camera!");
            }
        }

        /// <summary>
        /// Update the texture that has been connected to camera referenced by TangoCameraId with the latest image
        /// from the camera.
        /// </summary>
        /// <returns>The timestamp of the image that has been pushed to the connected texture.</returns>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to.  Only <code>TANGO_CAMERA_COLOR</code> and
        /// <code>TANGO_CAMERA_FISHEYE</code> are supported.
        /// </param>
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
        /// Get the intrinsic calibration parameters for a given camera.
        /// 
        /// The intrinsics are as specified by the TangoCameraIntrinsics struct.  Intrinsics are read from the
        /// on-device intrinsics file (typically <code>/sdcard/config/calibration.xml</code>, but to ensure 
        /// compatibility applications should only access these parameters via the API), or default internal model 
        /// parameters corresponding to the device are used if the calibration.xml file is not found.
        /// </summary>
        /// <param name="cameraId">The camera ID to retrieve the calibration intrinsics for.</param>
        /// <param name="intrinsics">A TangoCameraIntrinsics filled with calibration intrinsics for the camera.</param>
        public static void GetIntrinsics(TangoEnums.TangoCameraId cameraId, [Out] TangoCameraIntrinsics intrinsics)
        {
            int returnValue = VideoOverlayAPI.TangoService_getCameraIntrinsics(cameraId, intrinsics);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("IntrinsicsProviderAPI.TangoService_getCameraIntrinsics() failed!");
            }
        }

        /// <summary>
        /// Experimental API only, subject to change.  Connect a Texture IDs to a camera.
        /// 
        /// The camera is selected via TangoCameraId.  Currently only TANGO_CAMERA_COLOR is supported.  The texture
        /// handles will be regenerated by the API on startup after which the application can use them, and will be
        /// packed RGBA8888 data containing bytes of the image (so a single RGBA8888 will pack 4 neighbouring pixels).
        /// If the config flag experimental_image_pixel_format is set to HAL_PIXEL_FORMAT_YCrCb_420_SP, texture_y will
        /// pack 1280x720 pixels into a 320x720 RGBA8888 texture.  texture_Cb and texture_Cr will contain copies of
        /// the 2x2 downsampled interleaved UV planes packed similarly.  If experimental_image_pixel_format is set to
        /// HAL_PIXEL_FORMAT_YV12 then texture_y will have a stride of 1536 containing 1280 columns of data, packed
        /// similarly in a RGBA8888 texture. texture_Cb and texture_Cr will be 2x2 downsampled versions of the same.  
        /// See YV12 and NV21 formats for details.
        /// 
        /// Note: The first scan-line of the color image is reserved for metadata instead of image pixels.
        /// </summary>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to.  Only TANGO_CAMERA_COLOR and TANGO_CAMERA_FISHEYE are
        /// supported.
        /// </param>
        /// <param name="textures">The texture IDs to use for the Y, Cb, and Cr planes.</param>
        /// <param name="onUnityFrameAvailable">Callback method.</param>
        internal static void ExperimentalConnectTexture(TangoEnums.TangoCameraId cameraId, YUVTexture textures, TangoService_onUnityFrameAvailable onUnityFrameAvailable)
        {
            int returnValue = VideoOverlayAPI.TangoService_Experimental_connectTextureIdUnity(
                cameraId, 
                (uint)textures.m_videoOverlayTextureY.GetNativeTexturePtr().ToInt64(), 
                (uint)textures.m_videoOverlayTextureCb.GetNativeTexturePtr().ToInt64(), 
                (uint)textures.m_videoOverlayTextureCr.GetNativeTexturePtr().ToInt64(), 
                callbackContext, 
                onUnityFrameAvailable);
            
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.ConnectTexture() Texture was not connected to camera!");
            }
        }

        /// <summary>
        /// Connect a callback to a camera for access to the pixels.
        /// 
        /// This is not recommended for display but for applications requiring access to the
        /// <code>HAL_PIXEL_FORMAT_YV12</code> pixel data.  The camera is selected via TangoCameraId.  Currently only 
        /// <code>TANGO_CAMERA_COLOR</code> and <code>TANGO_CAMERA_FISHEYE</code> are supported.
        /// 
        /// The <i>onImageAvailable</i> callback will be called when a new frame is available from the camera. The
        /// Enable Video Overlay option must be enabled for this to succeed.
        ///
        /// Note: The first scan-line of the color image is reserved for metadata instead of image pixels.
        /// </summary>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to.  Only <code>TANGO_CAMERA_COLOR</code> and
        /// <code>TANGO_CAMERA_FISHEYE</code> are supported.
        /// </param>
        /// <param name="onImageAvailable">Function called when a new frame is available from the camera.</param>
        internal static void SetCallback(TangoEnums.TangoCameraId cameraId, TangoService_onImageAvailable onImageAvailable)
        {
            int returnValue = VideoOverlayAPI.TangoService_connectOnFrameAvailable(cameraId, callbackContext, onImageAvailable);
            if (returnValue == Tango.Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was set.");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".SetCallback() Callback was not set!");
            }
        }
        
        #region NATIVE_FUNCTIONS
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct VideoOverlayAPI
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectTextureId(TangoEnums.TangoCameraId cameraId, int textureHandle);
            
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnFrameAvailable(TangoEnums.TangoCameraId cameraId,
                                                                          IntPtr context,
                                                                          [In, Out] TangoService_onImageAvailable onImageAvailable);
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
                                                                   [In, Out] TangoService_onImageAvailable onImageAvailable)
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

    /// <summary>
    /// Wraps separate textures for Y, U, and V planes.
    /// </summary>
    public class YUVTexture
    {
        /// <summary>
        /// The m_video overlay texture y.
        /// Columns     1280/4 [bytes packed in RGBA channels]
        /// Rows        720
        /// This size is for a 1280x720 screen.
        /// </summary>
        public Texture2D m_videoOverlayTextureY;
        
        /// <summary>
        /// The m_video overlay texture cb.
        /// Columns     640/4 [bytes packed in RGBA channels]
        /// Rows        360
        /// This size is for a 1280x720 screen.
        /// </summary>
        public Texture2D m_videoOverlayTextureCb;
        
        /// <summary>
        /// The m_video overlay texture cr.
        /// Columns     640 * 2 / 4 [bytes packed in RGBA channels]
        /// Rows        360
        /// This size is for a 1280x720 screen.
        /// </summary>
        public Texture2D m_videoOverlayTextureCr;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Tango.YUVTexture"/> class.
        /// NOTE : Texture resolutions will be reset by the API. The sizes passed
        /// into the constructor are not guaranteed to persist when running on device.
        /// </summary>
        /// <param name="yPlaneWidth">Y plane width.</param>
        /// <param name="yPlaneHeight">Y plane height.</param>
        /// <param name="uvPlaneWidth">UV plane width.</param>
        /// <param name="uvPlaneHeight">UV plane height.</param>
        /// <param name="format">Texture format.</param>
        /// <param name="mipmap">If set to <c>true</c> mipmap.</param>
        public YUVTexture(int yPlaneWidth, int yPlaneHeight,
                          int uvPlaneWidth, int uvPlaneHeight,
                          TextureFormat format, bool mipmap)
        {
            m_videoOverlayTextureY = new Texture2D(yPlaneWidth, yPlaneHeight, format, mipmap);
            m_videoOverlayTextureY.filterMode = FilterMode.Point;
            m_videoOverlayTextureCb = new Texture2D(uvPlaneWidth, uvPlaneHeight, format, mipmap);
            m_videoOverlayTextureCb.filterMode = FilterMode.Point;
            m_videoOverlayTextureCr = new Texture2D(uvPlaneWidth, uvPlaneHeight, format, mipmap);
            m_videoOverlayTextureCr.filterMode = FilterMode.Point;
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
}