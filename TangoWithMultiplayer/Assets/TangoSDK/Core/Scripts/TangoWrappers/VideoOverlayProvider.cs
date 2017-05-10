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
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// C API wrapper for the Tango video overlay interface.
    /// </summary>
    public class VideoOverlayProvider
    {
#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Dimension of simulated color camera textures.
        /// </summary>
        internal const int EMULATED_CAMERA_PACKED_WIDTH = 1280 / 4;

        /// <summary>
        /// INTERNAL USE: Dimension of simulated color camera Y texture.
        /// </summary>
        internal const int EMULATED_CAMERA_PACKED_Y_HEIGHT = 720;

        /// <summary>
        /// INTERNAL USE: Dimension of simulated color camera CbCr texture.
        /// </summary>
        internal const int EMULATED_CAMERA_PACKED_UV_HEIGHT = 720 / 2;

        /// <summary>
        /// INTERNAL USE: Flag set to true whenever emulated values have been updated.
        /// </summary>
        internal static bool m_emulationIsDirty;

        private const int EMULATED_CAMERA_WIDTH = 1280;
        private const int EMULATED_CAMERA_HEIGHT = 720;

        private const string EMULATED_RGB2YUV_Y_SHADERNAME = "Hidden/Tango/RGB2YUV_Y";
        private const string EMULATED_RGB2YUV_CBCR_SHADERNAME = "Hidden/Tango/RGB2YUV_CbCr";
        private const string EMULATED_RGB_ARSCREEN_SHADERNAME = "Hidden/Tango/RGB_ARScreen";
#endif

        private static readonly string CLASS_NAME = "VideoOverlayProvider";

#if UNITY_EDITOR
        /// <summary>
        /// Render target used to render environment for Tango emulation on PC.
        /// </summary>
        private static RenderTexture m_emulatedColorRenderTexture = null;

        /// <summary>
        /// Underlying Y texture when using experimental texture-ID method.
        /// </summary>
        private static RenderTexture m_emulatedExpId_Y = null;

        /// <summary>
        /// Underlying Y texture when using experimental texture-ID method.
        /// </summary>
        private static RenderTexture m_emulatedExpId_CbCr = null;

        /// <summary>
        /// Underlying RGB texture for the AR Screen.
        /// </summary>
        private static RenderTexture m_emulatedARScreenTexture = null;

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
        /// Simple material that emulates the AR Screen.
        /// </summary>
        private static Material m_emulationArScreenMaterial;

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
        /// <param name="context">Callback context.</param>
        /// <param name="cameraId">Camera ID.</param>
        /// <param name="image">Image buffer.</param>
        /// <param name="cameraMetadata">Camera metadata.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void APIOnImageAvailable(
            IntPtr context, TangoEnums.TangoCameraId cameraId, ref TangoImage image,
            ref TangoCameraMetadata cameraMetadata);

        /// <summary>
        /// Tango camera texture C callback function signature.
        /// </summary>
        /// <param name="context">Callback context.</param>
        /// <param name="cameraId">Camera ID.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void APIOnTextureAvailable(IntPtr context, TangoEnums.TangoCameraId cameraId);

        /// <summary>
        /// DEPRECATED: Update the texture that has been connected to camera referenced by TangoCameraId with the latest image
        /// from the camera.
        /// </summary>
        /// <returns>The timestamp of the image that has been pushed to the connected texture.</returns>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to.  Only <c>TANGO_CAMERA_COLOR</c> is supported.
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
            }

            return m_lastColorEmulationTime;
#else

            double timestamp = 0.0;
            int returnValue = API.TangoService_updateTexture(cameraId, ref timestamp);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("VideoOverlayProvider.UpdateTexture() Texture was not updated by camera!");
            }

            return timestamp;
#endif
        }

        /// <summary>
        /// Create a command buffer that renders the AR screen full screen at the right time.
        /// </summary>
        /// <returns>The AR screen command buffer.</returns>
        public static CommandBuffer CreateARScreenCommandBuffer()
        {
            CommandBuffer buf = new CommandBuffer();
#if UNITY_EDITOR
            _InternResourcesForEmulation();

            buf.Blit((Texture)m_emulatedARScreenTexture, BuiltinRenderTextureType.CurrentActive,
                     m_emulationArScreenMaterial);
#else
            IntPtr func = API.TangoUnity_getRenderTextureFunction();
            buf.IssuePluginEvent(func, 0);
#endif
            return buf;
        }

        /// <summary>
        /// Set the AR screen rendering UVs.  This affects how much of the screen is visible.
        /// </summary>
        /// <param name="uv">
        /// Array of four UV coordinates in order: bottom left, top left, bottom right, top right.
        /// </param>
        public static void SetARScreenUVs(Vector2[] uv)
        {
#if UNITY_EDITOR
            if (m_emulationArScreenMaterial != null)
            {
                m_emulationArScreenMaterial.SetVector("_UVBottomLeft", new Vector4(uv[0].x, uv[0].y, 0, 0));
                m_emulationArScreenMaterial.SetVector("_UVTopLeft", new Vector4(uv[1].x, uv[1].y, 0, 0));
                m_emulationArScreenMaterial.SetVector("_UVBottomRight", new Vector4(uv[2].x, uv[2].y, 0, 0));
                m_emulationArScreenMaterial.SetVector("_UVTopRight", new Vector4(uv[3].x, uv[3].y, 0, 0));
            }
#else
            API.TangoUnity_setRenderTextureUVs(uv);
#endif
        }

        /// <summary>
        /// Set the AR screen rendering distortion parameters.  This affects correcting for the curvature of the lens.
        /// </summary>
        /// <param name="rectifyImage">If <c>true</c>, rectify the AR screen image when rendering.</param>
        public static void SetARScreenDistortion(bool rectifyImage)
        {
#if UNITY_EDITOR
            // There is no distortion in emulation.
#else
            if (!rectifyImage)
            {
                API.TangoUnity_setRenderTextureDistortion(null);
            }
            else
            {
                TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
                GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);
                API.TangoUnity_setRenderTextureDistortion(intrinsics);
            }
#endif
        }

        /// <summary>
        /// Update the AR screen's texture to the most recent state of the specified camera.
        /// </summary>
        /// <returns>The timestamp of the image that has been pushed to the AR screen's texture.</returns>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to.  Only <c>TANGO_CAMERA_COLOR</c> is supported.
        /// </param>
        public static double UpdateARScreen(TangoEnums.TangoCameraId cameraId)
        {
#if UNITY_EDITOR
            if (m_emulatedARScreenTexture != null)
            {
                m_emulatedARScreenTexture.DiscardContents();
                Graphics.Blit(m_emulatedColorRenderTexture, m_emulatedARScreenTexture);
            }

            return m_lastColorEmulationTime;
#else
            double timestamp = 0.0;
            uint tex = API.TangoUnity_getArTexture();
            int returnValue = API.TangoService_updateTextureExternalOes(cameraId, tex, out timestamp);

            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("Unable to update texture. " + Environment.StackTrace);
            }

            // Rendering the latest frame changes a bunch of OpenGL state.  Ensure Unity knows the current OpenGL
            // state.
            GL.InvalidateState();

            return timestamp;
#endif
        }

        /// <summary>
        /// Get the intrinsic calibration parameters for a given camera, this also aligns the camera intrinsics based
        /// on device orientation.
        ///
        /// For example, if the device orientation is portrait and camera intrinsics is in
        /// landscape. This function will inverse the intrinsic x and y, and report intrinsics in portrait mode.
        ///
        /// The intrinsics are as specified by the TangoCameraIntrinsics struct and are accessed via the API.
        /// </summary>
        /// <param name="cameraId">The camera ID to retrieve the calibration intrinsics for.</param>
        /// <param name="alignedIntrinsics">
        /// A TangoCameraIntrinsics filled with calibration intrinsics for the camera, this intrinsics is also
        /// aligned with device orientation.
        /// </param>
        public static void GetDeviceOrientationAlignedIntrinsics(TangoEnums.TangoCameraId cameraId,
                                                                 TangoCameraIntrinsics alignedIntrinsics)
        {
            TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
            GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);

            float intrinsicsRatio = (float)intrinsics.width / (float)intrinsics.height;
            Tango.OrientationManager.Rotation rotation = TangoSupport.RotateFromAToB(
                AndroidHelper.GetDisplayRotation(), 
                AndroidHelper.GetColorCameraRotation());

            switch (rotation) 
            {
                case Tango.OrientationManager.Rotation.ROTATION_90:
                    alignedIntrinsics.cx = intrinsics.cy;
                    alignedIntrinsics.cy = intrinsics.width - intrinsics.cx;
                    alignedIntrinsics.fx = intrinsics.fy;
                    alignedIntrinsics.fy = intrinsics.fx;
                    alignedIntrinsics.width = intrinsics.height;
                    alignedIntrinsics.height = intrinsics.width;
                    break;
                case Tango.OrientationManager.Rotation.ROTATION_180:
                    alignedIntrinsics.cx = intrinsics.width - intrinsics.cx;
                    alignedIntrinsics.cy = intrinsics.height - intrinsics.cy;
                    alignedIntrinsics.fx = intrinsics.fx;
                    alignedIntrinsics.fy = intrinsics.fy;
                    alignedIntrinsics.width = intrinsics.width;
                    alignedIntrinsics.height = intrinsics.height;
                    break;
                case Tango.OrientationManager.Rotation.ROTATION_270:
                    alignedIntrinsics.cx = intrinsics.height - intrinsics.cy;
                    alignedIntrinsics.cy = intrinsics.cx;
                    alignedIntrinsics.fx = intrinsics.fy;
                    alignedIntrinsics.fy = intrinsics.fx;
                    alignedIntrinsics.width = intrinsics.height;
                    alignedIntrinsics.height = intrinsics.width;
                    break;
                default:
                    alignedIntrinsics.cx = intrinsics.cx;
                    alignedIntrinsics.cy = intrinsics.cy;
                    alignedIntrinsics.fx = intrinsics.fx;
                    alignedIntrinsics.fy = intrinsics.fy;
                    alignedIntrinsics.width = intrinsics.width;
                    alignedIntrinsics.height = intrinsics.height;
                    break;
            }

            alignedIntrinsics.distortion0 = intrinsics.distortion0;
            alignedIntrinsics.distortion1 = intrinsics.distortion1;
            alignedIntrinsics.distortion2 = intrinsics.distortion2;
            alignedIntrinsics.distortion3 = intrinsics.distortion3;
            alignedIntrinsics.distortion4 = intrinsics.distortion4;
            alignedIntrinsics.camera_id = intrinsics.camera_id;
            alignedIntrinsics.calibration_type = intrinsics.calibration_type;
        }

        /// <summary>
        /// Get the intrinsic calibration parameters for a given camera.
        ///
        /// The intrinsics are as specified by the TangoCameraIntrinsics struct and are accessed via the API.
        /// </summary>
        /// <param name="cameraId">The camera ID to retrieve the calibration intrinsics for.</param>
        /// <param name="intrinsics">A TangoCameraIntrinsics filled with calibration intrinsics for the camera.</param>
        public static void GetIntrinsics(TangoEnums.TangoCameraId cameraId, TangoCameraIntrinsics intrinsics)
        {
            int returnValue = API.TangoService_getCameraIntrinsics(cameraId, intrinsics);

#if UNITY_EDITOR
            // In editor, base 'intrinsics' off of properties of emulation camera.
            if (cameraId == TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR && EmulatedEnvironmentRenderHelper.m_emulationCamera != null)
            {
                // Instantiate any resources that we haven't yet.
                _InternResourcesForEmulation();

                EmulatedEnvironmentRenderHelper.m_emulationCamera.targetTexture = m_emulatedColorRenderTexture;

                intrinsics.width = (uint)EMULATED_CAMERA_WIDTH;
                intrinsics.height = (uint)EMULATED_CAMERA_HEIGHT;

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
        /// DEPRECATED: Connect a Texture IDs to a camera.
        ///
        /// The camera is selected via TangoCameraId.  Currently only TANGO_CAMERA_COLOR is supported.  The texture
        /// handles will be regenerated by the API on startup after which the application can use them, and will be
        /// packed RGBA8888 data containing bytes of the image (so a single RGBA8888 will pack 4 neighboring pixels).
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
        /// <param name="callback">Callback method.</param>
        internal static void ExperimentalConnectTexture(
            TangoEnums.TangoCameraId cameraId, YUVTexture textures, APIOnTextureAvailable callback)
        {
#if UNITY_EDITOR
            if (cameraId == TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR)
            {
                m_emulatedExpId_Y = (RenderTexture)textures.m_videoOverlayTextureY;
                m_emulatedExpId_CbCr = (RenderTexture)textures.m_videoOverlayTextureCb;
            }
#else
            int returnValue = API.TangoService_Experimental_connectTextureIdUnity(
                cameraId,
                (uint)textures.m_videoOverlayTextureY.GetNativeTexturePtr().ToInt64(),
                (uint)textures.m_videoOverlayTextureCb.GetNativeTexturePtr().ToInt64(),
                (uint)textures.m_videoOverlayTextureCr.GetNativeTexturePtr().ToInt64(),
                IntPtr.Zero,
                callback);

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
        /// <param name="callback">Function called when a new frame is available from the camera.</param>
        internal static void SetCallback(TangoEnums.TangoCameraId cameraId, APIOnImageAvailable callback)
        {
            int returnValue = API.TangoService_connectOnImageAvailable(cameraId, IntPtr.Zero, callback);
            bool success = returnValue == Common.ErrorType.TANGO_SUCCESS;
            Debug.LogFormat("{0}.SetCallback(OnImageAvailable) Callback was {1}set.",
                            CLASS_NAME, success ? string.Empty : "not ");
        }

        /// <summary>
        /// Connect a callback to a camera for texture updates.
        /// </summary>
        /// <param name="cameraId">
        /// The ID of the camera to connect this texture to.  Only <code>TANGO_CAMERA_COLOR</code> and
        /// <code>TANGO_CAMERA_FISHEYE</code> are supported.
        /// </param>
        /// <param name="callback">Function called when a new frame is available from the camera.</param>
        internal static void SetCallback(TangoEnums.TangoCameraId cameraId, APIOnTextureAvailable callback)
        {
            int returnValue = API.TangoService_connectOnTextureAvailable(cameraId, IntPtr.Zero, callback);
            if (returnValue == Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".SetCallback(OnTextureAvailable) Callback was set.");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".SetCallback(OnTextureAvailable) Callback was not set!");
            }
        }

        /// <summary>
        /// Clear all camera callbacks.
        /// </summary>
        /// <param name="cameraId">Camera identifier.</param>
        internal static void ClearCallback(TangoEnums.TangoCameraId cameraId)
        {
            int returnValue = API.TangoService_Experimental_connectTextureIdUnity(cameraId, 0, 0, 0,
                                                                                  IntPtr.Zero, null);
            if (returnValue == Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Unity callback was cleared.");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Unity callback was not cleared!");
            }

            returnValue = API.TangoService_connectOnImageAvailable(cameraId, IntPtr.Zero, null);
            if (returnValue == Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Frame callback was cleared.");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Frame callback was not cleared!");
            }

            returnValue = API.TangoService_connectOnTextureAvailable(cameraId, IntPtr.Zero, null);
            if (returnValue == Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Texture callback was cleared.");
            }
            else
            {
                Debug.Log(CLASS_NAME + ".ClearCallback() Texture callback was not cleared!");
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
            _InternResourcesForEmulation();

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
        private static void _InternResourcesForEmulation()
        {
            if(m_emulationIsInitialized)
            {
                return;
            }

            // Create textures:

            m_emulatedColorRenderTexture = new RenderTexture(EMULATED_CAMERA_WIDTH, EMULATED_CAMERA_HEIGHT, 24,
                                                             RenderTextureFormat.ARGB32);
            m_emulatedARScreenTexture = new RenderTexture(EMULATED_CAMERA_WIDTH, EMULATED_CAMERA_HEIGHT, 0,
                                                          RenderTextureFormat.ARGB32);

            m_emulationByteBufferCaptureTextures = new Texture2D[2];
            m_emulationByteBufferCaptureTextures[0] = new Texture2D(EMULATED_CAMERA_PACKED_WIDTH,
                                                                    EMULATED_CAMERA_PACKED_Y_HEIGHT,
                                                                    TextureFormat.ARGB32, false);
            m_emulationByteBufferCaptureTextures[1] = new Texture2D(EMULATED_CAMERA_PACKED_WIDTH,
                                                                    EMULATED_CAMERA_PACKED_UV_HEIGHT,
                                                                    TextureFormat.ARGB32, false);

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

            if (EmulatedEnvironmentRenderHelper.CreateMaterialFromShaderName(EMULATED_RGB_ARSCREEN_SHADERNAME,
                                                                             out m_emulationArScreenMaterial))
            {
                m_emulationArScreenMaterial.SetVector("_UVBottomLeft", new Vector4(0, 0, 0, 0));
                m_emulationArScreenMaterial.SetVector("_UVBottomRight", new Vector4(0, 1, 0, 0));
                m_emulationArScreenMaterial.SetVector("_UVTopLeft", new Vector4(1, 0, 0, 0));
                m_emulationArScreenMaterial.SetVector("_UVTopRight", new Vector4(1, 1, 0, 0));
            }
            else
            {
                Debug.LogError("Could not find shader "
                               + EMULATED_RGB_ARSCREEN_SHADERNAME
                               + ". Tango color camera emulation will not work correctly.");
            }

            m_emulationIsInitialized = true;
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct API
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_updateTexture(
                TangoEnums.TangoCameraId cameraId, ref double timestamp);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_updateTextureExternalOes(
                TangoEnums.TangoCameraId cameraId, UInt32 glTextureId, out double timestamp);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_getCameraIntrinsics(
                TangoEnums.TangoCameraId cameraId, [Out] TangoCameraIntrinsics intrinsics);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_connectOnImageAvailable(
                TangoEnums.TangoCameraId cameraId, IntPtr context,
                [In, Out] APIOnImageAvailable callback);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_connectOnTextureAvailable(
                TangoEnums.TangoCameraId cameraId, IntPtr ContextMenu, APIOnTextureAvailable callback);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_Experimental_connectTextureIdUnity(
                TangoEnums.TangoCameraId id, UInt32 texture_y, UInt32 texture_Cb, UInt32 texture_Cr, IntPtr context,
                APIOnTextureAvailable callback);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern UInt32 TangoUnity_getArTexture();

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern void TangoUnity_setRenderTextureUVs(Vector2[] uv);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern void TangoUnity_setRenderTextureDistortion(TangoCameraIntrinsics intrinsics);

            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern IntPtr TangoUnity_getRenderTextureFunction();
#else
            public static int TangoService_updateTexture(TangoEnums.TangoCameraId cameraId, ref double timestamp)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_updateTextureExternalOes(
                TangoEnums.TangoCameraId cameraId, UInt32 glTextureId, out double timestamp)
            {
                timestamp = 0;
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_getCameraIntrinsics(
                TangoEnums.TangoCameraId cameraId, [Out] TangoCameraIntrinsics intrinsics)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_connectOnImageAvailable(
                TangoEnums.TangoCameraId cameraId, IntPtr context,
                [In, Out] APIOnImageAvailable callback)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_connectOnTextureAvailable(
                TangoEnums.TangoCameraId cameraId, IntPtr context, APIOnTextureAvailable callback)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoService_Experimental_connectTextureIdUnity(
                TangoEnums.TangoCameraId id, UInt32 texture_y, UInt32 texture_Cb, UInt32 texture_Cr,
                IntPtr context, APIOnTextureAvailable callback)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static UInt32 TangoUnity_getArTexture()
            {
                return 0;
            }

            public static void TangoUnity_setRenderTextureUVs(Vector2[] uv)
            {
            }

            public static void TangoUnity_setRenderTextureDistortion(TangoCameraIntrinsics intrinsics)
            {
            }

            public static IntPtr TangoUnity_getRenderTextureFunction()
            {
                return IntPtr.Zero;
            }
#endif
        }
    }

    /// <summary>
    /// DEPRECATED: Wraps separate textures for Y, U, and V planes.
    /// </summary>
    public class YUVTexture
    {
        /// <summary>
        /// The m_video overlay texture y.
        /// Columns     1280/4 [bytes packed in RGBA channels]
        /// Rows        720
        /// This size is for a 1280x720 screen.
        /// </summary>
        public readonly Texture m_videoOverlayTextureY;

        /// <summary>
        /// The m_video overlay texture cb.
        /// Columns     640/4 [bytes packed in RGBA channels]
        /// Rows        360
        /// This size is for a 1280x720 screen.
        /// </summary>
        public readonly Texture m_videoOverlayTextureCb;

        /// <summary>
        /// The m_video overlay texture cr.
        /// Columns     640 * 2 / 4 [bytes packed in RGBA channels]
        /// Rows        360
        /// This size is for a 1280x720 screen.
        /// </summary>
        public readonly Texture m_videoOverlayTextureCr;

        /// <summary>
        /// Initializes a new instance of the <see cref="YUVTexture"/> class.
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
            // We always know the simulated 'camera resolution' in the Editor, so we can also do away with the width/height options:
            yPlaneWidth = VideoOverlayProvider.EMULATED_CAMERA_PACKED_WIDTH;
            yPlaneHeight = VideoOverlayProvider.EMULATED_CAMERA_PACKED_Y_HEIGHT;
            uvPlaneWidth = VideoOverlayProvider.EMULATED_CAMERA_PACKED_WIDTH;
            uvPlaneHeight = VideoOverlayProvider.EMULATED_CAMERA_PACKED_UV_HEIGHT;

            // Format needs to be ARGB32 in editor to use Texture2D.ReadPixels() in emulation
            // in Unity 4.6.
            RenderTexture Y = new RenderTexture(yPlaneWidth, yPlaneHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture CbCr = new RenderTexture(uvPlaneWidth, uvPlaneHeight, 0, RenderTextureFormat.ARGB32);
            Y.Create();
            CbCr.Create();

            m_videoOverlayTextureY = Y;
            m_videoOverlayTextureCb = CbCr;
            m_videoOverlayTextureCr = CbCr;
#else
            m_videoOverlayTextureY = new Texture2D(yPlaneWidth, yPlaneHeight, format, mipmap);
            m_videoOverlayTextureCb = new Texture2D(uvPlaneWidth, uvPlaneHeight, format, mipmap);
            m_videoOverlayTextureCr = new Texture2D(uvPlaneWidth, uvPlaneHeight, format, mipmap);
#endif
            m_videoOverlayTextureY.filterMode = FilterMode.Point;
            m_videoOverlayTextureCb.filterMode = FilterMode.Point;
            m_videoOverlayTextureCr.filterMode = FilterMode.Point;
        }

        /// <summary>
        /// Resizes all YUV texture planes.
        /// </summary>
        /// <param name="yPlaneWidth">Y plane width.</param>
        /// <param name="yPlaneHeight">Y plane height.</param>
        /// <param name="uvPlaneWidth">UV plane width.</param>
        /// <param name="uvPlaneHeight">UV plane height.</param>
        public void ResizeAll(int yPlaneWidth, int yPlaneHeight,
                              int uvPlaneWidth, int uvPlaneHeight)
        {
#if !UNITY_EDITOR
            ((Texture2D)m_videoOverlayTextureY).Resize(yPlaneWidth, yPlaneHeight);
            ((Texture2D)m_videoOverlayTextureCb).Resize(uvPlaneWidth, uvPlaneHeight);
            ((Texture2D)m_videoOverlayTextureCr).Resize(uvPlaneWidth, uvPlaneHeight);
#endif
        }
    }
}
