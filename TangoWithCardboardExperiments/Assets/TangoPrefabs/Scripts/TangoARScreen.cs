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

/// <summary>
/// TangoARScreen takes the YUV image from the API, resize the image plane and passes
/// the YUV data and vertices data to the YUV2RGB shader to produce a properly
/// sized RGBA image.
/// 
/// Please note that all the YUV to RGB conversion is done through the YUV2RGB
/// shader, no computation is in this class, this class only passes the data to
/// shader.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TangoARScreen : MonoBehaviour, ITangoLifecycle, IExperimentalTangoVideoOverlay
{   
    /// <summary>
    /// If set, m_updatePointsMesh in PointCloud also gets set. Then PointCloud material's renderqueue is set to 
    /// background-1 so that PointCloud data gets written to Z buffer for Depth test with virtual
    /// objects in scene. Note 1: This is a very rudimentary way of doing occlusion and limited by the capabilities of
    /// depth camera. Note 2: To enable occlusion TangoPointCloud prefab must be present in the scene as well.
    /// </summary>
    public bool m_enableOcclusion;

    /// <summary>
    /// Set this to the AR Screen material.
    /// </summary>
    public Material m_screenMaterial;

    /// <summary>
    /// The most recent time (in seconds) the screen was updated.
    /// </summary>
    [HideInInspector]
    public double m_screenUpdateTime;

    /// <summary>
    /// The Background renderqueue's number.
    /// </summary>
    private const int BACKGROUND_RENDER_QUEUE = 1000;
    
    /// <summary>
    /// Point size of PointCloud data when projected on to image plane.
    /// </summary>
    private const int POINTCLOUD_SPLATTER_UPSAMPLE_SIZE = 30;

    /// <summary>
    /// Script that manages the postprocess distortiotn of the camera image.
    /// </summary>
    private ARCameraPostProcess m_arCameraPostProcess;

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
    /// image.
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
        TangoApplication tangoApplication = FindObjectOfType<TangoApplication>();
        m_arCameraPostProcess = gameObject.GetComponent<ARCameraPostProcess>();
        if (tangoApplication != null)
        {
            tangoApplication.Register(this);

            // If already connected to a service, then do initialization now.
            if (tangoApplication.IsServiceConnected)
            {
                OnTangoServiceConnected();
            }

            // Pass YUV textures to shader for process.
            YUVTexture textures = tangoApplication.GetVideoOverlayTextureYUV();
            m_screenMaterial.SetTexture("_YTex", textures.m_videoOverlayTextureY);
            m_screenMaterial.SetTexture("_UTex", textures.m_videoOverlayTextureCb);
            m_screenMaterial.SetTexture("_VTex", textures.m_videoOverlayTextureCr);
        }

        if (m_enableOcclusion) 
        {
            TangoPointCloud pointCloud = FindObjectOfType<TangoPointCloud>();
            if (pointCloud != null)
            {
                Renderer renderer = pointCloud.GetComponent<Renderer>();
                renderer.enabled = true;

                // Set the renderpass as background renderqueue's number minus one. YUV2RGB shader executes in 
                // Background queue which is 1000.
                // But since we want to write depth data to Z buffer before YUV2RGB shader executes so that YUV2RGB 
                // data ignores Ztest from the depth data we set renderqueue of PointCloud as 999.
                renderer.material.renderQueue = BACKGROUND_RENDER_QUEUE - 1;
                renderer.material.SetFloat("point_size", POINTCLOUD_SPLATTER_UPSAMPLE_SIZE);
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
    /// This is called when succesfully connected to the Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
        // Set up the size of ARScreen based on camera intrinsics.
        TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
        VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);
        if (intrinsics.width != 0 && intrinsics.height != 0)
        {
            Camera camera = GetComponent<Camera>();
            if (camera != null)
            {
                // If this script is attached to a camera, then the camera is an Augmented Reality camera.  The color
                // camera image then must fill the viewport.  That means we must clip the color camera image to make
                // its ratio the same as the Unity camera.  If we don't do this the color camera image will be
                // stretched non-uniformly, making a circle into an ellipse.
                float widthRatio = (float)camera.pixelWidth / (float)intrinsics.width;
                float heightRatio = (float)camera.pixelHeight / (float)intrinsics.height;
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

                m_arCameraPostProcess.SetupIntrinsic(intrinsics);
                _MeshUpdateForIntrinsics(GetComponent<MeshFilter>().mesh, m_uOffset, m_vOffset);
                _CameraUpdateForIntrinsics(camera, intrinsics, m_uOffset, m_vOffset);
            }
        }
        else
        {
            m_uOffset = 0;
            m_vOffset = 0;
            m_arCameraPostProcess.enabled = false;
        }
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
    public void OnExperimentalTangoImageAvailable(TangoEnums.TangoCameraId cameraId)
    {
        if (cameraId == TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR)
        {
            m_screenUpdateTime = VideoOverlayProvider.RenderLatestFrame(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);

            // Rendering the latest frame changes a bunch of OpenGL state.  Ensure Unity knows the current OpenGL state.
            GL.InvalidateState();
        }
    }

    /// @endcond
    /// <summary>
    /// Update a mesh so it can be used for the Video Overlay image plane.
    ///
    /// The image plane is drawn without any projection or view matrix transforms, so it must line up exactly with
    /// normalized screen space.  The texture coordinates of the mesh are adjusted so it properly clips the image
    /// plane.
    /// </summary>
    /// <param name="mesh">Mesh to update.</param> 
    /// <param name="uOffset">U texture coordinate clipping.</param>
    /// <param name="vOffset">V texture coordinate clipping.</param>
    private static void _MeshUpdateForIntrinsics(Mesh mesh, float uOffset, float vOffset)
    {
        // Set the vertices base on the offset, note that the offset is used to compensate
        // the ratio differences between the camera image and device screen.
        Vector3[] verts = new Vector3[4];
        verts[0] = new Vector3(-1, -1, 1);
        verts[1] = new Vector3(-1, +1, 1);
        verts[2] = new Vector3(+1, +1, 1);
        verts[3] = new Vector3(+1, -1, 1);

        // Set indices.
        int[] indices = new int[6];
        indices[0] = 0;
        indices[1] = 2;
        indices[2] = 3;
        indices[3] = 1;
        indices[4] = 2;
        indices[5] = 0;

        // Set UVs.
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0 + uOffset, 0 + vOffset);
        uvs[1] = new Vector2(0 + uOffset, 1 - vOffset);
        uvs[2] = new Vector2(1 - uOffset, 1 - vOffset);
        uvs[3] = new Vector2(1 - uOffset, 0 + vOffset);

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.uv = uvs;

        // Make this mesh never fail the occlusion cull
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
    }

    /// <summary>
    /// Update a camera so its perspective lines up with the color camera's perspective.
    /// </summary>
    /// <param name="cam">Camera to update.</param>
    /// <param name="intrinsics">Tango camera intrinsics for the color camera.</param>
    /// <param name="uOffset">U texture coordinate clipping.</param>
    /// <param name="vOffset">V texture coordinate clipping.</param>
    private static void _CameraUpdateForIntrinsics(Camera cam, TangoCameraIntrinsics intrinsics, float uOffset,
                                                   float vOffset)
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
}
