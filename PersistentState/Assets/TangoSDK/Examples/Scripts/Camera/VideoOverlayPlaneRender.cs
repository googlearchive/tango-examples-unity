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
/// Responsible for drawing the AR screen video overlay.
/// </summary>
public class VideoOverlayPlaneRender : IBasePreRenderer
{
    private int TEX_WIDTH = Screen.width;
    private int TEX_HEIGHT = Screen.height;
    private const TextureFormat TEX_FORMAT = TextureFormat.RGB565;

    private Texture2D m_texture;

    /// <summary>
    /// Perform any Camera.OnPreRender() logic
    /// here.
    /// </summary>
    public sealed override void OnPreRender()
    {
		VideoOverlayProvider.RenderLatestFrame(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);
        GL.InvalidateState();
    }

	public void SetTargetCameraTexture(TangoEnums.TangoCameraId cameraId)
    {
        VideoOverlayProvider.ConnectTexture(cameraId,
                                            m_texture.GetNativeTextureID());
    }

    /// <summary>
    /// Initialize this instance.
    /// </summary>
    private void Awake()
    {
        m_texture = new Texture2D(TEX_WIDTH, TEX_HEIGHT, TEX_FORMAT, false);
        renderer.material.mainTexture = m_texture;
    }
}