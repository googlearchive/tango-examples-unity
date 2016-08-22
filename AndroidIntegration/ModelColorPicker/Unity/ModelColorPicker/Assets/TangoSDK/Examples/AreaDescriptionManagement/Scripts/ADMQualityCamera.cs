//-----------------------------------------------------------------------
// <copyright file="ADMQualityCamera.cs" company="Google">
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
using UnityEngine;

/// <summary>
/// Camera controller that smoothly moves to make sure the entire quality visualization is in view.
/// </summary>
public class ADMQualityCamera : MonoBehaviour
{
    /// <summary>
    /// Maximum speed the camera center moves.
    /// </summary>
    public const float MOVEMENT_SPEED = 8;

    /// <summary>
    /// Maximum amount the camera's orthographic size changes.
    /// </summary>
    public const float ZOOM_SPEED = 8;

    /// <summary>
    /// Minimum allowed value for the camera's orthographic size.
    /// </summary>
    public const float MIN_ZOOM = 2;

    /// <summary>
    /// The component managing the visualization quality.
    /// </summary>
    private ADMQualityManager m_qualityManager;

    /// <summary>
    /// The camera for the visualization.
    /// </summary>
    private Camera m_camera;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods is called the
    /// first time.
    /// </summary>
    public void Start()
    {
        m_qualityManager = FindObjectOfType<ADMQualityManager>();
        m_camera = GetComponent<Camera>();
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        Vector2 min;
        Vector2 max;
        m_qualityManager.GetBoundingBox(out min, out max);

        Vector3 targetPosition;
        targetPosition.x = (min.x + max.x) / 2;
        targetPosition.y = transform.position.y;
        targetPosition.z = (min.y + max.y) / 2;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, MOVEMENT_SPEED * Time.deltaTime);

        float halfSizeX = (max.x - min.x) / 2;
        float halfSizeZ = (max.y - min.y) / 2;
        float targetSize = Mathf.Max(Mathf.Max(halfSizeX / m_camera.aspect, halfSizeZ), MIN_ZOOM);
        m_camera.orthographicSize = Mathf.MoveTowards(m_camera.orthographicSize, targetSize, ZOOM_SPEED * Time.deltaTime);
    }
}
