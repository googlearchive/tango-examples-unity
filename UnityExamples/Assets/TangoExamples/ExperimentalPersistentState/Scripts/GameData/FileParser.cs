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
using System.IO;
using System.Text;

public class FileParser : MonoBehaviour {
    public static void SaveBuildingDataToPath(List<Building> buildingList, string path) {
        StringBuilder strBuilder = new StringBuilder();
        foreach(Building iter in buildingList) {
            strBuilder.Append(iter.buildingId.ToString() + ",");
            strBuilder.Append(iter.buildingObject.transform.position.x.ToString() + ",");
            strBuilder.Append(iter.buildingObject.transform.position.z.ToString() + "\n");
        }
        System.IO.File.WriteAllText(path, strBuilder.ToString());
    }

    public static void GetVectorListFromPath(out List<Building> buildList, string path) {
        string line = "";
        buildList = new List<Building>();
        // Null terminator causes problem in the file syste.
        StreamReader file = new StreamReader(path);
        while ((line = file.ReadLine()) != null) {
            Building b = new Building();
            string[] ints = line.Split(',');
            b = BuildingManager.instance.InstantiateBuilding(int.Parse(ints[0]), float.Parse(ints[1]), float.Parse(ints[2]));
            b.buildingObject.GetComponent<BuildingController> ().buildingOutfit.SetActive (false);
            buildList.Add(b);
        }
    }
}
