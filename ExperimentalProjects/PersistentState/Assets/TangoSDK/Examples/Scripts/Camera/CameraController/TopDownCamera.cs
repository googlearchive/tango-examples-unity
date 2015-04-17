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
/// Used to show follow the target object from a
/// top-down perspective.
/// </summary>
public class TopDownCamera : IBaseCamera 
{
    private float m_counter = 0.0f;
    private Quaternion m_startRotation;
    private Quaternion m_endRotation;
    private float m_smoothTimeInverse;

    /// <summary>
    /// Set camera initial parameters.
    /// </summary>
    /// <param name="targetObject"> Reference to the target game object.</param>
    /// <param name="offset"> Position to maintain while following the
    /// target object.</param>
    public override void SetCamera(GameObject targetObject,
                                   Vector3 offset, float smoothTime = 0.05f)
    {
        m_targetObject = targetObject;
        m_offset = offset;
        m_smoothTime = smoothTime;
        m_smoothTimeInverse = 1 / m_smoothTime;
        m_startRotation = transform.rotation;
        m_endRotation = Quaternion.Euler(new Vector3(90.0f, 0, 0.0f));
        m_fieldOfViewSetting = 70;
    }

    /// <summary>
    /// Update the Top-Down Camera.
    /// </summary>
    public override void Update() 
    {
        Vector3 endPosition = m_targetObject.transform.position + m_offset;
        float newPositionY = Mathf.SmoothDamp(transform.position.y, 
                                              endPosition.y, 
                                              ref m_velocityY, 
                                              m_smoothTime);
        float newPositionX = Mathf.SmoothDamp(transform.position.x, 
                                              endPosition.x, 
                                              ref m_velocityX, 
                                              m_smoothTime);
        float newPositionZ = Mathf.SmoothDamp(transform.position.z, 
                                              endPosition.z, 
                                              ref m_velocityZ, 
                                              m_smoothTime);
        transform.position = new Vector3(newPositionX, 
                                                     newPositionY, 
                                                     newPositionZ);

        if (m_counter <= m_smoothTime)
        {
            m_counter += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(m_startRotation, 
                                                  m_endRotation, 
                                                  m_counter * m_smoothTimeInverse);
        }
    }

    /// <summary>
    /// On disable call back
    /// reset lerp time counter.
    /// </summary>
    private void OnDisable()
    {
        m_counter = 0.0f;
    }
}
