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
#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Flag set to true whenever emulated values have been updated.
        /// </summary>
        internal static bool m_emulationIsDirty;

        private const int EMULATED_CAMERA_WIDTH = 1280;
        private const int EMULATED_CAMERA_HEIGHT = 720;

        private const int EMULATED_CAMERA_PACKED_WIDTH = 1280 / 4;
        private const int EMULATED_CAMERA_PACKED_Y_HEIGHT = 720;
        private const int EMULATED_CAMERA_PACKED_UV_HEIGHT = 720 / 2;

        private const string EMULATED_RGB2YUV_Y_SHADERNAME = "Hidden/Tango/RGB2YUV_Y";
        private const string EMULATED_RGB2YUV_CBCR_SHADERNAME = "Hidden/Tango/RGB2YUV_CbCr";
#endif
        
        private static readonly string CLASS_NAME = "VideoOverlayProvider";
        private static IntPtr callbackContext = IntPtr.Zero;

#if UNITY_EDITOR
        /// <summary>
        /// Render target used to render environment for Tango emulation on PC.
        /// </summary>
        private static RenderTexture m_emulatedColorRenderTexture = null;

#if UNITY_EDITOR_WIN
        /// <summary>
        /// Most recent set of textures submitted from TangoApplication.
        /// </summary>
        private static YUVTexture m_emulationTexIdCaptureTextures;
#endif // UNITY_EDITOR_WIN

        /// <summary>
        /// Underlying Y texture when using experimental texture-ID method.
        /// </summary>
        private static RenderTexture m_emulatedExpId_Y = null;

        /// <summary>
        /// Underlying Y texture when using experimental texture-ID method.
        /// </summary>
        private static RenderTexture m_emulatedExpId_CbCr = null;
        
        /// <summary>
        /// Textures used to capture emulated color feed from render target.
        /// (First texture is Y component, second is CbCr).
        /// </summary>
        private static Texture2D[] m_emulationByteBufferCaptureTextures = null;

        /// <summary>
        /// Post-process filter used to turn an RGB texture into a texture
        /// containing the Y-component data expected by experimental texture-ID method.
        /// </summary>
        private static Material m_yuvFilterY;

        /// <summary>
        /// Post-process filter used to turn an RGB texture into a texture
        /// containing the Cb/Cr component data expected by experimental texture-ID method.
        /// </summary>
        private static Material m_yuvFilterCbCr;

        /// <summary>
        /// The theoretical capture time of most recent emulated color frame.
        /// </summary>
        private static float m_lastColorEmulationTime;

        /// <summary>
        /// Whether resources needed for emulation have been created.
        /// </summary>
        private static bool m_emulationIsInitialized = false;
#endif
        
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
#if UNITY_EDITOR
            if (m_emulatedExpId_Y != null && m_emulatedExpId_CbCr != null)
            {
                m_emulatedExpId_Y.DiscardContents();
                m_emulatedExpId_CbCr.DiscardContents();
                Graphics.Blit(m_emulatedColorRenderTexture, m_emulatedExpId_Y, m_yuvFilterY);
                Graphics.Blit(m_emulatedColorRenderTexture, m_emulatedExpId_CbCr, m_yuvFilterCbCr);
#if UNITY_EDITOR_WIN
                // A crash occurs when assigning the pointer of a Unity RenderTexture to a Texture2D in a DirectX
                // environment, so must use the slower technique of using normal Texture2D objects with ReadPixels().
                // See emulation comments in ExperimentalConnectTexture().
                RenderTexture.active = m_emulatedExpId_Y;
                m_emulationTexIdCaptureTextures.m_videoOverlayTextureY.ReadPixels(new Rect(0, 0,
                                                                                           m_emulatedExpId_Y.width,
                                                                                           m_emulatedExpId_Y.height),
                                                                                  0, 0, false);
                m_emulationTexIdCaptureTextures.m_videoOverlayTextureY.Apply();
                
                RenderTexture.active = m_emulatedExpId_CbCr;
                m_emulationTexIdCaptureTextures.m_videoOverlayTextureCb.ReadPixels(new Rect(0, 0,
                                                                                            m_emulatedExpId_CbCr.width,
                                                                                            m_emulatedExpId_CbCr.height),
                                                                                   0, 0, false);
                m_emulationTexIdCaptureTextures.m_videoOverlayTextureCb.Apply();
#endif  // UNITY_EDITOR_WIN
            }

            return m_lastColorEmulationTime;
#else

            double timestamp = 0.0;
            int returnValue = VideoOverlayAPI.TangoService_updateTexture(cameraId, ref timestamp);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.UpdateTexture() Texture was not updated by camera!");
            }
            
            return timestamp;
#endif
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

#if UNITY_EDITOR
            // In editor, base 'intrinsics' off of properties of emulation camera.
            if (cameraId == TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR && EmulatedEnvironmentRenderHelper.m_emulationCamera != null)
            {
                // Instantiate any resources that we haven't yet.
                if (!m_emulationIsInitialized)
                {
                    _InitializeResourcesForEmulation();
                    m_emulationIsInitialized = true;
                }

                EmulatedEnvironmentRenderHelper.m_emulationCamera.targetTexture = m_emulatedColorRenderTexture;

                intrinsics.width = (uint)EMULATED_CAMERA_WIDTH;
                intrinsics.height = (uint)EMULATED_CAMERA_HEIGHT;

                float height = Screen.height;
                float fov = EmulatedEnvironmentRenderHelper.m_emulationCamera.fieldOfView;
                float focalLengthInPixels = (1 / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad)) * (intrinsics.height * 0.5f);
                intrinsics.fy = intrinsics.fx = focalLengthInPixels;
                intrinsics.cx = intrinsics.width / 2f;
                intrinsics.cy = intrinsics.height / 2f;
            }
#endif
            
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
#if UNITY_EDITOR
            if (cameraId == TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR)
            {
                // Resize textures to to simulated width.
                textures.ResizeAll(EMULATED_CAMERA_PACKED_WIDTH, EMULATED_CAMERA_PACKED_Y_HEIGHT,
                                   EMULATED_CAMERA_PACKED_WIDTH, EMULATED_CAMERA_PACKED_UV_HEIGHT);

                if(!m_emulationIsInitialized)
                {
                    _InitializeResourcesForEmulation();
                    m_emulationIsInitialized = true;
                }
                
#if !UNITY_EDITOR_WIN
                // Rebind Texture2Ds to the underlying OpenGL texture ids of our render textures
                // Which is more or less the inverse of what the acutal tango service does but has the same effect.
                textures.m_videoOverlayTextureY.UpdateExternalTexture(m_emulatedExpId_Y.GetNativeTexturePtr());
                textures.m_videoOverlayTextureCb.UpdateExternalTexture(m_emulatedExpId_CbCr.GetNativeTexturePtr());
                textures.m_videoOverlayTextureCr.UpdateExternalTexture(m_emulatedExpId_CbCr.GetNativeTexturePtr());
#else   // !UNITY_EDITOR_WIN
                // A crash occurs when assigning the pointer of a Unity RenderTexture to a Texture2D (as above)
                // in a DirectX environment. Instead, size the Texture2D's correctly and copy render targets
                // with ReadPixels() when updating experimental textures. 
                // Keeping separate paths because ReadPixels() is a significant performance hit.

                textures.m_videoOverlayTextureY.Resize(m_emulatedExpId_Y.width, m_emulatedExpId_Y.height);
                textures.m_videoOverlayTextureCb.Resize(m_emulatedExpId_CbCr.width, m_emulatedExpId_CbCr.height);

                m_emulationTexIdCaptureTextures = textures;
#endif  // !UNITY_EDITOR_WIN
            }
#else

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
#endif
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

#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Update the Tango emulation state for color camera data.
        /// </summary>
        /// <param name="useByteBufferMethod">Whether to update emulation for byte-buffer method.</param>
        internal static void UpdateTangoEmulation(bool useByteBufferMethod)
        {
            // Get emulated position and rotation in Unity space.
            TangoPoseData poseData = new TangoPoseData();
            TangoCoordinateFramePair pair;
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE; 

            if (!PoseProvider.GetTimestampForColorEmulation(out m_lastColorEmulationTime))
            {
                Debug.LogError("Couldn't get a valid timestamp with which to emulate color camera. "
                               + "Color camera emulation will be skipped this frame.");
                return;
            }

            PoseProvider.GetPoseAtTime(poseData, m_lastColorEmulationTime, pair);
            if (poseData.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                return;
            }
            
            Vector3 position;
            Quaternion rotation;
            TangoSupport.TangoPoseToWorldTransform(poseData, out position, out rotation);

            // Instantiate any resources that we haven't yet.
            if(!m_emulationIsInitialized)
            {
                _InitializeResourcesForEmulation();
                m_emulationIsInitialized = true;
            }

            // Render. 
            EmulatedEnvironmentRenderHelper.RenderEmulatedEnvironment(m_emulatedColorRenderTexture,
                                                                      EmulatedEnvironmentRenderHelper.EmulatedDataType.COLOR_CAMERA,
                                                                      position, rotation);

            m_emulationIsDirty = true;
        }
        
        /// <summary>
        /// INTERNAL USE: Fill out most recent color data for Tango emulation.
        /// NOTE: Does not emulate first line of metadata in color buffer.
        /// </summary>
        /// <param name="colorImageData">TangoUnityImageData structure to update with emulated data.</param>
        internal static void GetTangoEmulation(TangoUnityImageData colorImageData)
        {
            int yDataSize = EMULATED_CAMERA_WIDTH * EMULATED_CAMERA_HEIGHT;
            int cbCrDataSize = yDataSize / 2;
            int dataSize = yDataSize + cbCrDataSize;
            if (colorImageData.data == null || colorImageData.data.Length != dataSize)
            {
                colorImageData.data = new byte[dataSize];
            }
            
            RenderTexture yRT = RenderTexture.GetTemporary(EMULATED_CAMERA_PACKED_WIDTH,
                                                           EMULATED_CAMERA_PACKED_Y_HEIGHT, 
                                                           0, RenderTextureFormat.ARGB32);
            RenderTexture cbCrRT = RenderTexture.GetTemporary(EMULATED_CAMERA_PACKED_WIDTH, 
                                                              EMULATED_CAMERA_PACKED_UV_HEIGHT, 
                                                              0, RenderTextureFormat.ARGB32);
            Graphics.Blit(m_emulatedColorRenderTexture, yRT, m_yuvFilterY);
            m_emulationByteBufferCaptureTextures[0].ReadPixels(new Rect(0, 0, yRT.width, yRT.height), 0, 0);
            Graphics.Blit(m_emulatedColorRenderTexture, cbCrRT, m_yuvFilterCbCr);
            m_emulationByteBufferCaptureTextures[1].ReadPixels(new Rect(0, 0, cbCrRT.width, cbCrRT.height), 0, 0);

            Color32[] colors = m_emulationByteBufferCaptureTextures[0].GetPixels32();
            for (int i = 0; i < yDataSize / 4; i++)
            {
                colorImageData.data[(i * 4)] = colors[i].r;
                colorImageData.data[(i * 4) + 1] = colors[i].g;
                colorImageData.data[(i * 4) + 2] = colors[i].b;
                colorImageData.data[(i * 4) + 3] = colors[i].a;
            }

            int startOffset = colors.Length * 4;
            colors = m_emulationByteBufferCaptureTextures[1].GetPixels32();
            for (int i = 0; i < cbCrDataSize / 4; i++)
            {
                colorImageData.data[(i * 4) + startOffset] = colors[i].r;
                colorImageData.data[(i * 4) + startOffset + 1] = colors[i].g;
                colorImageData.data[(i * 4) + startOffset + 2] = colors[i].b;
                colorImageData.data[(i * 4) + startOffset + 3] = colors[i].a;
            }

            RenderTexture.ReleaseTemporary(yRT);
            RenderTexture.ReleaseTemporary(cbCrRT);
            
            colorImageData.format = TangoEnums.TangoImageFormatType.TANGO_HAL_PIXEL_FORMAT_YV12;
            colorImageData.width = EMULATED_CAMERA_WIDTH;
            colorImageData.height = EMULATED_CAMERA_HEIGHT;
            colorImageData.stride = EMULATED_CAMERA_WIDTH;
            colorImageData.timestamp = m_lastColorEmulationTime;
        }

        /// <summary>
        /// Create any resources needed for emulation.
        /// </summary>
        private static void _InitializeResourcesForEmulation()
        {
            // Create textures:

            m_emulatedColorRenderTexture = new RenderTexture(EMULATED_CAMERA_WIDTH, 
                                                             EMULATED_CAMERA_HEIGHT, 
                                                             24, RenderTextureFormat.ARGB32);

            m_emulationByteBufferCaptureTextures = new Texture2D[2];
            m_emulationByteBufferCaptureTextures[0] = new Texture2D(EMULATED_CAMERA_PACKED_WIDTH,
                                                                    EMULATED_CAMERA_PACKED_Y_HEIGHT, 
                                                                    TextureFormat.ARGB32, false);
            m_emulationByteBufferCaptureTextures[1] = new Texture2D(EMULATED_CAMERA_PACKED_WIDTH,
                                                                    EMULATED_CAMERA_PACKED_UV_HEIGHT, 
                                                                    TextureFormat.ARGB32, false);

            m_emulatedExpId_Y = new RenderTexture(EMULATED_CAMERA_PACKED_WIDTH, 
                                                  EMULATED_CAMERA_PACKED_Y_HEIGHT, 
                                                  0, RenderTextureFormat.ARGB32);
            m_emulatedExpId_CbCr = new RenderTexture(EMULATED_CAMERA_PACKED_WIDTH, 
                                                     EMULATED_CAMERA_PACKED_UV_HEIGHT, 
                                                     0, RenderTextureFormat.ARGB32);
            m_emulatedExpId_Y.filterMode = FilterMode.Point;
            m_emulatedExpId_CbCr.filterMode = FilterMode.Point;
            m_emulatedExpId_Y.Create();
            m_emulatedExpId_CbCr.Create();

            // Find shaders by searching for them:
            if (EmulatedEnvironmentRenderHelper.CreateMaterialFromShaderName(EMULATED_RGB2YUV_Y_SHADERNAME,
                                                                             out m_yuvFilterY))
            {
                m_yuvFilterY.SetFloat("_TexWidth", EMULATED_CAMERA_WIDTH);
            }
            else
            {
                Debug.LogError("Could not find shader "
                               + EMULATED_RGB2YUV_Y_SHADERNAME
                               + ". Tango color camera emulation will not work correctly.");
            }

            if (EmulatedEnvironmentRenderHelper.CreateMaterialFromShaderName(EMULATED_RGB2YUV_CBCR_SHADERNAME, 
                                                                              out m_yuvFilterCbCr))
            {
                m_yuvFilterCbCr.SetFloat("_TexWidth", EMULATED_CAMERA_WIDTH);
            }
            else
            {
                Debug.LogError("Could not find shader "
                               + EMULATED_RGB2YUV_CBCR_SHADERNAME
                               + ". Tango color camera emulation will not work correctly.");
            }
        }
#endif
        
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
#if UNITY_EDITOR
            // Format needs to be ARGB32 in editor to use Texture2D.ReadPixels() in emulation
            // in Unity 4.6.
            format = TextureFormat.ARGB32;
#endif
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