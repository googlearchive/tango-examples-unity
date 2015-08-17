//-----------------------------------------------------------------------
// <copyright file="BuildingManager.cs" company="Google">
//
// Copyright 2015 Google Inc. All Rights Reserved.
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
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton building manager.
/// </summary>
public class BuildingManager : MonoBehaviour
{
    public GameObject[] buildingPrototypes;
    public List<Building> buildingList = new List<Building>();
    
    public GameObject goundObject;
    public Camera mainCamera;
    public Camera uiCamera;
    public Building curBuldingObject;
    
    public Color buildingErrorColor;
    public Color buildingCorrectColor;
    
    public GameObject placeBuildingButton;
    public GameObject cancelBuildingButton;
    
    private static BuildingManager m_instance;

    private bool[] occupancyIndex = new bool[400 * 400];

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    /// <value>The instance.</value>
    public static BuildingManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType<BuildingManager>();
                DontDestroyOnLoad(m_instance.gameObject);
            }
            
            return m_instance;
        }
    }

    /// <summary>
    /// Called before Start().
    /// </summary>
    public void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != m_instance)
            {
                Destroy(this.gameObject);
            }
        }
    }

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        EventManager.TangoPoseStateChanged += TangoStateChanged;
    
        // Null terminator cause problem in the file system.
        Statics.curADFId = Statics.curADFId.Replace("\0", string.Empty);
        string path = Application.persistentDataPath + "/" + Statics.curADFId;
        FileParser.GetVectorListFromPath(out buildingList, path);
        SetAllBuildingActive(false);
    }

    private Vector2 hitPoint = new Vector2();
    private int index = 0;

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        if (Statics.isPlacingObject)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (RayCastGroud(out hitPoint))
                {
                    hitPoint = FindContainningGrid(out index, hitPoint.x, hitPoint.y);
                    curBuldingObject.buildingObject.transform.position = new Vector3(hitPoint.x, 0.0f, hitPoint.y);
                }
            }
            if (!occupancyIndex[index])
            {
                // make the building red.
                curBuldingObject.buildingObject.GetComponent<BuildingController>().SetBuildingOutfitColor(buildingCorrectColor);
            }
            else
            {
                // make building green.
                curBuldingObject.buildingObject.GetComponent<BuildingController>().SetBuildingOutfitColor(buildingErrorColor);
            }
        }
    }

    /// <summary>
    /// Sets all buildings' active state.
    /// </summary>
    /// <param name="isActive">If set to <c>true</c> is active.</param>
    public void SetAllBuildingActive(bool isActive)
    {
        foreach (Building building in buildingList)
        {
            building.SetBuildingActive(isActive);
        }
    }

    /// <summary>
    /// Place a new building.
    /// </summary>
    public void PlaceBuilding()
    {
        if (occupancyIndex[index])
        {
            // not placeable.
            return;
        }
        occupancyIndex[index] = true;
        curBuldingObject.buildingObject.GetComponent<BuildingController>().buildingOutfit.SetActive(false);
        Statics.isPlacingObject = false;
        placeBuildingButton.SetActive(false);
        cancelBuildingButton.SetActive(false);
        buildingList.Add(curBuldingObject);
    }

    /// <summary>
    /// Cancel the placement of a building.
    /// </summary>
    public void CancelBuildingPlacement()
    {
        Statics.isPlacingObject = false;
        placeBuildingButton.SetActive(false);
        cancelBuildingButton.SetActive(false);
        DestroyImmediate(curBuldingObject.buildingObject);
    }

    /// <summary>
    /// Create a building in one place.
    /// </summary>
    /// <param name="index">Index.</param>
    public void CreateBulding(int index)
    {
        if (Statics.isPlacingObject)
        {
            // place on bulding at a time.
            return;
        }
        curBuldingObject = InstantiateBuilding(index, 0.0f, 0.0f);
        Statics.isPlacingObject = true;
        placeBuildingButton.SetActive(true);
        cancelBuildingButton.SetActive(true);
    }

    /// <summary>
    /// Get a new building object at a specific index / coordinate.
    /// </summary>
    /// <returns>The building.</returns>
    /// <param name="index">Index.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    public Building InstantiateBuilding(int index, float x, float z)
    {
        GameObject buildingObj = (GameObject)Instantiate(buildingPrototypes[index]);
        Building building = new Building();
        building.buildingObject = buildingObj;
        building.buildingObject.transform.position = new Vector3(x, 0.0f, z);
        building.buildingId = index;
        return building;
    }

    /// <summary>
    /// Clear all buildings.
    /// </summary>
    public void ResetBuildingManager()
    {
        buildingList = new List<Building>();
    }
    
    /// <summary>
    /// Finds the correct grid cell for this position.
    /// </summary>
    /// <returns>The containning grid cell.</returns>
    /// <param name="index">Index of the grid cell.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    private Vector2 FindContainningGrid(out int index, float x, float z)
    {
        float iterX = -50.0f;
        float iterY = -50.0f;
        int indexX = 0;
        int indexY = 0;
        while (iterX < x)
        {
            iterX += 0.25f;
            indexX++;
        }
        iterX = iterX - 0.125f;
        while (iterY < z)
        {
            iterY += 0.25f;
            indexY++;
        }
        iterY = iterY - 0.125f;
        
        index = (indexY * 400) + indexX;
        return new Vector2(iterX, iterY);
    }
    
    /// <summary>
    /// Called up update the Tango state.
    /// </summary>
    /// <param name="curState">Current state.</param>
    private void TangoStateChanged(TangoPoseStates curState)
    {
        if (curState == TangoPoseStates.Running)
        {
            SetAllBuildingActive(true);
        }
        else
        {
            SetAllBuildingActive(false);
        }
    }

    /// <summary>
    /// Raycast from the "mouse" to the ground.
    /// </summary>
    /// <returns><c>true</c>, if cast groud was rayed, <c>false</c> otherwise.</returns>
    /// <param name="outHitPoint">Out hit point.</param>
    private bool RayCastGroud(out Vector2 outHitPoint)
    {
        RaycastHit hit = new RaycastHit();
        Vector3 screenPosition = Input.mousePosition;
        if (Physics.Raycast(uiCamera.ScreenPointToRay(screenPosition), out hit, Mathf.Infinity))
        {
            outHitPoint = new Vector2();
            return false;
        }
        if (Physics.Raycast(mainCamera.ScreenPointToRay(screenPosition), out hit, Mathf.Infinity))
        {
            Debug.DrawRay(mainCamera.transform.position, hit.point - mainCamera.transform.position, Color.red);
            if (hit.transform.gameObject == goundObject)
            {
                outHitPoint = new Vector2(hit.point.x, hit.point.z);
                return true;
            }
        }
        outHitPoint = new Vector2();
        return false;
    }
}

/// <summary>
/// A single building.
/// </summary>
public class Building : MonoBehaviour
{
    public GameObject buildingObject;
    public int buildingId;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Building"/> class.
    /// </summary>
    public Building()
    {
    }
    
    /// <summary>
    /// Sets the building active.
    /// </summary>
    /// <param name="isActive">If set to <c>true</c> is active.</param>
    public void SetBuildingActive(bool isActive)
    {
        buildingObject.SetActive(isActive);
    }
    
    /// <summary>
    /// Releases unmanaged resources and performs other cleanup operations before the <see cref="Building"/> is
    /// reclaimed by garbage collection.
    /// </summary>
    ~Building()
    {
        DestroyObject(buildingObject);
    }
}
