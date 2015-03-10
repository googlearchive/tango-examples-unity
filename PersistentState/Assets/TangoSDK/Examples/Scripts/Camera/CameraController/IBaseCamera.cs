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
/// Abstract base class that all camera behaviors should
/// derive from.
/// </summary>
[RequireComponent(typeof(Camera))]
public abstract class IBaseCamera : MonoBehaviour
{
    public float m_fieldOfViewSetting = 45;
    protected GameObject m_targetObject;
    protected Vector3 m_offset;
    protected Vector3 m_lookAtPosition;

    protected float m_smoothTime = 0.3f;
    protected float m_velocityX = 0.0f;
    protected float m_velocityY = 0.0f;
    protected float m_velocityZ = 0.0f;

    /// <summary>
    /// Property to get/set camera offset.
    /// </summary>
    /// <value> Vector3 - offset.</value>
    public Vector3 Offset
    {
        get
        {
            return m_offset;
        }

        set
        {
            m_offset = value;
        }
    }

    /// <summary>
    /// Property to get/set target object.
    /// </summary>
    /// <value> GameObject - target object.</value>
    public GameObject TargetObject
    {
        get
        {
            return m_targetObject;
        }

        set
        {
            m_targetObject = value;
        }
    }
    
    /// <summary>
    /// All derived classes must provide their
    /// own update functionality.
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// Set up a camera's parameters.
    /// </summary>
    /// <param name="targetObject"> Target object of the camera.</param>
    /// <param name="offset"> Offset from the Target object.</param>
    public abstract void SetCamera(GameObject targetObject, Vector3 offset, float smoothTime);
}
