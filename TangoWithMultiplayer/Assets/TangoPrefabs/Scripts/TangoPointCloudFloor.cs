//-----------------------------------------------------------------------
// <copyright file="TangoPointCloudFloor.cs" company="Google">
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
using System.IO;
using Tango;
using UnityEngine;

/// <summary>
/// If this script is attached to a game object, its y position will be set to
/// the real world's floor height. The floor height is found by TangoPointCloud.
/// To use this script, TangoPointCloud must be in the scene and depth must be
/// enabled on the TangoApplication script/TangoManager prefab.
/// </summary>
public class TangoPointCloudFloor : MonoBehaviour 
{
    /// <summary>
    /// If <c>true</c>, turn off depth camera after the floor has been found.
    /// </summary>
    public bool m_turnOffDepthCamera = true;

    /// <summary>
    /// Is <c>true</c> if a floor has been found by TangoPointCloud. Matches the same condition in TangoPointCloud.
    /// </summary>
    [HideInInspector]
    public bool m_floorFound = false;

    /// <summary>
    /// The scene's Tango application.
    /// </summary>
    private TangoApplication m_tangoApplication;
    
    /// <summary>
    /// Reference to the TangoPointCloud object.
    /// </summary>
    private TangoPointCloud m_pointCloud;

    /// <summary>
    /// Check to prevent multiple unnecessary calls to TangoApplication to set the depth camera rate.
    /// </summary>
    private bool m_depthTriggered = false;

    /// @cond
    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        m_pointCloud = FindObjectOfType<TangoPointCloud>();
        m_tangoApplication = FindObjectOfType<TangoApplication>();

        // All child objects are disabled until the floor is found.
        foreach (Transform t in transform)
        {
            t.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        // If the point cloud has found the floor, adjust the position accordingly.
        if (m_pointCloud.m_floorFound)
        {
            m_floorFound = true;
            if (transform.position.y != m_pointCloud.m_floorPlaneY)
            {
                transform.position = new Vector3(0.0f, m_pointCloud.m_floorPlaneY, 0.0f);
                foreach (Transform t in transform)
                {
                    t.gameObject.SetActive(true);
                }
            }

            // Disable depth camera if requested and not already done.
            if (m_turnOffDepthCamera && !m_depthTriggered)
            {
                m_depthTriggered = true;
                m_tangoApplication.SetDepthCameraRate(TangoEnums.TangoDepthCameraRate.DISABLED);
            }
        }
        else
        {
            m_floorFound = false;
            m_depthTriggered = false;
        }
    }

    /// @endcond
}
