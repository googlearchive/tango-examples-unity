//-----------------------------------------------------------------------
// <copyright file="MarkerDetectionUIController.cs" company="Google">
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
using System.Collections.Generic;
using Tango;
using UnityEngine;

/// <summary>
/// Detect a single AR Tag marker and place a virtual reference object on the
/// physical marker position.
/// </summary>
public class MarkerDetectionUIController : MonoBehaviour, ITangoVideoOverlay
{
    /// <summary>
    /// The prefabs of planets.
    /// </summary>
    public GameObject[] m_planetsPrefabs;
    
    /// <summary>
    /// Length of side of the physical AR Tag marker in meters.
    /// </summary>
    private const double MARKER_SIZE = 0.1397;

    /// <summary>
    /// The objects of all planets.
    /// </summary>
    private GameObject[] m_planets;

    /// <summary>
    /// The list of markers detected in each frame.
    /// </summary>
    private List<TangoSupport.Marker> m_markerList;

    /// <summary>
    /// A reference to TangoApplication in current scene.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Unity Start function.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }

        m_planets = new GameObject[m_planetsPrefabs.Length];
        m_markerList = new List<TangoSupport.Marker>();
    }

    /// <summary>
    /// Detect one or more markers in the input image.
    /// </summary>
    /// <param name="cameraId">
    /// Returned camera ID.
    /// </param>
    /// <param name="imageBuffer">
    /// Color camera image buffer.
    /// </param>
    public void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId,
        TangoUnityImageData imageBuffer)
    {
        TangoSupport.DetectMarkers(imageBuffer, cameraId,
            TangoSupport.MarkerType.ARTAG, MARKER_SIZE, m_markerList);

        for (int i = 0; i < m_markerList.Count; ++i) 
        {
            TangoSupport.Marker marker = m_markerList[i];
            int markerId = Convert.ToInt32(marker.m_content);
            int planetIndex = (markerId - 1) % m_planets.Length;
            
            if (m_planets[planetIndex] == null) 
            {
                m_planets[planetIndex] = Instantiate<GameObject>(m_planetsPrefabs[planetIndex]);
            }

            m_planets[planetIndex].transform.position = marker.m_translation;
            m_planets[planetIndex].transform.rotation = marker.m_orientation;
        }
    }
}