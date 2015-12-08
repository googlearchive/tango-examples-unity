//-----------------------------------------------------------------------
// <copyright file="VolumetricHashTree.cs" company="Google">
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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A binary tree datastructure that uses a hashkey based on the 3D coordinates.
/// Space surrounding the origin is divided into 1 meter cubes.  The hashkey is a 
/// reversible index into that volume.  allows quick indexing, and ray marching 
/// through space. Supports a 1000m x 1000m x 1000m volume centered at the origin.
/// </summary>
public class VolumetricHashTree
{
    /// <summary>
    /// Left subtree.
    /// </summary>
    private VolumetricHashTree m_leftHashTree = null;

    /// <summary>
    /// Right subtree.
    /// </summary>
    private VolumetricHashTree m_rightHashTree = null;

    /// <summary>
    /// Pointer to the root node of the tree.
    /// </summary>
    private VolumetricHashTree m_rootHashTree = null;
    
    /// <summary>
    /// Prefab that will be instantiated for each tree node.
    /// </summary>
    private GameObject m_meshPrefab = null;

    /// <summary>
    /// Dynamic Meshing class that will handle the data processing.
    /// </summary>
    private DynamicMeshCube m_dynamicMeshCube = null;

    /// <summary>
    /// Unpacked spatial volume index of the position of this tree node.
    /// </summary>
    private int[] m_volumeIndex = new int[3];
    
    /// <summary>
    /// HashKey of the position of this tree node.
    /// </summary>
    private int m_hashKey = 0;

    /// <summary>
    /// Maximum dimension of 3D the hashkey space.
    /// </summary>
    private int m_maximumVolumeIndexDimension = 1000; // supports +/- 500m from origin along each axis

    /// <summary>
    /// Epsilon tolerance for boundary insertions.
    /// </summary>
    private float m_epsilon = 0.01f;
    
    /// <summary>
    /// Create a new tree node with a specified parent and hashkey.
    /// </summary>
    /// <param name="parent">The parent tree node.</param>
    /// <param name="hashkey">The spatial hashkey for this tree node.</param>
    public VolumetricHashTree(VolumetricHashTree parent, int hashkey)
    {
        if (parent == null)
        {
            m_rootHashTree = this;
        }
        else
        {
            m_rootHashTree = parent;
        }
        
        m_hashKey = hashkey;
    }
    
    /// <summary>
    /// Get the DynamicMeshCube that contains the mesh data.
    /// </summary>
    /// <value>The dynamic mesh cube.</value>
    public DynamicMeshCube DynamicMeshCube
    {
        get { return m_dynamicMeshCube; }
    }

    /// <summary>
    /// Enumeration function for iterating through the entire tree structure.
    /// </summary>
    /// <returns>The enumerable.</returns>
    public IEnumerable<VolumetricHashTree> GetEnumerable()
    {
        if (m_leftHashTree != null)
        {
            foreach (VolumetricHashTree n in m_leftHashTree.GetEnumerable())
            {
                yield return n;
            }
        }
        yield return this;
        if (m_rightHashTree != null)
        {
            foreach (VolumetricHashTree n in m_rightHashTree.GetEnumerable())
            {
                yield return n;
            }
        }
    }

    /// <summary>
    /// Computes the spatial hashkey given a 3D point.
    /// </summary>
    /// <returns>The hashkey value for the point.</returns>
    /// <param name="p">Point for hashing into the key space.</param>
    public int GetHashKey(Vector3 p)
    {
        return Mathf.FloorToInt(p.x) + (int)(m_maximumVolumeIndexDimension * Mathf.Floor(p.y)) + (int)(m_maximumVolumeIndexDimension * m_maximumVolumeIndexDimension * Mathf.Floor(p.z));
    }

    /// <summary>
    /// Computes the new spatial hashkey given an integer offset in the 3D volume.
    /// </summary>
    /// <returns>The hashkey value for the new position.</returns>
    /// <param name="startKey">Start key.</param>
    /// <param name="xOffset">X offset.</param>
    /// <param name="yOffset">Y offset.</param>
    /// <param name="zOffset">Z offset.</param>
    public int OffsetHashKey(int startKey, int xOffset, int yOffset, int zOffset) 
    {
        return startKey + xOffset + (m_maximumVolumeIndexDimension * yOffset) + (m_maximumVolumeIndexDimension * m_maximumVolumeIndexDimension * zOffset);
    }

    /// <summary>
    /// Insert 3D point into the hash storage.  Creates a new meshing cube at the location if needed.
    /// </summary>
    /// <returns>The value of the voxel that received the insertion.</returns>
    /// <param name="p">The 3D point.</param>
    /// <param name="obs">Observation vector from the camera to the point.</param>
    /// <param name="weight">Weight of the observation.</param>
    /// <param name="prefab">Unity Prefab to be instantiated at this node location if needed.</param>
    /// <param name="parent">Unity transform of the parent object for the prefab created at this node location.</param>
    /// <param name="voxelResolution">Resolution of the voxels for this meshing cube.</param>
    public float InsertPoint(Vector3 p, Vector3 obs, float weight, GameObject prefab, Transform parent, int voxelResolution)
    {
        int hashKey = GetHashKey(p);
        return InsertPoint(hashKey, p, obs, weight, prefab, parent, voxelResolution);
    }
    
    /// <summary>
    /// Raycast through the volume to quickly determine the list of voxels intersected.
    /// </summary>
    /// <returns>List of populated voxels the ray intersects.</returns>
    /// <param name="start">Start point of the ray.</param>
    /// <param name="stop">Stop point of the ray.</param>
    public List<Voxel> RaycastVoxelHitlist(Vector3 start, Vector3 stop)
    {
        Vector3 dir = (stop - start).normalized;
        List<int> volumeHitKeys = new List<int>();
        volumeHitKeys.Add(GetHashKey(start));
        
        // x crosses
        if (dir.x > 0) 
        {
            for (float x = Mathf.Ceil(start.x) + m_epsilon; x < stop.x; x += 1)
            {
                float scale = (x - start.x) / dir.x;
                volumeHitKeys.Add(GetHashKey(start + (scale * dir)));
            }
        } 
        else 
        {
            for (float x = Mathf.Floor(start.x) - m_epsilon; x > stop.x; x -= 1)
            {
                float scale = (x - start.x) / dir.x;
                volumeHitKeys.Add(GetHashKey(start + (scale * dir)));
            }
        }
        
        // y crosses
        if (dir.y > 0) 
        {
            for (float y = Mathf.Ceil(start.y) + m_epsilon; y < stop.y; y += 1)
            {
                float scale = (y - start.y) / dir.y;
                volumeHitKeys.Add(GetHashKey(start + (scale * dir)));
            }
        } 
        else 
        {
            for (float y = Mathf.Floor(start.y) - m_epsilon; y > stop.y; y -= 1)
            {
                float scale = (y - start.y) / dir.y;
                volumeHitKeys.Add(GetHashKey(start + (scale * dir)));
            }
        }
        
        // z crosses
        if (dir.z > 0) 
        {
            for (float z = Mathf.Ceil(start.z) + m_epsilon; z < stop.z; z += 1)
            {
                float scale = (z - start.z) / dir.z;
                volumeHitKeys.Add(GetHashKey(start + (scale * dir)));
            }
        } 
        else 
        {
            for (float z = Mathf.Floor(start.z) - m_epsilon; z > stop.z; z -= 1)
            {
                float scale = (z - start.z) / dir.z;
                volumeHitKeys.Add(GetHashKey(start + (scale * dir)));
            }
        }
        
        List<Voxel> voxelHits = new List<Voxel>();
        
        foreach (int volumeKey in volumeHitKeys)
        {
            VolumetricHashTree result = Query(volumeKey);
            if (result == null)
            {
                continue;
            }
            if (result.m_meshPrefab == null)
            {
                continue;
            }
            List<Voxel> voxels = result.m_dynamicMeshCube.RayCastVoxelHitlist(start, stop, dir);
            foreach (Voxel v in voxels)
            {
                voxelHits.Add(v);
            }
        }
        
        return voxelHits;
    }
    
    /// <summary>
    /// Query for the existence of a node at a hashKey location.
    /// </summary>
    /// <param name="hashKey">Hash key.</param>
    /// <returns>The node if it exists, null if it does not.</returns>
    public VolumetricHashTree Query(int hashKey)
    {
        if (hashKey == m_hashKey)
        {
            return this;
        }
        if (hashKey < m_hashKey)
        {
            if (m_leftHashTree != null)
            {
                return m_leftHashTree.Query(hashKey);
            }
        }
        else
        {
            if (m_rightHashTree != null)
            {
                return m_rightHashTree.Query(hashKey);
            }
        }
        return null;
    }
    
    /// <summary>
    /// Clears the contents of this tree node and its subtrees.
    /// </summary>
    public void Clear()
    {
        if (m_leftHashTree != null)
        {
            m_leftHashTree.Clear();
            m_leftHashTree = null;
        }
        if (m_rightHashTree != null)
        {
            m_rightHashTree.Clear();
            m_rightHashTree = null;
        }
        
        if (m_dynamicMeshCube != null)
        {
            m_dynamicMeshCube.Clear();
        }
        
        if (m_meshPrefab != null)
        {
            GameObject.DestroyImmediate(m_meshPrefab);
            m_meshPrefab = null;
        }
    }
    
    /// <summary>
    /// Calculates statistics about this tree node and its subtrees.
    /// </summary>
    /// <param name="vertCount">Vertex counter.</param>
    /// <param name="triangleCount">Triangle counter.</param>
    /// <param name="nodeCount">Tree node counter.</param>
    public void ComputeStats(ref int vertCount, ref int triangleCount, ref int nodeCount)
    {
        if (m_leftHashTree != null)
        {
            m_leftHashTree.ComputeStats(ref vertCount, ref triangleCount, ref nodeCount);
        }
        if (m_meshPrefab != null)
        {
            vertCount += m_dynamicMeshCube.Vertices.Count;
            triangleCount += m_dynamicMeshCube.Triangles.Count;
        }
        nodeCount++;
        
        if (m_rightHashTree != null)
        {
            m_rightHashTree.ComputeStats(ref vertCount, ref triangleCount, ref nodeCount);
        }
    }
    
    /// <summary>
    /// Debug Draw will draw debug lines outlining the populated volumes.
    /// </summary>
    public void DebugDraw()
    {
        if (m_leftHashTree != null)
        {
            m_leftHashTree.DebugDraw();
        }
        if (m_dynamicMeshCube != null)
        {
            m_dynamicMeshCube.DebugDrawNormals();
        }
        if (m_rightHashTree != null)
        {
            m_rightHashTree.DebugDraw();
        }
    }

    /// <summary>
    /// Instantiate the Unity Prefab that will exist at this node location.
    /// </summary>
    /// <param name="hashkey">The spatial hashkey for this tree node.</param>
    /// <param name="prefab">The Unity Prefab object.</param>
    /// <param name="parent">The Unity transform of the GameObject parent of the prefab.</param>
    /// <param name="voxelResolution">The voxel resolution of the meshing cube.</param>
    private void InstantiatePrefab(int hashkey, GameObject prefab, Transform parent, int voxelResolution)
    {
        int x, y, z;
        ReverseHashKey(hashkey, out x, out y, out z);
        m_meshPrefab = (GameObject)GameObject.Instantiate(prefab);
        m_meshPrefab.transform.position = new Vector3(x, y, z);
        m_meshPrefab.transform.parent = parent;
        m_dynamicMeshCube = m_meshPrefab.GetComponent<DynamicMeshCube>();
        if (m_meshPrefab == null)
        {
            Debug.LogError("Game Object does not have the dynamic mesh component");
        }
        m_dynamicMeshCube.SetProperties(voxelResolution);
        m_dynamicMeshCube.Key = hashkey;
    }

    /// <summary>
    /// Computes the x, y, z integer coordinates that coorespond to a given hashkey.
    /// </summary>
    /// <param name="hashKey">The key to reverse.</param>
    /// <param name="x">The output x position.</param>
    /// <param name="y">The output y position.</param>
    /// <param name="z">The output z position.</param>
    private void ReverseHashKey(int hashKey, out int x, out int y, out int z) 
    {
        int flipLimit = m_maximumVolumeIndexDimension / 2;
        int temp = hashKey;
        
        x = temp % m_maximumVolumeIndexDimension;
        if (x > flipLimit)
        {
            // x is negative, but opposite sign of y
            x = x - m_maximumVolumeIndexDimension;
        }
        if (x < -flipLimit)
        {
            // x is positive, but opposite sign of y
            x = m_maximumVolumeIndexDimension + x;
        }
        temp -= x;
        temp /= m_maximumVolumeIndexDimension;
        
        y = temp % m_maximumVolumeIndexDimension;
        if (y > flipLimit)
        {
            // y is negative, but opposite sign of z
            y = y - m_maximumVolumeIndexDimension;
        }
        if (y < -flipLimit)
        {
            // y is positive, but opposite sign of z
            y = m_maximumVolumeIndexDimension + y;
        }
        
        temp -= y;
        z = temp / m_maximumVolumeIndexDimension;
    }

    /// <summary>
    /// Insert 3D point into the hash storage.  Creates a new meshing cube at the location if needed.
    /// </summary>
    /// <returns>The value of the voxel that received the insertion.</returns>
    /// <param name="hashkey">Hashkey index of the target node.</param>
    /// <param name="p">The 3D point.</param>
    /// <param name="obs">Observation vector from the camera to the point.</param>
    /// <param name="weight">Weight of the observation.</param>
    /// <param name="prefab">Unity Prefab to be instantiated at this node location if needed.</param>
    /// <param name="parent">Unity transform of the parent object for the prefab created at this node location.</param>
    /// <param name="voxelResolution">Resolution of the voxels for this meshing cube.</param>
    private float InsertPoint(int hashkey, Vector3 p, Vector3 obs, float weight, GameObject prefab, Transform parent, int voxelResolution) 
    {
        if (m_hashKey == hashkey) 
        {
            if (m_meshPrefab == null)
            {
                InstantiatePrefab(hashkey, prefab, parent, voxelResolution);
            }
            
            if (m_meshPrefab == null)
            {
                m_dynamicMeshCube = m_meshPrefab.GetComponent<DynamicMeshCube>();
            }
            if (m_meshPrefab == null)
            {
                Debug.Log("Error: cannot find DynamicMeshVolume");
                return 0;
            }
            
            if (m_dynamicMeshCube.IsRegenerating)
            {
                return 0;
            }
            
            // adjust weight of mutiple voxels along observation ray
            float result = m_dynamicMeshCube.InsertPoint(p, obs, weight, ref m_volumeIndex);
            Vector3 closerPoint = p - (obs * m_dynamicMeshCube.VoxelSize);
            Vector3 furtherPoint = p + (obs * m_dynamicMeshCube.VoxelSize);

            // voxel was inside the surface, back out one, and insert in the next closest voxel
            if (result > 0)
            {
                m_dynamicMeshCube.InsertPoint(closerPoint, p, obs, weight);
            }
            else
            {
                m_dynamicMeshCube.InsertPoint(furtherPoint, p, obs, weight);
            }

            if (m_volumeIndex[0] == 0)
            {
                int neighborHashKey = hashkey - 1;
                result = m_rootHashTree.InsertPoint(neighborHashKey, p, obs, weight, prefab, parent, voxelResolution);
            }
            if (m_volumeIndex[1] == 0)
            {
                int neighborHashKey = hashkey - m_maximumVolumeIndexDimension;
                result = m_rootHashTree.InsertPoint(neighborHashKey, p, obs, weight, prefab, parent, voxelResolution);
            }
            if (m_volumeIndex[2] == 0)
            {
                int neighborHashKey = hashkey - (m_maximumVolumeIndexDimension * m_maximumVolumeIndexDimension);
                result = m_rootHashTree.InsertPoint(neighborHashKey, p, obs, weight, prefab, parent, voxelResolution);
            }
            
            return result;
        }
        
        if (hashkey < m_hashKey)
        {
            if (m_leftHashTree == null)
            {
                m_leftHashTree = new VolumetricHashTree(m_rootHashTree, hashkey);
            }
            return m_leftHashTree.InsertPoint(hashkey, p, obs, weight, prefab, parent, voxelResolution);
        }
        else
        {
            if (m_rightHashTree == null)
            {
                m_rightHashTree = new VolumetricHashTree(m_rootHashTree, hashkey);
            }
            return m_rightHashTree.InsertPoint(hashkey, p, obs, weight, prefab, parent, voxelResolution);
        }
    }
}
