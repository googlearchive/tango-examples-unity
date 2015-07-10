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
using System;
using System.Collections;
using System.Collections.Generic;


/**
 * Volumetric Hash Tree
 * A binary tree datastructure that uses a hashkey based on the 3D coordinates.
 * Space surrounding the origin is divided into 1 meter cubes.  The hashkey is a 
 * reversible index into that volume.  allows quick indexing, and ray marching 
 * through space. Supports a 1000m x 1000m x 1000m volume centered at the origin.
 */
public class VolumetricHashTree {
    
    /**
     * Left subtree.
     */    
    private VolumetricHashTree m_leftHashTree = null;

    /**
     * Right subtree.
     */
    private VolumetricHashTree m_rightHashTree = null;

    /**
     * Pointer to the root node of the tree.
     */
    private VolumetricHashTree m_rootHashTree = null;
    
    /**
     * Prefab that will be instantiated for each tree node
     */
    private GameObject m_meshPrefab = null;

    /**
     * Dynamic Meshing class that will handle the data processing
     */
    private DynamicMeshCube m_dynamicMeshCube = null;

    /**
     * Unpacked spatial volume index of the position of this tree node
     */
    private int[] m_volumeIndex = new int[3];
    
    /**
     * HashKey of the position of this tree node
     */
    private int m_hashKey = 0;

    /**
     * Maximum dimension of 3D the hashkey space
     */
    private int m_maximumVolumeIndexDimension = 1000;//supports +/- 500m from origin along each axis

    /**
     * Epsilon tolerance for boundary insertions
     */
    private float m_epsilon = 0.01f;
    
    /**
     * Create a new tree node with a specified parent and hashkey
     * @param parent The parent tree node.
     * @param hashkey The spatial hashkey for this tree node.
     */
    public VolumetricHashTree(VolumetricHashTree parent, int hashkey) {
        if (parent == null)
            m_rootHashTree = this;
        else
            m_rootHashTree = parent;
        
        m_hashKey = hashkey;
    }
    
    /**
     * Get the DynamicMeshCube that contains the mesh data
     */
    public DynamicMeshCube DynamicMeshCube {
        get {
            return m_dynamicMeshCube;
        }
    }
    
    /**
     * Instantiate the Unity Prefab that will exist at this node location
     * @param hashkey The spatial hashkey for this tree node.
     * @param prefab The Unity Prefab object.
     * @param parent The Unity transform of the GameObject parent of the prefab.
     * @param voxelResolution The voxel resolution of the meshing cube.
     * @return void
     */
    private void InstantiatePrefab(int hashkey, GameObject prefab, Transform parent, int voxelResolution) {
        int x, y, z;
        ReverseHashKey(hashkey, out x, out y, out z);
        m_meshPrefab = (GameObject)GameObject.Instantiate (prefab);
        m_meshPrefab.transform.position = new Vector3 (x, y, z);
        m_meshPrefab.transform.parent = parent;
        m_dynamicMeshCube = m_meshPrefab.GetComponent<DynamicMeshCube> ();
        if (m_meshPrefab == null)
            Debug.LogError ("Game Object does not have the dynamic mesh component");
        m_dynamicMeshCube.SetProperties (voxelResolution);
        m_dynamicMeshCube.Key = hashkey;
    }


    /**
     * Enumeration function for iterating through the entire tree structure
     */
    public IEnumerable<VolumetricHashTree> GetEnumerable()
    {
        if (m_leftHashTree != null)
            foreach(VolumetricHashTree n in m_leftHashTree.GetEnumerable())
                yield return n;
        yield return this;
        if (m_rightHashTree != null)
            foreach (VolumetricHashTree n in m_rightHashTree.GetEnumerable())
                yield return n;
    }
    
    /**
     * Computes the spatial hashkey given a 3D point
     * @param p Point for hashing into the key space.
     * @return the hashkey value for the point.
     */
    public int GetHashKey(Vector3 p) {
        return (int)Mathf.Floor (p.x) + (int)(m_maximumVolumeIndexDimension * Mathf.Floor (p.y)) + (int)(m_maximumVolumeIndexDimension*m_maximumVolumeIndexDimension * Mathf.Floor (p.z));
    }

    /**
     * Computes the new spatial hashkey given an integer offset in the 3D volume
     * @return the hashkey value for the new position
     */
    public int OffsetHashKey(int startKey, int xOffset, int yOffset, int zOffset) 
    {
        return startKey + xOffset + m_maximumVolumeIndexDimension * yOffset + m_maximumVolumeIndexDimension*m_maximumVolumeIndexDimension * zOffset;
    }

    /**
     * Computes the x, y, z integer coordinates that coorespond to a given hashkey
     * @param hashKey the key to reverse
     * @param x the output x position
     * @param y the output y position
     * @param z the output z position
     * @return void
     */
    private void ReverseHashKey(int hashKey, out int x, out int y, out int z) 
    {
        int flipLimit = m_maximumVolumeIndexDimension / 2;
        int temp = hashKey;
        
        x = temp % m_maximumVolumeIndexDimension;
        if (x > flipLimit) //x is negative, but opposite sign of y
            x = x - m_maximumVolumeIndexDimension;
        if (x < -flipLimit) //x is positive, but opposite sign of y
            x = m_maximumVolumeIndexDimension + x;
        temp -= x;
        temp /= m_maximumVolumeIndexDimension;
        
        y = temp % m_maximumVolumeIndexDimension;
        if (y > flipLimit)
            y = y - m_maximumVolumeIndexDimension;//y is negative, but opposite sign of z
        if (y < -flipLimit)
            y = m_maximumVolumeIndexDimension + y;//y is positive, but opposite sign of z
        
        temp -= y;
        z = temp/m_maximumVolumeIndexDimension;
    }

    /**
     * Insert 3D point into the hash storage.  Creates a new meshing cube at the location if needed.
     * @param p the 3D point
     * @param obs observation vector from the camera to the point
     * @param weight weight of the observation
     * @param prefab Unity Prefab to be instantiated at this node location if needed
     * @param tranform Unity transform of the parent object for the prefab created at this node location
     * @param voxelResolution Resolution of the voxels for this meshing cube
     * @return the value of the voxel that received the insertion
     */
    public float InsertPoint(Vector3 p, Vector3 obs, float weight, GameObject prefab, Transform parent, int voxelResolution) {
        int hashKey = GetHashKey(p);
        return InsertPoint (hashKey, p, obs, weight, prefab, parent, voxelResolution);
    }

    /**
     * Insert 3D point into the hash storage.  Creates a new meshing cube at the location if needed.
     * @param hashkey hashkey index of the target node
     * @param p the 3D point
     * @param obs observation vector from the camera to the point
     * @param weight weight of the observation
     * @param prefab Unity Prefab to be instantiated at this node location if needed
     * @param tranform Unity transform of the parent object for the prefab created at this node location
     * @param voxelResolution Resolution of the voxels for this meshing cube
     * @return the value of the voxel that received the insertion
     */
    private float InsertPoint(int hashkey, Vector3 p, Vector3 obs, float weight, GameObject prefab, Transform parent, int voxelResolution) 
    {
        if (m_hashKey == hashkey) 
        {
            if(m_meshPrefab == null) {
                InstantiatePrefab(hashkey, prefab, parent, voxelResolution);
            }
            
            if(m_meshPrefab == null)
                m_dynamicMeshCube = m_meshPrefab.GetComponent<DynamicMeshCube> ();
            if(m_meshPrefab == null) {
                Debug.Log ("Error: cannot find DynamicMeshVolume");
                return 0;
            }
            
            if(m_dynamicMeshCube.IsRegenerating)
                return 0;
            
            //adjust weight of mutiple voxels along observation ray
            float result = m_dynamicMeshCube.InsertPoint(p, obs, weight, ref m_volumeIndex);
            Vector3 closerPoint = p-obs*m_dynamicMeshCube.VoxelSize;
            Vector3 furtherPoint = p+obs*m_dynamicMeshCube.VoxelSize;
            //voxel was inside the surface, back out one, and insert in the next closest voxel
            if(result > 0)
                m_dynamicMeshCube.InsertPoint(closerPoint, p, obs, weight);
            else
                m_dynamicMeshCube.InsertPoint(furtherPoint, p, obs, weight);
            
            
            if(m_volumeIndex[0] == 0) {
                int neighborHashKey = hashkey - 1;
                result = m_rootHashTree.InsertPoint(neighborHashKey, p,obs,weight,prefab,parent,voxelResolution);
            }
            if(m_volumeIndex[1] == 0) {
                int neighborHashKey = hashkey - m_maximumVolumeIndexDimension;
                result = m_rootHashTree.InsertPoint(neighborHashKey, p,obs,weight,prefab,parent,voxelResolution);
            }
            if(m_volumeIndex[2] == 0) {
                int neighborHashKey = hashkey - m_maximumVolumeIndexDimension*m_maximumVolumeIndexDimension;
                result = m_rootHashTree.InsertPoint(neighborHashKey, p,obs,weight,prefab,parent,voxelResolution);
            }
            
            return result;
        }
        
        if(hashkey < m_hashKey) {
            if(m_leftHashTree == null)
                m_leftHashTree = new VolumetricHashTree(m_rootHashTree, hashkey);
            return m_leftHashTree.InsertPoint(hashkey, p, obs, weight, prefab, parent, voxelResolution);
        } else {
            if(m_rightHashTree == null)
                m_rightHashTree = new VolumetricHashTree(m_rootHashTree, hashkey);
            return m_rightHashTree.InsertPoint(hashkey, p, obs, weight, prefab, parent, voxelResolution);
        }        
    }    

    /**
     * Raycast through the volume to quickly determine the list of voxels intersected.
     * @param start start point of the ray.
     * @param stop stop point of the ray.
     * @return list of populated voxels the ray intersects.
     */
    public List<Voxel> RaycastVoxelHitlist(Vector3 start, Vector3 stop) 
    {
        Vector3 dir = (stop - start).normalized;
        List<int> volumeHitKeys = new List<int>();
        volumeHitKeys.Add(GetHashKey(start));
        
        
        //x crosses
        if (dir.x > 0) 
        {
            for (float x = Mathf.Ceil(start.x)+m_epsilon; x < stop.x; x += 1) {
                float scale = (x - start.x) / dir.x;
                volumeHitKeys.Add (GetHashKey (start + scale * dir));
            }
        } 
        else 
        {
            for (float x = Mathf.Floor(start.x)-m_epsilon; x > stop.x; x -= 1) {
                float scale = (x - start.x) / dir.x;
                volumeHitKeys.Add (GetHashKey (start + scale * dir));
            }
        }
        
        //y crosses
        if (dir.y > 0) 
        {
            for (float y = Mathf.Ceil(start.y)+m_epsilon; y < stop.y; y += 1) {
                float scale = (y - start.y) / dir.y;
                volumeHitKeys.Add (GetHashKey (start + scale * dir));
            }
        } 
        else 
        {
            for (float y = Mathf.Floor(start.y)-m_epsilon; y > stop.y; y -= 1) {
                float scale = (y - start.y) / dir.y;
                volumeHitKeys.Add (GetHashKey (start + scale * dir));
            }
        }
        
        //z crosses
        if (dir.z > 0) 
        {
            for (float z = Mathf.Ceil(start.z)+m_epsilon; z < stop.z; z += 1) {
                float scale = (z - start.z) / dir.z;
                volumeHitKeys.Add (GetHashKey (start + scale * dir));
            }
        } 
        else 
        {
            for (float z = Mathf.Floor(start.z)-m_epsilon; z > stop.z; z -= 1) {
                float scale = (z - start.z) / dir.z;
                volumeHitKeys.Add (GetHashKey (start + scale * dir));
            }
        }
        
        List<Voxel> voxelHits = new List<Voxel> ();
        
        foreach (int volumeKey in volumeHitKeys) {
            VolumetricHashTree result = Query(volumeKey);
            if(result == null)
                continue;
            if(result.m_meshPrefab == null)
                continue;
            List<Voxel> voxels = result.m_dynamicMeshCube.RayCastVoxelHitlist(start, stop, dir);
            foreach(Voxel v in voxels){
                voxelHits.Add(v);
            }
        }
        
        return voxelHits;
    }

    /**
     * Query for the existence of a node at a hashKey location
     * @return the node if it exists, null if it does not.
     */
    public VolumetricHashTree Query(int hashKey) {
        if (hashKey == m_hashKey) {
            return this;
        }
        if(hashKey < m_hashKey) {
            if(m_leftHashTree != null)
                return m_leftHashTree.Query(hashKey);
        } else {
            if(m_rightHashTree != null)
                return m_rightHashTree.Query(hashKey);
        }
        return null;
    }

    /**
     * Clears the contents of this tree node and its subtrees
     */

    public void Clear() {
        if (m_leftHashTree != null) {
            m_leftHashTree.Clear ();
            m_leftHashTree = null;
        }
        if (m_rightHashTree != null) {
            m_rightHashTree.Clear ();
            m_rightHashTree = null;
        }
        
        if (m_dynamicMeshCube != null) {
            m_dynamicMeshCube.Clear ();
        }
        
        if (m_meshPrefab != null) {
            GameObject.DestroyImmediate (m_meshPrefab);
            m_meshPrefab = null;
        }
    }

    /**
     * Calculates statistics about this tree node and its subtrees
     * @param vertCount vertex counter
     * @param triangleCount triangle counter
     * @param nodeCount tree node counter
     */
    public void ComputeStats(ref int vertCount, ref int triangleCount, ref int nodeCount) {
        if (m_leftHashTree != null)
            m_leftHashTree.ComputeStats(ref vertCount, ref triangleCount, ref nodeCount);
        if (m_meshPrefab != null) {
            vertCount += m_dynamicMeshCube.Vertices.Count;
            triangleCount += m_dynamicMeshCube.Triangles.Count;
        }
        nodeCount++;
        
        if (m_rightHashTree != null)
            m_rightHashTree.ComputeStats(ref vertCount, ref triangleCount, ref nodeCount);
    }

    /**
     * Debug Draw will draw debug lines outlining the populated volumes
     */
    public void DebugDraw() {
        if (m_leftHashTree != null)
            m_leftHashTree.DebugDraw();
        if (m_dynamicMeshCube != null) {
            m_dynamicMeshCube.DebugDrawNormals();
        }
        if (m_rightHashTree != null)
            m_rightHashTree.DebugDraw();
    }
}
