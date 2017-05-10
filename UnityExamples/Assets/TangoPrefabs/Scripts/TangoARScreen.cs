//-----------------------------------------------------------------------
// <copyright file="TangoARScreen.cs" company="Google">
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
using System.Collections;
using Tango;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// TangoARScreen takes the YUV image from the API, resizes the image plane, and
/// passes the YUV data and vertices data to the YUV2RGB shader to produce a
/// properly sized RGBA image.
/// 
/// Please note that all the YUV to RGB conversion is done through the YUV2RGB
/// shader. No computation is in this class, it only passes the data to the
/// shader.
/// </summary>
[RequireComponent(typeof(Camera)), DisallowMultipleComponent]
public class TangoARScreen : MonoBehaviour, ITangoLifecycle, ITangoCameraTexture
{   
    /// <summary>
    /// If set, m_updatePointsMesh in PointCloud also gets set. Then PointCloud 
    /// material's render queue is set to background-1 so that PointCloud data
    /// gets written to Z buffer for Depth test with virtual objects in scene.
    /// Note 1: This is a very rudimentary way of doing occlusion and limited by
    /// the capabilities of depth camera. Note 2: To enable occlusion
    /// TangoPointCloud prefab must be present in the scene as well.
    /// </summary>
    public bool m_enableOcclusion;

    /// <summary>
    /// The shader to use for rendering occlusion points.
    /// </summary>
    public Shader m_occlusionShader;

    /// <summary>
    /// The most recent time (in seconds) the screen was updated.
    /// </summary>
    [System.NonSerialized]
    public double m_screenUpdateTime;

    /// <summary>
    /// The tango application script in the scene.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Script that manages the postprocess distortion of the camera image.
    /// </summary>
    private ARCameraPostProcess m_arCameraPostProcess;

    /// <summary>
    /// Camera the TangoARScreen is attached to,
    /// to which the color camera image will be rendered.
    /// </summary>
    private Camera m_camera;

    /// <summary>
    /// U texture coordinate amount that the color camera image is being clipped on each side.
    ///
    /// Ranges from 0 (no clipping) to 0.5 (full clipping).
    /// </summary>
    private float m_uOffset;

    /// <summary>
    /// V texture coordinate amount that the color camera image is being clipped on each side.
    ///
    /// Ranges from 0 (no clipping) to 0.5 (full clipping).
    /// </summary>
    private float m_vOffset;

    /// <summary>
    /// Gets a value indicating whether the AR screen is rendering.
    /// </summary>
    /// <value>Whether the AR screen is rendering.</value>
    public bool IsRendering { get; private set; }

    /// <summary>
    /// Converts a normalized Unity viewport position into its corresponding normalized position on the color camera
    /// image.
    /// </summary>
    /// <returns>Normalized position for the color camera.</returns>
    /// <param name="pos">Normalized position for the 3D viewport.</param>
    public Vector2 ViewportPointToCameraImagePoint(Vector2 pos)
    {
        return new Vector2(Mathf.Lerp(m_uOffset, 1 - m_uOffset, pos.x),
                           Mathf.Lerp(m_vOffset, 1 - m_vOffset, pos.y));
    }

    /// <summary>
    /// Converts a color camera position into its corresponding normalized Unity viewport position.
    /// </summary>
    /// <returns>Normalized position for the color camera.</returns>
    /// <param name="pos">Normalized position for the 3D viewport.</param>
    public Vector2 CameraImagePointToViewportPoint(Vector2 pos)
    {
        return new Vector2((pos.x - m_uOffset) / (1 - (2 * m_uOffset)),
                           (pos.y - m_vOffset) / (1 - (2 * m_vOffset)));
    }

    /// @cond
    /// <summary>
    /// Initialize the AR Screen.
    /// </summary>
    public void Start()
    {
        m_camera = GetComponent<Camera>();
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_arCameraPostProcess = gameObject.GetComponent<ARCameraPostProcess>();

        if (m_tangoApplication != null)
        {
            m_tangoApplication.OnDisplayChanged += _OnDisplayChanged;
            m_tangoApplication.Register(this);

            // If already connected to a service, then do initialization now.
            if (m_tangoApplication.IsServiceConnected)
            {
                OnTangoServiceConnected();
            }
        }

        if (m_enableOcclusion) 
        {
            TangoPointCloud pointCloud = FindObjectOfType<TangoPointCloud>();
            if (pointCloud != null)
            {
                Renderer renderer = pointCloud.GetComponent<Renderer>();
                renderer.enabled = true;
                renderer.material.shader = m_occlusionShader;
                pointCloud.m_updatePointsMesh = true;
            }
            else
            {
                Debug.Log("Point Cloud data is not available, occlusion is not possible.");
            }
        }
    }

    /// <summary>
    /// Unity callback when the component gets destroyed.
    /// </summary>
    public void OnDestroy()
    {
        TangoApplication tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication != null)
        {
            tangoApplication.Unregister(this);
        }
    }
    
    /// <summary>
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
    }

    /// <summary>
    /// This is called when successfully connected to the Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
        // Disable 
        if (!m_tangoApplication.EnableVideoOverlay)
        {
            IsRendering = false;
            return;
        }

        CommandBuffer buf = VideoOverlayProvider.CreateARScreenCommandBuffer();
        m_camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, buf);
        m_camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, buf);
        _SetRenderAndCamera(AndroidHelper.GetDisplayRotation(), AndroidHelper.GetColorCameraRotation());
        IsRendering = true;
    }

    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// <summary>
    /// This will be called when a new frame is available from the camera.
    ///
    /// The first scan-line of the color image is reserved for metadata instead of image pixels.
    /// </summary>
    /// <param name="cameraId">Camera identifier.</param>
    public void OnTangoCameraTextureAvailable(TangoEnums.TangoCameraId cameraId)
    {
        if (IsRendering && cameraId == TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR)
        {
            m_screenUpdateTime = VideoOverlayProvider.UpdateARScreen(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);
        }
    }

    /// @endcond
    /// <summary>
    /// Rotate color camera render material's UV based on the color camera orientation and current activity orientation.
    /// </summary>
    /// <param name="uv">Input UV.</param>
    /// <param name="colorCamerRDisplay">Combined rotation index with color camera sensor and activity rotation.</param>
    /// <returns>Converted UV in Vector2.</returns>
    private static Vector2 _GetUnityUvBasedOnRotation(Vector2 uv, OrientationManager.Rotation colorCamerRDisplay)
    {
        switch (colorCamerRDisplay)
        {
        case OrientationManager.Rotation.ROTATION_270: 
            return new Vector2(1 - uv.y, 1 - uv.x);
        case OrientationManager.Rotation.ROTATION_180:
            return new Vector2(1 - uv.x, 0 + uv.y);
        case OrientationManager.Rotation.ROTATION_90:
            return new Vector2(0 + uv.y, 0 + uv.x);
        default:
            return new Vector2(0 + uv.x, 1 - uv.y);
        }
    }

    /// <summary>
    /// Update AR screen material with camera texture size data 
    /// (and distortion parameters if using distortion post-process filter).
    /// </summary>
    /// <param name="uOffset">U texcoord offset.</param>
    /// <param name="vOffset">V texcoord offset.</param>
    /// <param name="colorCameraRDisplay">Rotation of the display with respect to the color camera.</param> 
    private static void _MaterialUpdateForIntrinsics(
        float uOffset, float vOffset, OrientationManager.Rotation colorCameraRDisplay)
    {
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0 + uOffset, 0 + vOffset);
        uvs[1] = new Vector2(0 + uOffset, 1 - vOffset);
        uvs[2] = new Vector2(1 - uOffset, 0 + vOffset);
        uvs[3] = new Vector2(1 - uOffset, 1 - vOffset);

        for (int i = 0; i < 4; ++i)
        {
            uvs[i] = _GetUnityUvBasedOnRotation(uvs[i], colorCameraRDisplay);
        }
        
        VideoOverlayProvider.SetARScreenUVs(uvs);
    }

    /// <summary>
    /// Update a camera so its perspective lines up with the color camera's perspective.
    /// </summary>
    /// <param name="cam">Camera to update.</param>
    /// <param name="intrinsics">Tango camera intrinsics for the color camera.</param>
    /// <param name="uOffset">U texture coordinate clipping.</param>
    /// <param name="vOffset">V texture coordinate clipping.</param>
    private static void _CameraUpdateForIntrinsics(
            Camera cam, TangoCameraIntrinsics intrinsics, float uOffset, float vOffset)
    {
        float cx = (float)intrinsics.cx;
        float cy = (float)intrinsics.cy;
        float width = (float)intrinsics.width;
        float height = (float)intrinsics.height;

        float xscale = cam.nearClipPlane / (float)intrinsics.fx;
        float yscale = cam.nearClipPlane / (float)intrinsics.fy;

        float pixelLeft = -cx + (uOffset * width);
        float pixelRight = width - cx - (uOffset * width);

        // OpenGL coordinates has y pointing downwards so we negate this term.
        float pixelBottom = -height + cy + (vOffset * height);
        float pixelTop = cy - (vOffset * height);

        cam.projectionMatrix = _Frustum(pixelLeft * xscale, pixelRight * xscale,
                                        pixelBottom * yscale, pixelTop * yscale,
                                        cam.nearClipPlane, cam.farClipPlane);
    }

    /// <summary>
    /// Compute a projection matrix for a frustum.
    /// 
    /// This function's implementation is same as glFrustum.
    /// </summary>
    /// <returns>Projection matrix.</returns>
    /// <param name="left">Specify the coordinates for the left vertical clipping planes.</param>
    /// <param name="right">Specify the coordinates for the right vertical clipping planes.</param> 
    /// <param name="bottom">Specify the coordinates for the bottom horizontal clipping planes.</param> 
    /// <param name="top">Specify the coordinates for the top horizontal clipping planes.</param> 
    /// <param name="zNear">Specify the distances to the near depth clipping planes. Both distances must be positive.</param> 
    /// <param name="zFar">Specify the distances to the far depth clipping planes. Both distances must be positive.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*",
                                                     Justification = "Matrix visibility is more important.")]
    private static Matrix4x4 _Frustum(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, new Vector4(2.0f * zNear / (right - left), 0.0f,                          (right + left) / (right - left) , 0.0f));
        m.SetRow(1, new Vector4(0.0f,                          2.0f * zNear / (top - bottom), (top + bottom) / (top - bottom) , 0.0f));
        m.SetRow(2, new Vector4(0.0f,                          0.0f,                          -(zFar + zNear) / (zFar - zNear), -(2 * zFar * zNear) / (zFar - zNear)));
        m.SetRow(3, new Vector4(0.0f,                          0.0f,                          -1.0f,                            0.0f));
        return m;
    }

    /// <summary>
    /// Called when device orientation is changed.
    /// </summary>
    /// <param name="displayRotation">Orientation of current activity. Index enum is same as Android screen
    /// rotation standard.</param>
    /// <param name="colorCameraRotation">Orientation of current color camera sensor. Index enum is same as Android
    /// camera rotation standard.</param>
    private void _OnDisplayChanged(OrientationManager.Rotation displayRotation, 
        OrientationManager.Rotation colorCameraRotation)
    {
        if (IsRendering)
        {
            _SetRenderAndCamera(displayRotation, colorCameraRotation);
        }
    }

    /// <summary>
    /// Update AR screen rendering and attached Camera's projection matrix.
    /// </summary>
    /// <param name="displayRotation">Activity (screen) rotation.</param>
    /// <param name="colorCameraRotation">Color camera sensor rotation.</param>
    private void _SetRenderAndCamera(OrientationManager.Rotation displayRotation,
                                     OrientationManager.Rotation colorCameraRotation)
    {
        float cameraWidth = (float)Screen.width;
        float cameraHeight = (float)Screen.height;
        
        #pragma warning disable 0219
        // Here we are computing if current display orientation is landscape or portrait.
        // AndroidHelper.GetAndroidDefaultOrientation() returns 1 if device default orientation is in portrait,
        // returns 2 if device default orientation is landscape. Adding device default orientation with
        // how much the display is rotated from default orientation will get us the result of current display
        // orientation. (landscape vs. portrait)
        bool isLandscape = (AndroidHelper.GetDefaultOrientation() + (int)displayRotation) % 2 == 0;
        bool needToFlipCameraRatio = false;
        float cameraRatio = (float)Screen.width / (float)Screen.height;
        #pragma warning restore 0219
        
#if !UNITY_EDITOR
        // In most cases, we don't need to flip the camera width and height. However, in some cases Unity camera
        // only updates a couple of frames after the display changed callback from Android; thus, we need to flip the width
        // and height in this case.
        //
        // This does not happen in the editor, because the emulated device does not ever rotate.
        needToFlipCameraRatio = (!isLandscape & (cameraRatio > 1.0f)) || (isLandscape & (cameraRatio < 1.0f));

        if (needToFlipCameraRatio)
        {
            cameraRatio = 1.0f / cameraRatio;
            float tmp = cameraWidth;
            cameraWidth = cameraHeight;
            cameraHeight = tmp;
        }
#endif

        TangoCameraIntrinsics alignedIntrinsics = new TangoCameraIntrinsics();
        TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
        VideoOverlayProvider.GetDeviceOrientationAlignedIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR,
                                                                   alignedIntrinsics);
        VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR,
                                           intrinsics);

        if (alignedIntrinsics.width != 0 && alignedIntrinsics.height != 0)
        {
            // The camera to which this script is attached is an Augmented Reality camera.  The color camera
            // image must fill that camera's viewport.  That means we must clip the color camera image to make
            // its ratio the same as the Unity camera.  If we don't do this the color camera image will be
            // stretched non-uniformly, making a circle into an ellipse.
            float widthRatio = (float)cameraWidth / (float)alignedIntrinsics.width;
            float heightRatio = (float)cameraHeight / (float)alignedIntrinsics.height;

            if (widthRatio >= heightRatio)
            {
                m_uOffset = 0;
                m_vOffset = (1 - (heightRatio / widthRatio)) / 2;
            }
            else
            {
                m_uOffset = (1 - (widthRatio / heightRatio)) / 2;
                m_vOffset = 0;
            }

            // Note that here we are passing in non-inverted intrinsics, because the YUV conversion is still operating
            // on native buffer layout.
            OrientationManager.Rotation rotation = TangoSupport.RotateFromAToB(displayRotation, colorCameraRotation);
            _MaterialUpdateForIntrinsics(m_uOffset, m_vOffset, rotation);
            _CameraUpdateForIntrinsics(m_camera, alignedIntrinsics, m_uOffset, m_vOffset);
            if (m_arCameraPostProcess != null)
            {
                m_arCameraPostProcess.SetupIntrinsic(intrinsics);
            }
        }
        else
        {
            Debug.LogError("AR Camera intrinsic is not valid.");
        }
    }
}
