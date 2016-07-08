//-----------------------------------------------------------------------
// <copyright file="MotionTrackingRotate.cs" company="Google">
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
using System.Collections;
using UnityEngine;

/// <summary>
/// Simple class for rotating an object.
/// </summary>
public class MotionTrackingRotate : MonoBehaviour
{
    /// <summary>
    /// Speed of rotation, in degrees per second.
    /// </summary>
    public float m_rotationSpeed = 15.0f;

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update()
    {
        // Rotate the object around the World's Y axis
        transform.Rotate(Vector3.up * (Time.deltaTime * m_rotationSpeed), Space.World);
    }
}
