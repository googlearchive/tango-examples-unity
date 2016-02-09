//-----------------------------------------------------------------------
// <copyright file="ADMQualityManager.cs" company="Google">
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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manage the visuals for an Area Description recording's quality.
/// 
/// The quality is done by keeping track of all the locations the Project Tango device has been in and remembering
/// which orientations it has faced.  A high quality recording is where for every location the device has been in, 
/// there has been a 360 degree coverage in locations.
/// 
/// To keep things simple, the recording quality assumes a relative flat world.
/// </summary>
public class ADMQualityManager : MonoBehaviour
{
    /// <summary>
    /// The PoseController whose position corresponds to the Project Tango device's position.
    /// </summary>
    public Transform m_poseController;

    /// <summary>
    /// Visualization prefab.  This will get instantiated for each instance of QualityCell.
    /// </summary>
    public GameObject m_cellVisualsPrefab;

    /// <summary>
    /// Parent of UI that should be shown when the quality of transforms coming in is bad.
    /// </summary>
    public RectTransform m_badQualityTransformParent;

    /// <summary>
    /// Textual description of quality.
    /// </summary>
    public Text m_qualityText;

    /// <summary>
    /// Size of each cell measuring quality, in meters.
    /// </summary>
    private const int CELL_SIZE = 2;

    /// <summary>
    /// Number of angles displayed for each cell.
    /// </summary>
    private const int ANGLE_RESOLUTION = 8;

    /// <summary>
    /// The maximum time a bad quality transform is allowed before showing the UI.
    /// </summary>
    private const float MAX_ALLOWED_BAD_QUALITY_TRANSFORM_TIME = 0.5f;

    /// <summary>
    /// The maximum pitch of the device allowed in a good quality transform.
    /// </summary>
    private const float MAX_ALLOWED_PITCH = 45;

    /// <summary>
    /// A regular grid of quality cells indexed in order X, Z.
    /// 
    /// This will get grown as necessary.
    /// </summary>
    private List<List<QualityCell>> m_cellQualities;

    /// <summary>
    /// The origin of the m_cellQualities grid, in (X, Z) coordinates.
    /// </summary>
    private Vector2 m_cellsOrigin;

    /// <summary>
    /// The time in seconds of having a bad quality transform.
    /// </summary>
    private float m_badQualityTransformTime;

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    public void OnEnable()
    {
        m_cellQualities = new List<List<QualityCell>>();
        m_cellsOrigin = Vector2.zero;
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called.
    /// </summary>
    public void LateUpdate()
    {
        // This logic all needs to run after the pose controller has moved in its update, hence LateUpdate.
        int cellX;
        int cellZ;
        int angleIndex;

        if (_DiscretizeTransform(m_poseController, out cellX, out cellZ, out angleIndex))
        {
            m_badQualityTransformTime = 0;

            QualityCell cell = _InternCell(cellX, cellZ);
            cell.m_angleVisited[angleIndex] = true;
            _UpdateVisuals(cell);
        }
        else
        {
            m_badQualityTransformTime += Time.deltaTime;
        }

        // Hide / show the bad quality UI as needed.
        if (m_badQualityTransformTime > MAX_ALLOWED_BAD_QUALITY_TRANSFORM_TIME)
        {
            m_badQualityTransformParent.gameObject.SetActive(true);
        }
        else
        {
            m_badQualityTransformParent.gameObject.SetActive(false);
        }

        // Update the quality text
        m_qualityText.text = string.Format("{0}% coverage", Mathf.FloorToInt(_CalculateQuality() * 100));
    }

    /// <summary>
    /// Get the bounding box for the quality visualization in (X, Z) coordinates.
    /// </summary>
    /// <param name="minXZ">Returns the minimum (X, Z) coordinates of the visualization.</param>
    /// <param name="maxXZ">Returns the maximum (X, Z) coordinates of the visualization.</param>
    public void GetBoundingBox(out Vector2 minXZ, out Vector2 maxXZ)
    {
        int sizeX = m_cellQualities.Count;
        int sizeY = 0;
        if (sizeX > 0)
        {
            sizeY = m_cellQualities[0].Count;
        }

        minXZ = m_cellsOrigin - (new Vector2(CELL_SIZE, CELL_SIZE) / 2);
        maxXZ = minXZ + new Vector2(sizeX * CELL_SIZE, sizeY * CELL_SIZE);
    }

    /// <summary>
    /// Update the visuals for a cell.
    /// </summary>
    /// <param name="cell">Cell to update.</param>
    private static void _UpdateVisuals(QualityCell cell)
    {
        bool anyAreTrue = false;
        bool anyAreFalse = false;

        for (int it = 0; it < cell.m_angleVisited.Length; ++it)
        {
            // The first object is the "fully filled" object.  Children are after that.
            GameObject childObject = cell.m_visuals.transform.GetChild(it + 1).gameObject;
            bool value = cell.m_angleVisited[it];
            
            childObject.SetActive(value);
            if (value)
            {
                anyAreTrue = true;
            }
            else
            {
                anyAreFalse = true;
            }
        }

        cell.m_visuals.SetActive(anyAreTrue);
        cell.m_visuals.transform.GetChild(0).gameObject.SetActive(anyAreTrue && !anyAreFalse);
    }

    /// <summary>
    /// Discretize the transform into integral values.
    /// 
    /// This returns true only if the transform is of good quality.  A good quality transform is facing roughly 
    /// forward, not looking too far down or too far up.
    /// </summary>
    /// <returns><c>true</c>, if the transform is of good quality, <c>false</c> otherwise.</returns>
    /// <param name="t">Transform to discretize.</param>
    /// <param name="cellX">Discrete X value, as a cell index.</param>
    /// <param name="cellZ">Discrete Z value, as a cell index.</param>
    /// <param name="angleIndex">Discrete angle value, as an angle index.</param>
    private bool _DiscretizeTransform(Transform t, out int cellX, out int cellZ, out int angleIndex)
    {
        Vector3 eulerAngles = t.eulerAngles;

        cellX = Mathf.RoundToInt((t.position.x - m_cellsOrigin.x) / CELL_SIZE);
        cellZ = Mathf.RoundToInt((t.position.z - m_cellsOrigin.y) / CELL_SIZE);
        angleIndex = Mathf.RoundToInt(eulerAngles.y / 360 * ANGLE_RESOLUTION) % ANGLE_RESOLUTION;

        // If you are looking too far up or down, there won't be a good view for localization.
        return eulerAngles.x < MAX_ALLOWED_PITCH || eulerAngles.x > 360 - MAX_ALLOWED_PITCH;
    }

    /// <summary>
    /// Get the cell for a specific coordinate, allocating new cells if necessary.
    /// 
    /// Think string.Intern.
    /// </summary>
    /// <returns>The cell for that coordinate.</returns>
    /// <param name="x">X index for the cell.</param>
    /// <param name="z">Z index for the cell.</param>
    private QualityCell _InternCell(int x, int z)
    {
        int curSizeX = m_cellQualities.Count;
        int curSizeY = 0;
        if (curSizeX > 0)
        {
            curSizeY = m_cellQualities[0].Count;
        }

        // Ensure that x, y can be used to index into m_cellQualities, growing the array if needed.
        while (x < 0)
        {
            List<QualityCell> toInsert = new List<QualityCell>();
            for (int it = 0; it < curSizeY; ++it)
            {
                QualityCell cell = new QualityCell();
                toInsert.Add(cell);

                cell.m_visuals = Instantiate(m_cellVisualsPrefab) as GameObject;
                cell.m_visuals.transform.position = new Vector3(
                    m_cellsOrigin.x - CELL_SIZE, 0, m_cellsOrigin.y + (it * CELL_SIZE));
                _UpdateVisuals(cell);
            }

            m_cellQualities.Insert(0, toInsert);

            ++curSizeX;
            ++x;
            m_cellsOrigin.x -= CELL_SIZE;
        }

        while (x >= curSizeX)
        {
            List<QualityCell> toInsert = new List<QualityCell>();
            for (int it = 0; it < curSizeY; ++it)
            {
                QualityCell cell = new QualityCell();
                toInsert.Add(cell);

                cell.m_visuals = Instantiate(m_cellVisualsPrefab) as GameObject;
                cell.m_visuals.transform.position = new Vector3(
                    m_cellsOrigin.x + (curSizeX * CELL_SIZE), 0, m_cellsOrigin.y + (it * CELL_SIZE));
                _UpdateVisuals(cell);
            }

            m_cellQualities.Add(toInsert);
            
            ++curSizeX;
        }

        while (z < 0)
        {
            for (int it = 0; it < curSizeX; ++it)
            {
                List<QualityCell> cellList = m_cellQualities[it];
                QualityCell cell = new QualityCell();
                cellList.Insert(0, cell);

                cell.m_visuals = Instantiate(m_cellVisualsPrefab) as GameObject;
                cell.m_visuals.transform.position = new Vector3(
                    m_cellsOrigin.x + (it * CELL_SIZE), 0, m_cellsOrigin.y - CELL_SIZE);
                _UpdateVisuals(cell);
            }

            ++curSizeY;
            ++z;
            m_cellsOrigin.y -= CELL_SIZE;
        }

        while (z >= curSizeY)
        {
            for (int it = 0; it < curSizeX; ++it)
            {
                List<QualityCell> cellList = m_cellQualities[it];
                QualityCell cell = new QualityCell();
                cellList.Add(cell);

                cell.m_visuals = Instantiate(m_cellVisualsPrefab) as GameObject;
                cell.m_visuals.transform.position = new Vector3(
                    m_cellsOrigin.x + (it * CELL_SIZE), 0, m_cellsOrigin.y + (curSizeY * CELL_SIZE));
                _UpdateVisuals(cell);
            }

            ++curSizeY;
        }

        // Now the indexes are both in-bounds of the cell grid.
        return m_cellQualities[x][z];
    }

    /// <summary>
    /// Calculate the quality of the current Area Description, as a percentage.
    /// </summary>
    /// <returns>The calculated quality on a scale from 0 to 1, where 1 is the best.</returns>
    private float _CalculateQuality()
    {
        int angleCount = 0;
        int maxAngleCount = 0;

        foreach (List<QualityCell> cellList in m_cellQualities)
        {
            foreach (QualityCell cell in cellList)
            {
                if (cell.m_visuals.activeInHierarchy)
                {
                    foreach (bool angle in cell.m_angleVisited)
                    {
                        if (angle)
                        {
                            ++angleCount;
                        }
                    }

                    maxAngleCount += ANGLE_RESOLUTION;
                }
            }
        }

        if (maxAngleCount == 0)
        {
            return 0;
        }
        else
        {
            return (float)angleCount / maxAngleCount;
        }
    }

    /// <summary>
    /// Positions the Project Tango device has visited, discretized.
    /// </summary>
    private class QualityCell
    {
        /// <summary>
        /// Angles for this position, discretized.
        /// </summary>
        public bool[] m_angleVisited = new bool[ANGLE_RESOLUTION];

        /// <summary>
        /// The associated GameObject visualizing this cell.
        /// </summary>
        public GameObject m_visuals;
    }
}
