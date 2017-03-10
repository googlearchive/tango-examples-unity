//-----------------------------------------------------------------------
// <copyright file="PointToPointGUIController.cs" company="Google">
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
using System;
using System.Collections;
using System.Collections.Generic;
using Tango;
using UnityEngine;

/// <summary>
/// GUI controller to show distance data.
/// </summary>
public class PointToPointGUIController : MonoBehaviour, ITangoDepth
{
    // Constant values for overlay.
    public const float UI_LABEL_START_X = 15.0f;
    public const float UI_LABEL_START_Y = 15.0f;
    public const float UI_LABEL_SIZE_X = 1920.0f;
    public const float UI_LABEL_SIZE_Y = 35.0f;

    /// <summary>
    /// The point cloud object in the scene.
    /// </summary>
    public TangoPointCloud m_pointCloud;

    /// <summary>
    /// The line renderer to draw a line between two points.
    /// </summary>
    public LineRenderer m_lineRenderer;

    /// <summary>
    /// The scene's Tango application.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// If set, then the depth camera is on and we are waiting for the next
    /// depth update.
    /// </summary>
    private bool m_waitingForDepth;

    /// <summary>
    /// The older of the two points to measure.
    /// </summary>
    private Vector3 m_startPoint;

    /// <summary>
    /// The newer of the two points to measure.
    /// </summary>
    private Vector3 m_endPoint;

    /// <summary>
    /// The distance between the two selected points.
    /// </summary>
    private float m_distance;

    /// <summary>
    /// The text to display the distance.
    /// </summary>
    private string m_distanceText;

    /// <summary>
    /// Start this instance.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();

        m_tangoApplication.Register(this);
    }

    /// <summary>
    /// Unity destroy function.
    /// </summary>
    public void OnDestroy()
    {
        m_tangoApplication.Unregister(this);
    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    public void Update()
    {
        // Distance was found.
        m_distanceText = "Distance is " + m_distance + " meters.";

        _RenderLine();

        if (Input.GetMouseButtonDown(0))
        {
             StartCoroutine(_WaitForDepth(Input.mousePosition));
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            // This is a fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application
            // immediately results in a deadlocked app.
            AndroidHelper.AndroidQuit();
        }
    }

    /// <summary>
    /// Display simple GUI.
    /// </summary>
    public void OnGUI()
    {
        if (m_tangoApplication.HasRequiredPermissions)
        {
            GUI.color = Color.black;
            GUI.Label(new Rect(UI_LABEL_START_X,
                               UI_LABEL_START_Y,
                               UI_LABEL_SIZE_X,
                               UI_LABEL_SIZE_Y),
                      "<size=25>" + m_distanceText + "</size>");
        }
    }

    /// <summary>
    /// This is called each time new depth data is available.
    ///
    /// On the Tango tablet, the depth callback occurs at 5 Hz.
    /// </summary>
    /// <param name="tangoDepth">Tango depth.</param>
    public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
    {
        // Don't handle depth here because the PointCloud may not have been
        // updated yet. Just tell the coroutine it can continue.
        m_waitingForDepth = false;
    }

    /// <summary>
    /// This is called when successfully connected to Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
        m_tangoApplication.SetDepthCameraRate(
            TangoEnums.TangoDepthCameraRate.DISABLED);
    }

    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// <summary>
    /// Render the line from the start point to the end point.
    /// </summary>
    private void _RenderLine()
    {
        m_lineRenderer.SetPosition(0, m_startPoint);
        m_lineRenderer.SetPosition(1, m_endPoint);
    }

    /// <summary>
    /// Wait for the next depth update, then find the nearest point in the point
    /// cloud.
    /// </summary>
    /// <param name="touchPosition">Touch position on the screen.</param>
    /// <returns>Coroutine IEnumerator.</returns>
    private IEnumerator _WaitForDepth(Vector2 touchPosition)
    {
        m_waitingForDepth = true;

        // Turn on the camera and wait for a single depth update
        m_tangoApplication.SetDepthCameraRate(
            TangoEnums.TangoDepthCameraRate.MAXIMUM);
        while (m_waitingForDepth)
        {
            yield return null;
        }

        m_tangoApplication.SetDepthCameraRate(
            TangoEnums.TangoDepthCameraRate.DISABLED);

        Camera cam = Camera.main;
        int pointIndex = m_pointCloud.FindClosestPoint(cam, touchPosition, 10);

        if (pointIndex > -1)
        {
            // Index is valid
            m_startPoint = m_endPoint;
            m_endPoint = m_pointCloud.m_points[pointIndex];

            m_distance = Vector3.Distance(m_startPoint, m_endPoint);
        }
    }
}
