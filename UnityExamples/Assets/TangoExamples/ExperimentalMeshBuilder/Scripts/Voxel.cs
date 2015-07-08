/*
 * Copyright 2014 Google Inc. All Rights Reserved.
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

/**
 * Voxel class for storing voxel data and state
 */

public class Voxel
{
    /**
     * x position index within the hash volume
     */
    public int xID;

    /**
     * y position index within the hash volume
     */
    public int yID;

    /**
     * z position index within the hash volume
     */
    public int zID;

    /**
     * signed distance value of the voxel
     */
    public float value;

    /**
     * signed distance weight of the voxel
     */
    public float weight;

    /**
     * previous signed distance wieght of the voxel
     */
    public float lastMeshedValue;

    /**
     * 3D point position of the voxel
     */
    public Vector3 anchor;

    /**
     * size of the voxel
     */
    public float size;

    /**
     * estaimte normal of the voxel surface
     */
    public Vector3 normal;

    /**
     * Unity object that own this voxel
     */
    public Transform parent;

    /**
     * flags for preprocessing neighbors
     */
    public bool neighborsCreated;

    /**
     * indices of triangles this voxel participated in
     */
    public List<int> trianglesIndicies = new List<int>(); 

    /**
     * neighbor voxels for faster meshing
     */
    public Voxel v1;
    public Voxel v2;
    public Voxel v3;
    public Voxel v4;
    public Voxel v5;
    public Voxel v6;
    public Voxel v7;

}