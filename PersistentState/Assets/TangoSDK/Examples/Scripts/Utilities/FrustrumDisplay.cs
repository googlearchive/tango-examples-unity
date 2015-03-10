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
/// Calculate and render the frustrum based on target object.
/// </summary>
[RequireComponent(typeof(ARScreen))]
public class FrustrumDisplay : IBasePostRenderer 
{
    private GameObject m_targetObject;
    
    // far end plane distance.
    private float m_distance;
    private Material m_lineMaterial;
    
    private bool m_isFrustrumEnabled = true;

    private Vector3[] m_frustrumlocationPosition;
    private Vector3[] m_frustrumWorldPosition;
    private Vector3[] m_frontFaceVertices;
    
    /// <summary>
    /// Enable/disable the frustrum render.
    /// </summary>
    /// <param name="enabled"> If enable or disable.</param>
    public void SetEnable(bool enabled)
    {
        m_isFrustrumEnabled = enabled;
    }
    
    /// <summary>
    /// Set the distance of farclip plane.
    /// </summary>
    /// <param name="farClipPlaneDistance"> Distance of farclip plane.</param>
    public void SetFarClipPlane(float farClipPlaneDistance)
    {
        Camera mainCamera = Camera.main;

        // screen corner
        Vector3 leftBottom, leftTop, rightBottom, rightTop;

        leftBottom = new Vector3(0.0f, 0.0f, farClipPlaneDistance);
        leftTop = new Vector3(0.0f, mainCamera.pixelHeight, farClipPlaneDistance);
        rightBottom = new Vector3(mainCamera.pixelWidth, 0.0f, farClipPlaneDistance);
        rightTop = new Vector3(mainCamera.pixelWidth, mainCamera.pixelHeight, farClipPlaneDistance);

        m_frustrumlocationPosition[0] = mainCamera.ScreenToWorldPoint(leftBottom);
        m_frustrumlocationPosition[1] = mainCamera.ScreenToWorldPoint(leftTop);
        m_frustrumlocationPosition[2] = mainCamera.ScreenToWorldPoint(rightTop);
        m_frustrumlocationPosition[3] = mainCamera.ScreenToWorldPoint(rightBottom);

        Matrix4x4 worldToLocal = mainCamera.transform.worldToLocalMatrix;

        int lengthOfArray = m_frustrumlocationPosition.Length;
        for (int i = 0; i < lengthOfArray; i++)
        {
            m_frustrumlocationPosition[i] = 
                worldToLocal.MultiplyPoint3x4(m_frustrumlocationPosition[i]);
        }
    }

    /// <summary>
    /// Unity post render call back.
    /// Apply the VIO object's TRS to the points.
    /// </summary>
    public override void OnPostRender() 
    {
        if (!m_isFrustrumEnabled)
        {
            return;
        }
        m_frontFaceVertices[0] = 
            new Vector3(-transform.parent.gameObject.transform.localScale.x * 0.5f,
                        -transform.parent.gameObject.transform.localScale.y * 0.5f,
                        transform.parent.gameObject.transform.localScale.z * 0.5f);
        
        m_frontFaceVertices[1] = 
            new Vector3(-transform.parent.gameObject.transform.localScale.x * 0.5f,
                        transform.parent.gameObject.transform.localScale.y * 0.5f,
                        transform.parent.gameObject.transform.localScale.z * 0.5f);
        
        m_frontFaceVertices[2] = 
            new Vector3(transform.parent.gameObject.transform.localScale.x * 0.5f,
                        transform.parent.gameObject.transform.localScale.y * 0.5f,
                        transform.parent.gameObject.transform.localScale.z * 0.5f);
        
        m_frontFaceVertices[3] = 
            new Vector3(transform.parent.gameObject.transform.localScale.x * 0.5f,
                        -transform.parent.gameObject.transform.localScale.y * 0.5f,
                        transform.parent.gameObject.transform.localScale.z * 0.5f);
        Matrix4x4 localToWorld = m_targetObject.transform.localToWorldMatrix;

        int lengthOfArray = m_frustrumlocationPosition.Length;
        for (int i = 0; i < lengthOfArray; i++)
        {
            m_frustrumWorldPosition[i] = 
                localToWorld.MultiplyPoint3x4(m_frustrumlocationPosition[i]);
        }

        lengthOfArray = m_frontFaceVertices.Length;
        for (int i = 0; i < m_frontFaceVertices.Length; i++)
        {
            m_frontFaceVertices[i] = localToWorld.MultiplyPoint3x4(m_frontFaceVertices[i]);
        }
        
        GL.PushMatrix();
        m_lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        
        GL.Vertex(m_frontFaceVertices[0]);
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.Vertex(m_frontFaceVertices[1]);
        GL.Vertex(m_frustrumWorldPosition[1]);
        GL.Vertex(m_frontFaceVertices[2]);
        GL.Vertex(m_frustrumWorldPosition[2]);
        GL.Vertex(m_frontFaceVertices[3]);
        GL.Vertex(m_frustrumWorldPosition[3]);
        
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.Vertex(m_frustrumWorldPosition[1]);
        GL.Vertex(m_frustrumWorldPosition[1]);
        GL.Vertex(m_frustrumWorldPosition[2]);
        GL.Vertex(m_frustrumWorldPosition[2]);
        GL.Vertex(m_frustrumWorldPosition[3]);
        GL.Vertex(m_frustrumWorldPosition[3]);
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.End();
        GL.PopMatrix();
    }

    /// <summary>
    /// Use this for initialization.
    /// Unproject the points and make it to local to the cam obejct.
    /// </summary>
    private void Start()
    {
        m_frustrumlocationPosition = new Vector3[5];
        m_frustrumWorldPosition = new Vector3[5];
        m_frontFaceVertices = new Vector3[4];
        ARScreen screenRef = GetComponent<ARScreen>();
        if (transform.parent == null)
        {
			Debug.Log("Use ArScreen with TangoController only.");
        }
        else
        {
            m_targetObject = transform.parent.gameObject;
        }

        // The frustum needs to be drawn keeping these parameter in mind
        m_distance = transform.localPosition.z + screenRef.m_frustumLength;

        SetFarClipPlane(m_distance);
        m_lineMaterial = Resources.Load("Materials/GLMaterial") as Material;
    }
}