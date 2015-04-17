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

/// <summary>
/// Responsible for locating all object of types IBasePreRenderer
/// and IBasePostRenderer, then making sure their drawing code is called.
/// </summary>
public class CameraRenderer : MonoBehaviour
{
    private IBasePreRenderer[] m_allRenderers;
    private IBasePostRenderer[] m_allPostRenderers;
    private IBaseOnRenderImage[] m_allOnRenderImageRenderer;

    /// <summary>
    /// Get all references.
    /// </summary>
    private void Start()
    {
        m_allRenderers = GameObject.FindObjectsOfType(typeof(IBasePreRenderer)) as IBasePreRenderer[];
        m_allOnRenderImageRenderer = 
            GameObject.FindObjectsOfType(typeof(IBaseOnRenderImage)) as IBaseOnRenderImage[];
        m_allPostRenderers = GameObject.FindObjectsOfType(typeof(IBasePostRenderer)) as IBasePostRenderer[];
    }

    /// <summary>
    /// Call OnPreRender on all IBasePreRender objects.
    /// </summary>
    private void OnPreRender()
    {
        foreach (IBasePreRenderer renderer in m_allRenderers)
        {
            renderer.OnPreRender();
        }
    }

    /// <summary>
    /// Call OnPostRender on all IBasePostRender objects.
    /// </summary>
    private void OnPostRender()
    {
        foreach (IBasePostRenderer renderer in m_allPostRenderers)
        {
            renderer.OnPostRender();
        }
    }

    /// <summary>
    /// Call OnRenderImage on all IBaseOnRenderImage objects.
    /// </summary>
    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        bool isRendersActive = false;
        foreach (IBaseOnRenderImage renderer in m_allOnRenderImageRenderer)
        {
            if(renderer.gameObject.activeSelf)
            {
                isRendersActive = true;
                renderer.OnRenderImage(source, destination);
            }
        }
        if(!isRendersActive)
        {
            Graphics.Blit(source, destination);
        }
    }
}
