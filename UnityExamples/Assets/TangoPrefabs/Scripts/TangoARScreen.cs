//-----------------------------------------------------------------------
// <copyright file="TangoARScreen.cs" company="Google">
//
// Copyright 2015 Google Inc. All Rights Reserved.
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
using UnityEngine;
using Tango;

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
public class TangoARScreen : MonoBehaviour
{
    /// <summary>
    /// Set this to the AR Screen material.
    /// </summary>
    public Material m_screenMaterial;

    /// <summary>
    /// The most recent time (in seconds) the screen was updated.
    /// </summary>
    [HideInInspector]
    public double m_screenUpdateTime;

    private TangoApplication m_tangoApplication;
    private YUVTexture m_textures;

    /// <summary>
    /// Initialize the AR Screen.
    /// </summary>
    private void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        if (m_tangoApplication != null)
        {
            m_tangoApplication.RegisterOnTangoConnect(_SetCameraIntrinsics);

            // Pass YUV textures to shader for process.
            m_textures = m_tangoApplication.GetVideoOverlayTextureYUV();
            m_screenMaterial.SetTexture("_YTex", m_textures.m_videoOverlayTextureY);
            m_screenMaterial.SetTexture("_UTex", m_textures.m_videoOverlayTextureCb);
            m_screenMaterial.SetTexture("_VTex", m_textures.m_videoOverlayTextureCr);
        }
    }

    /// <summary>
    /// Unity update function, we update our texture from here.
    /// </summary>
    private void Update()
    {
        m_screenUpdateTime = VideoOverlayProvider.RenderLatestFrame(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);

        // Rendering the latest frame changes a bunch of OpenGL state.  Ensure Unity knows the current OpenGL state.
        GL.InvalidateState();
    }
    
    /// <summary>
    /// Set the screen (video overlay image plane) size and vertices. The image plane is not
    /// applying any project matrix or view matrix. So it's drawing space is the normalized
    /// screen space, that is [-1.0f, 1.0f] for both width and height.
    /// </summary>
    /// <param name="normalizedOffsetX">Horizontal padding to add to the left and right edges.</param>
    /// <param name="normalizedOffsetY">Vertical padding to add to top and bottom edges.</param> 
    private void _SetScreenVertices(float normalizedOffsetX, float normalizedOffsetY)
    {
        // Set the vertices base on the offset, note that the offset is used to compensate
        // the ratio differences between the camera image and device screen.
        Vector3[] verts = new Vector3[4];
        verts[0] = new Vector3(-1.0f - normalizedOffsetX, -1.0f - normalizedOffsetY, 1.0f);
        verts[1] = new Vector3(-1.0f - normalizedOffsetX, 1.0f + normalizedOffsetY, 1.0f);
        verts[2] = new Vector3(1.0f + normalizedOffsetX, 1.0f + normalizedOffsetY, 1.0f);
        verts[3] = new Vector3(1.0f + normalizedOffsetX, -1.0f - normalizedOffsetY, 1.0f);

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
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, 1f);
        uvs[2] = new Vector2(1f, 1f);
        uvs[3] = new Vector2(1f, 0);

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.uv = uvs;

        // Make this mesh never fail the occlusion cull
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
    }
    
    /// <summary>
    /// Set up the size of ARScreen based on camera intrinsics.
    /// </summary>
    private void _SetCameraIntrinsics()
    {
        TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
        VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);

        if (intrinsics.width != 0 && intrinsics.height != 0)
        {
            Camera.main.projectionMatrix = ProjectionMatrixForCameraIntrinsics((float)intrinsics.width,
                                                                               (float)intrinsics.height,
                                                                               (float)intrinsics.fx,
                                                                               (float)intrinsics.fy,
                                                                               (float)intrinsics.cx,
                                                                               (float)intrinsics.cy,
                                                                               0.1f,
                                                                               1000.0f);

            // Here we are scaling the image plane to make sure the image plane's ratio is set as the
            // color camera image ratio.
            // If we don't do this, because we are drawing the texture fullscreen, the image plane will
            // be set to the screen's ratio.
            float widthRatio = (float)Screen.width / (float)intrinsics.width;
            float heightRatio = (float)Screen.height / (float)intrinsics.height;
            if (widthRatio >= heightRatio)
            {
                float normalizedOffset = ((widthRatio / heightRatio) - 1.0f) / 2.0f;
                _SetScreenVertices(0, normalizedOffset);
            }
            else
            {
                float normalizedOffset = ((heightRatio / widthRatio) - 1.0f) / 2.0f;
                _SetScreenVertices(normalizedOffset, 0);
            }
        }
    }

    /// <summary>
    /// Compute a projection matrix from window size, camera intrinsics, and clip settings.
    /// </summary>
    /// <returns>Projection matrix.</returns>
    /// <param name="width">The width of the camera image.</param>
    /// <param name="height">The height of the camera image.</param> 
    /// <param name="fx">The x-axis focal length of the camera.</param> 
    /// <param name="fy">The y-axis focal length of the camera.</param> 
    /// <param name="cx">The x-coordinate principal point in pixels.</param> 
    /// <param name="cy">The y-coordinate principal point in pixels.</param> 
    /// <param name="near">The desired near z-clipping plane.</param> 
    /// <param name="far">The desired far z-clipping plane.</param> 
    private Matrix4x4 ProjectionMatrixForCameraIntrinsics(float width, float height,
                                                          float fx, float fy,
                                                          float cx, float cy,
                                                          float near, float far)
    {
        float xscale = near / fx;
        float yscale = near / fy;
        
        float xoffset = (cx - (width  / 2.0f)) * xscale;

        // OpenGL coordinates has y pointing downwards so we negate this term.
        float yoffset = -(cy - (height / 2.0f)) * yscale;
        
        return Frustum((xscale * -width / 2.0f) - xoffset,
                       (xscale * +width / 2.0f) - xoffset,
                       (yscale * -height / 2.0f) - yoffset,
                       (yscale * +height / 2.0f) - yoffset,
                       near, far);
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
    private Matrix4x4 Frustum(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, new Vector4(2.0f * zNear / (right - left), 0.0f,                          (right + left) / (right - left) , 0.0f));
        m.SetRow(1, new Vector4(0.0f,                          2.0f * zNear / (top - bottom), (top + bottom) / (top - bottom) , 0.0f));
        m.SetRow(2, new Vector4(0.0f,                          0.0f,                          -(zFar + zNear) / (zFar - zNear), -(2 * zFar * zNear) / (zFar - zNear)));
        m.SetRow(3, new Vector4(0.0f,                          0.0f,                          -1.0f,                            0.0f));
        return m;
    }
}
