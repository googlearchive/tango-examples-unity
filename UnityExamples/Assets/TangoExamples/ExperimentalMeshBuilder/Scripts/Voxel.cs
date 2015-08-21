//-----------------------------------------------------------------------
// <copyright file="Voxel.cs" company="Google">
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
/// Voxel class for storing voxel data and state.
/// </summary>
public class Voxel
{
    /// <summary>
    /// X position index within the hash volume.
    /// </summary>
    public int xID;

    /// <summary>
    /// Y position index within the hash volume.
    /// </summary>
    public int yID;

    /// <summary>
    /// Z position index within the hash volume.
    /// </summary>
    public int zID;

    /// <summary>
    /// Signed distance value of the voxel.
    /// </summary>
    public float value;

    /// <summary>
    /// Signed distance weight of the voxel.
    /// </summary>
    public float weight;

    /// <summary>
    /// Previous signed distance wieght of the voxel.
    /// </summary>
    public float lastMeshedValue;

    /// <summary>
    /// 3D point position of the voxel.
    /// </summary>
    public Vector3 anchor;

    /// <summary>
    /// Size of the voxel.
    /// </summary>
    public float size;

    /// <summary>
    /// Estaimte normal of the voxel surface.
    /// </summary>
    public Vector3 normal;

    /// <summary>
    /// Unity object that own this voxel.
    /// </summary>
    public Transform parent;

    /// <summary>
    /// Flags for preprocessing neighbors.
    /// </summary>
    public bool neighborsCreated;

    /// <summary>
    /// Indices of triangles this voxel participated in.
    /// </summary>
    public List<int> trianglesIndicies = new List<int>(); 

    // neighbor voxels for faster meshing
    public Voxel v1;
    public Voxel v2;
    public Voxel v3;
    public Voxel v4;
    public Voxel v5;
    public Voxel v6;
    public Voxel v7;
}