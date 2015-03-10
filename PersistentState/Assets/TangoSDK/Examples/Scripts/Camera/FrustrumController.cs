//-----------------------------------------------------------------------
// <copyright file="FrustrumController.cs" company="Google">
//
// Copyright 2014 Google. Part of the Tango project. CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
//
// </copyright>
//-----------------------------------------------------------------------
using System.Collections;
using UnityEngine;

/// <summary>
/// Calculate and render the frustrum based on target object.
/// </summary>
public class FrustrumController : MonoBehaviour 
{
    public GameObject m_targetObject;

    // far end plane distance.
    public float m_distance;
    public Material m_lineMaterial;
	public Color m_frustumColor = Color.black;

	public float m_pixelBuffer = 0.1f;

    private bool m_isFrustrumEnabled = true;

    // 0 - camera position
    // 1 - left bottom corner
    // 2 - left top corner
    // 3 - right top corner
    // 4 - right bottom corner
    private Vector3[] m_frustrumlocationPosition;
    private Vector3[] m_frustrumWorldPosition;

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
    /// <param name="len"> Distance of farclip plane.</param>
    public void SetFarClipPlane(float farClipPlaneDistance)
    {
        Camera mainCamera = Camera.main;

        // screen corner
        Vector3 leftBottom, leftTop, rightBottom, rightTop;
        
		leftBottom = new Vector3(0.0f - m_pixelBuffer, 0.0f - m_pixelBuffer, farClipPlaneDistance);
		leftTop = new Vector3(0.0f - m_pixelBuffer, mainCamera.pixelHeight + m_pixelBuffer, farClipPlaneDistance);
		rightBottom = new Vector3(mainCamera.pixelWidth + m_pixelBuffer, 0.0f - m_pixelBuffer, farClipPlaneDistance);
		rightTop = new Vector3(mainCamera.pixelWidth + m_pixelBuffer, mainCamera.pixelHeight + m_pixelBuffer, farClipPlaneDistance);

        m_frustrumlocationPosition[0] = mainCamera.transform.position;
        m_frustrumlocationPosition[1] = mainCamera.ScreenToWorldPoint(leftBottom);
        m_frustrumlocationPosition[2] = mainCamera.ScreenToWorldPoint(leftTop);
        m_frustrumlocationPosition[3] = mainCamera.ScreenToWorldPoint(rightTop);
        m_frustrumlocationPosition[4] = mainCamera.ScreenToWorldPoint(rightBottom);

        Matrix4x4 worldToLocal = mainCamera.transform.worldToLocalMatrix;
        for (int i = 0; i < m_frustrumlocationPosition.Length; i++)
        {
            m_frustrumlocationPosition[i] = 
                worldToLocal.MultiplyPoint3x4(m_frustrumlocationPosition[i]);
        }
    }

    /// <summary>
    /// Use this for initialization.
    /// Unproject the points and make it to local to the cam obejct.
    /// </summary>
    private void Start()
    {
        m_frustrumlocationPosition = new Vector3[5];
        m_frustrumWorldPosition = new Vector3[5];
        SetFarClipPlane(m_distance);
    }
    
    /// <summary>
    /// Unity post render call back.
    /// Apply the VIO object's TRS to the points.
    /// </summary>
    private void OnPostRender() 
    {
        if (!m_isFrustrumEnabled)
        {
            return;
        }

        Matrix4x4 localToWorld = m_targetObject.transform.localToWorldMatrix;
        for (int i = 0; i < m_frustrumlocationPosition.Length; i++)
        {
            m_frustrumWorldPosition[i] = 
                localToWorld.MultiplyPoint3x4(m_frustrumlocationPosition[i]);
        }

        GL.PushMatrix();
        m_lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(m_frustumColor);
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.Vertex(m_frustrumWorldPosition[1]);
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.Vertex(m_frustrumWorldPosition[2]);
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.Vertex(m_frustrumWorldPosition[3]);
        GL.Vertex(m_frustrumWorldPosition[0]);
        GL.Vertex(m_frustrumWorldPosition[4]);
        
        GL.Vertex(m_frustrumWorldPosition[1]);
        GL.Vertex(m_frustrumWorldPosition[2]);
        GL.Vertex(m_frustrumWorldPosition[2]);
        GL.Vertex(m_frustrumWorldPosition[3]);
        GL.Vertex(m_frustrumWorldPosition[3]);
        GL.Vertex(m_frustrumWorldPosition[4]);
        GL.Vertex(m_frustrumWorldPosition[4]);
        GL.Vertex(m_frustrumWorldPosition[1]);
        GL.End();
        GL.PopMatrix();
    }
}
