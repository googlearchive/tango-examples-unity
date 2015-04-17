/*
 * Copyright 2015 Google Inc. All Rights Reserved.
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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Building : MonoBehaviour {
    public GameObject buildingObject;
    public int buildingId;

    public Building() {
    }

    public void SetBuildingActive (bool isActive) {
        buildingObject.SetActive(isActive);
    }

    ~Building() {
        DestroyObject(buildingObject);
    }
}

public class BuildingManager : MonoBehaviour {
    private static BuildingManager _instance;
    
    public static BuildingManager instance {
        get {
            if(_instance == null) {
                _instance = GameObject.FindObjectOfType<BuildingManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            
            return _instance;
        }
    }

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

    private bool[] occupancyIndex = new bool[400 * 400];

    void Awake () {
        if(_instance == null) {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            if(this != _instance) {
                Destroy(this.gameObject);
            }
        }
    }

    // Use this for initialization
    void Start () {
        EventManager.tangoPoseStatedChanged += TangoStateChanged;
        // Null terminator cause problem in the file system.
        Statics.curADFId = Statics.curADFId.Replace ("\0", "");
        string path = Application.persistentDataPath + "/" + Statics.curADFId;
        FileParser.GetVectorListFromPath (out buildingList, path);
        SetAllBuildingActive(false);
    }

    Vector2 hitPoint = new Vector2();
    int index = 0;
    // Update is called once per frame
    void Update () {
        if (Statics.isPlacingObject) {
            if (Input.GetKey(KeyCode.Mouse0)) {
                if (RayCastGroud(out hitPoint))
                {
                    hitPoint = FindContainningGrid(out index, hitPoint.x, hitPoint.y);
                    curBuldingObject.buildingObject.transform.position = new Vector3(hitPoint.x, 0.0f, hitPoint.y);
                }
            }
            if (!occupancyIndex[index]) {
                // make the building red.
                curBuldingObject.buildingObject.GetComponent<BuildingController>().SetBuildingOutfitColor(buildingCorrectColor);
            }
            else {
                // make building green.
                curBuldingObject.buildingObject.GetComponent<BuildingController>().SetBuildingOutfitColor(buildingErrorColor);
            }
        }
    }

    Vector2 FindContainningGrid (out int index, float x, float z) {
        float iterX = -50.0f;
        float iterY = -50.0f;
        int indexX = 0;
        int indexY = 0;
        while (iterX < x) {
            iterX += 0.25f;
            indexX++;
        }
        iterX = iterX - 0.125f;
        while (iterY < z) {
            iterY += 0.25f;
            indexY++;
        }
        iterY = iterY - 0.125f;

        index = indexY * 400 + indexX;
        return new Vector2(iterX, iterY);
    }

    void TangoStateChanged(TangoPoseStates curState) {
        if (curState == TangoPoseStates.Running) {
            SetAllBuildingActive(true);
        } else {
            SetAllBuildingActive(false);
        }
    }

    public void SetAllBuildingActive(bool isActive) {
        foreach(Building building in buildingList) {
            building.SetBuildingActive(isActive);
        }
    }

    public void PlaceBuilding() {
        if (occupancyIndex [index]) {
            // not placeable.
            return;
        }
        occupancyIndex [index] = true;
        curBuldingObject.buildingObject.GetComponent<BuildingController> ().buildingOutfit.SetActive (false);
        Statics.isPlacingObject = false;
        placeBuildingButton.SetActive (false);
        cancelBuildingButton.SetActive (false);
        buildingList.Add(curBuldingObject);
    }

    public void CancelBuildingPlacement() {
        Statics.isPlacingObject = false;
        placeBuildingButton.SetActive (false);
        cancelBuildingButton.SetActive (false);
        DestroyImmediate(curBuldingObject.buildingObject);
    }

    public void CreateBulding(int index) {
        if (Statics.isPlacingObject) {
            // place on bulding at a time.
            return;
        }
        curBuldingObject = InstantiateBuilding (index, 0.0f, 0.0f);
        Statics.isPlacingObject = true;
        placeBuildingButton.SetActive (true);
        cancelBuildingButton.SetActive (true);
    }

    public Building InstantiateBuilding(int index, float x, float z) {
        GameObject buildingObj = (GameObject)Instantiate(buildingPrototypes[index]);
        Building building = new Building();
        building.buildingObject = buildingObj;
        building.buildingObject.transform.position = new Vector3(x, 0.0f, z);
        building.buildingId = index;
        return building;
    }

    public void ResetBuildingManager() {
        buildingList = new List<Building>();
    }

    private bool RayCastGroud(out Vector2 outHitPoint) {
        RaycastHit hit = new RaycastHit();
        Vector3 screenPosition = Input.mousePosition;
        if (Physics.Raycast(uiCamera.ScreenPointToRay(screenPosition), out hit, Mathf.Infinity)) {
            outHitPoint = new Vector2();
            return false;
        }
        if (Physics.Raycast(mainCamera.ScreenPointToRay(screenPosition), out hit, Mathf.Infinity))  {
            Debug.DrawRay(mainCamera.transform.position, hit.point - mainCamera.transform.position, Color.red);
            if (hit.transform.gameObject == goundObject)  {
                outHitPoint = new Vector2(hit.point.x, hit.point.z);
                return true;
            }
        }
        outHitPoint = new Vector2();
        return false;
    }
}
