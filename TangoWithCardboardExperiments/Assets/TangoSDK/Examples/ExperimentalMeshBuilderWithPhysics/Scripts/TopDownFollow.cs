//-----------------------------------------------------------------------
// <copyright file="TopDownFollow.cs" company="Google">
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
/// Follows that target from above, for top down view of the main camera.
/// </summary>
public class TopDownFollow : MonoBehaviour
{
    public GameObject followTarget;
    public bool followYaw = false;
    private Vector3 pos;

    private Vector3 rotation;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        transform.parent = null;
        pos = transform.position;
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        pos.x = followTarget.transform.position.x;
        pos.z = followTarget.transform.position.z;
        transform.position = pos;

        if (followYaw)
        {
            rotation = followTarget.transform.rotation.eulerAngles;
            rotation.x = 90;
            rotation.z = 0;
            transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
