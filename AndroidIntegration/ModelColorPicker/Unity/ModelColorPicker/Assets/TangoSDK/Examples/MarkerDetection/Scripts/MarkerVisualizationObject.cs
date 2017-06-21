//-----------------------------------------------------------------------
// <copyright file="MarkerVisualizationObject.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// Unity object that represents a marker. 
/// A marker object renders its bounding box and three axes.
/// </summary>
public class MarkerVisualizationObject : MonoBehaviour 
{
    /// <summary>
    /// The bounding box LineRenderer object.
    /// </summary>
    public LineRenderer m_rect;

    /// <summary>
    /// Update the object with a new marker.
    /// </summary>
    /// <param name="marker">
    /// The input marker.
    /// </param>
    public void SetMarker(TangoSupport.Marker marker) 
    {
        m_rect.SetPosition(0, marker.m_corner3DP0);
        m_rect.SetPosition(1, marker.m_corner3DP1);
        m_rect.SetPosition(2, marker.m_corner3DP2);
        m_rect.SetPosition(3, marker.m_corner3DP3);
        m_rect.SetPosition(4, marker.m_corner3DP0);

        // Apply the pose of the marker to the object. 
        // This also applies implicitly to the axis object.
        transform.position = marker.m_translation;
        transform.rotation = marker.m_orientation;
    }
}
