// <copyright file="CubeGenerator.cs" company="Google">
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
using UnityEngine;

/// <summary>
/// Generate the cube world.
/// </summary>
public class CubeGenerator : MonoBehaviour
{
    public GameObject prefab;

    public float size = 1;
    public int gridSize = 4;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        float gridStep = 5 * size;
        for (int x = -gridSize; x <= gridSize; x++)
        {
            for (int y = -gridSize; y <= gridSize; y++)
            {
                for (int z = -gridSize; z <= gridSize; z++)
                {
                    GameObject obj = (GameObject)GameObject.Instantiate(prefab);
                    obj.transform.parent = transform;
                    obj.transform.localScale = new Vector3(size, size, size);
                    obj.transform.position = new Vector3(x * gridStep, y * gridStep, z * gridStep);
                }
            }
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
    }
}
