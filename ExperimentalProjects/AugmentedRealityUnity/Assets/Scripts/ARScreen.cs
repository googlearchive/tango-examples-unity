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
using System.Collections;
using UnityEngine;
using Tango;

/// <summary>
/// ARScreen takes the YUV image from the API, resize the image plane and passes
/// the YUV data and vertices data to the YUV2RGB shader to produce a properly
/// sized RGBA image.
/// 
/// Please note that all the YUV to RGB conversion is done through the YUV2RGB
/// shader, no computation is in this class, this class only passes the data to
/// shader.
/// </summary>
public class ARScreen : MonoBehaviour
{
    public Camera m_renderCamera;
    public Material m_screenMaterial;
    
    private TangoApplication m_tangoApplication;
    private YUVTexture m_textures;
    
    /// <summary>
    /// Set up the size of ARScreen based on camera intrinsics.
    /// </summary>
    public void SetCameraIntrinsics()
    {
        TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
        VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);
        
        float verticalFOV = 2.0f * Mathf.Rad2Deg * Mathf.Atan((intrinsics.height * 0.5f) / (float)intrinsics.fy);
        if(!float.IsNaN(verticalFOV))
        {
            m_renderCamera.fieldOfView = verticalFOV;

            // Here we are scaling the image plane to make sure the image plane's ratio is set as the
            // color camera image ratio.
            // If we don't do this, because we are drawing the texture fullscreen, the image plane will
            // be set to the screen's ratio.
            float widthRatio = (float)Screen.width / (float)intrinsics.width;
            float heightRatio = (float)Screen.height / (float)intrinsics.height;
            if (widthRatio >= heightRatio)
            {
                float normalizedOffset = (widthRatio / heightRatio - 1.0f) / 2.0f;
                _SetScreenVertices(0, normalizedOffset);
            }
            else
            {
                float normalizedOffset = (heightRatio / widthRatio - 1.0f) / 2.0f;
                _SetScreenVertices(normalizedOffset, 0);
            }
        }
    }
    
    /// <summary>
    /// Initialize the AR Screen.
    /// </summary>
    private void Start()
    {
        m_tangoApplication = FindObjectOfType<Tango.TangoApplication>();
        if(m_tangoApplication != null)
        {
            m_textures = m_tangoApplication.GetVideoOverlayTextureYUV();
            // Pass YUV textures to shader for process.
            m_screenMaterial.SetTexture("_YTex", m_textures.m_videoOverlayTextureY);
            m_screenMaterial.SetTexture("_UTex", m_textures.m_videoOverlayTextureCb);
            m_screenMaterial.SetTexture("_VTex", m_textures.m_videoOverlayTextureCr);
        }
        
        m_tangoApplication.Register(this);
    }

    /// <summary>
    /// Unity update function, we update our texture from here.
    /// </summary>
    private void Update() {
        VideoOverlayProvider.RenderLatestFrame (TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);
        GL.InvalidateState ();
    }

    /// <summary>
    /// Set the screen (video overlay image plane) size and vertices. The image plane is not
    /// applying any project matrix or view matrix. So it's drawing space is the normalized
    /// screen space, that is [-1.0f, 1.0f] for both width and height.
    /// </summary>
    private void _SetScreenVertices(float normalizedOffsetX, float normalizedOffsetY)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        mesh.Clear ();
        
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
        
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.uv = uvs;
        meshFilter.mesh = mesh;
        mesh.RecalculateNormals();
    }
}
