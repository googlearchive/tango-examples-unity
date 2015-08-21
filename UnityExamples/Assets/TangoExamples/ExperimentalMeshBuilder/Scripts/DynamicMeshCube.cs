//-----------------------------------------------------------------------
// <copyright file="DynamicMeshCube.cs" company="Google">
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
/// Manages the voxels and mesh data for each cube volume.
/// </summary>
public class DynamicMeshCube : MonoBehaviour
{
    /// <summary>
    /// Resolution of the voxels which determines mesh resolution.
    /// </summary>
    public int m_voxelResolution = 10;

    /// <summary>
    /// Dimension size of the voxel grid.  This is larger than the voxel resolution for buffer margin on each side.
    /// </summary>
    private int m_voxelGridDimension = 10;

    /// <summary>
    /// Margin count for voxels.
    /// </summary>
    private int m_voxelMarginCount = 2;

    /// <summary>
    /// Size of each voxel.
    /// </summary>
    private float m_voxelSize = 0.1f;

    /// <summary>
    /// Threshold for boundard insertions.
    /// </summary>
    private float m_epsilon = 0.001f;

    /// <summary>
    /// Spatial hashkey of this meshing cube.
    /// </summary>
    private int m_hashKey = int.MinValue;

    /// <summary>
    /// Flag used for thread safety.
    /// </summary>
    private bool m_isRegenerating = false;
    
    /// <summary>
    /// Unity Mesh Filter object that handles mesh management.
    /// </summary>
    private MeshFilter m_meshFilter = null;

    /// <summary>
    /// List of vertices in the mesh.
    /// </summary>
    private List<Vector3> m_vertices = new List<Vector3>();

    /// <summary>
    /// List of normals in the mesh.
    /// </summary>
    private List<Vector3> m_normals = new List<Vector3>();

    /// <summary>
    /// List of triangles in the mesh.
    /// </summary>
    private List<int> m_triangles = new List<int>(); 

    /// <summary>
    /// List of texture uvs in the mesh.
    /// </summary>
    private List<Vector2> m_uvs = new List<Vector2>();

    /// <summary>
    /// Isolevel for surfce computation in polygonization.
    /// </summary>
    private float m_meshingIsolevel = 0.0f;

    /// <summary>
    /// Default initial voxel value.
    /// </summary>
    private float m_initialVoxelValue = -1.0f;

    /// <summary>
    /// Maximum voxel weight.
    /// </summary>
    private float m_maximumVoxelWeight = 100;

    /// <summary>
    /// Percentage change in the voxel value as a function of size for the mesh to be considered dirty.
    /// </summary>
    private float m_minimumVoxelPercentageChangeForDirty = 0.33f;

    /// <summary>
    /// Minimum change for the voxel to make the entire mesh dirty.  get calcuated using percentage change.
    /// </summary>
    private float m_minimumVoxelChangeForDirty = 0.01f; // will get updated when voxel size is computed

    /// <summary>
    /// Hash tree storage for the voxel data.
    /// </summary>
    private VoxelHashTree voxelStorage = new VoxelHashTree();

    /// <summary>
    /// LUT for UV coorinadate combinations.
    /// </summary>
    private Vector2[] uvOptions = new Vector2[4];

    /// <summary>
    /// Flag to indicate if mesh is dirty.
    /// </summary>
    private bool isDirty = true;

    /// <summary>
    /// Gets list of vertices in the mesh.
    /// </summary>
    /// <value>The vertices.</value>
    public List<Vector3> Vertices
    {
        get { return m_vertices; }
    }

    /// <summary>
    /// Gets list of vertices in the mesh.
    /// </summary>
    /// <value>The triangles.</value>
    public List<int> Triangles
    {
        get { return m_triangles; }
    }

    /// <summary>
    /// Gets list of UVs in the mesh.
    /// </summary>
    /// <value>The UVs.</value>
    public List<Vector2> UVs
    {
        get { return m_uvs; }
    }

    /// <summary>
    /// Gets regeneration state.
    /// </summary>
    /// <value><c>true</c> if this instance is regenerating; otherwise, <c>false</c>.</value>
    public bool IsRegenerating
    {
        get { return m_isRegenerating; }
    }

    /// <summary>
    /// Gets hashkey of this meshing cube.
    /// </summary>
    /// <value>The key.</value>
    public int Key
    {
        get { return m_hashKey; }
        set { m_hashKey = value; }
    }

    /// <summary>
    /// Prints Debug Info for this meshing cube.
    /// </summary>
    public void PrintDebugInfo()
    {
        string info = string.Empty;
        info += "Mesh Volume Key: " + m_hashKey + "\n";
        info += "Root Voxel Key: " + voxelStorage.Key + "\n";

        foreach (VoxelHashTree t in voxelStorage.GetEnumerable()) 
        {
            info += "  " + t.Key + "\n";
        }
        Debug.Log(info);
    }

    /// <summary>
    /// Gets or sets the size of the voxels.
    /// </summary>
    /// <value>The size of the voxel.</value>
    public float VoxelSize
    {
        get
        {
            return m_voxelSize;
        }

        set
        {
            m_voxelSize = value;
            m_epsilon = m_voxelSize / 100.0f;
        }
    }

    /// <summary>
    /// Initializes the Meshing Cube.
    /// </summary>
    public void Start()
    {
        uvOptions[0] = new Vector3(-1, -1);
        uvOptions[1] = new Vector3(0, 1);
        uvOptions[2] = new Vector3(1, 1);
        uvOptions[3] = new Vector3(0, 0);
        m_meshFilter = gameObject.GetComponent<MeshFilter>();
        if (m_meshFilter == null)
        {
            m_meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (m_meshFilter == null)
        {
            Debug.LogError("Could not get or add MeshFilter");
        }

        Mesh mesh = m_meshFilter.sharedMesh;
        if (mesh == null)
        {
            m_meshFilter.mesh = new Mesh();
            mesh = m_meshFilter.sharedMesh;
        }
        if (mesh == null) 
        {
            Debug.LogError("Could not get or create Mesh");
        }
    }

    /// <summary>
    /// Sets the voxel resolution for this meshing cube.
    /// </summary>
    /// <param name="voxelResolution">Number of voxel divisions in this cube.</param>
    public void SetProperties(int voxelResolution)
    {
        this.m_voxelResolution = voxelResolution;
        m_voxelGridDimension = voxelResolution + (2 * m_voxelMarginCount);
        VoxelSize = transform.localScale.x / voxelResolution;
        m_minimumVoxelChangeForDirty = VoxelSize * m_minimumVoxelPercentageChangeForDirty;
        Clear();
    }
    
    /// <summary>
    /// Gets dirty flag.
    /// </summary>
    /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
    public bool IsDirty
    {
        get { return isDirty; }
    }
    
    /// <summary>
    /// Computes hashkey for particular voxel index.
    /// </summary>
    /// <returns>The voxel hash key.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    public int ComputeVoxelHashKey(int x, int y, int z)
    {
        return x + m_voxelMarginCount
               + ((y + m_voxelMarginCount) * m_voxelGridDimension)
               + ((z + m_voxelMarginCount) * m_voxelGridDimension * m_voxelGridDimension);
    }

    /// <summary>
    /// Computes hashkey for particular 3D point.
    /// </summary>
    /// <returns>The voxel hash key.</returns>
    /// <param name="p">The point.</param>
    public int ComputeVoxelHashKey(Vector3 p)
    {
        return ComputeVoxelHashKey(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
    }

    /// <summary>
    /// Clips the point+dir ray to the bounds of the meshing cube.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="dir">The dir ray.</param>
    public void ClipRayToBounds(ref Vector3 point, Vector3 dir) 
    {
        if (point.x < 0)
        {
            point = point + (dir * (0 - point.x) / dir.x);
        }
        if (point.y < 0)
        {
            point = point + (dir * (0 - point.y) / dir.y);
        }
        if (point.z < 0)
        {
            point = point + (dir * (0 - point.z) / dir.z);
        }
        
        if (point.x > 1)
        {
            point = point + (dir * (1 - point.x) / dir.x);
        }
        if (point.y > 1)
        {
            point = point + (dir * (1 - point.y) / dir.y);
        }
        if (point.z > 1)
        {
            point = point + (dir * (1 - point.z) / dir.z);
        }
    }

    /// <summary>
    /// Raycast into this meshing cube for a list of voxels that are intersected.
    /// </summary>
    /// <returns>List of populated voxels that are intersected by the ray.</returns>
    /// <param name="start">Start point of the ray.</param>
    /// <param name="stop">Stop point of the ray.</param>
    /// <param name="dir">The unit direction vector of the ray.</param>
    public List<Voxel> RayCastVoxelHitlist(Vector3 start, Vector3 stop, Vector3 dir)
    {
        List<Voxel> hits = new List<Voxel>();
        List<int> hitKeys = new List<int>();

        Vector3 adjustedStart = start - transform.position;
        Vector3 adjustedStop = stop - transform.position;

        ClipRayToBounds(ref adjustedStart, dir);
        ClipRayToBounds(ref adjustedStop, dir);

        // converts it to integer space
        adjustedStart *= m_voxelResolution;
        adjustedStop *= m_voxelResolution;

        // x crosses
        if (dir.x > 0) 
        {
            for (float x = Mathf.Ceil(adjustedStart.x) + m_epsilon; x < adjustedStop.x; x++)
            {
                float scale = (x - adjustedStart.x) / dir.x;
                hitKeys.Add(ComputeVoxelHashKey(adjustedStart + (scale * dir)));
            }
        } 
        else 
        {
            for (float x = Mathf.Floor(adjustedStart.x) - m_epsilon; x > adjustedStop.x; x -= 1)
            {
                float scale = (x - adjustedStart.x) / dir.x;
                hitKeys.Add(ComputeVoxelHashKey(adjustedStart + (scale * dir)));
            }
        }
        
        // y crosses
        if (dir.y > 0) 
        {
            for (float y = Mathf.Ceil(adjustedStart.y) + m_epsilon; y < adjustedStop.y; y += 1)
            {
                float scale = (y - adjustedStart.y) / dir.y;
                hitKeys.Add(ComputeVoxelHashKey(adjustedStart + (scale * dir)));
            }
        } 
        else 
        {
            for (float y = Mathf.Floor(adjustedStart.y) - m_epsilon; y > adjustedStop.y; y -= 1)
            {
                float scale = (y - adjustedStart.y) / dir.y;
                hitKeys.Add(ComputeVoxelHashKey(adjustedStart + (scale * dir)));
            }
        }
        
        // z crosses
        if (dir.z > 0) 
        {
            for (float z = Mathf.Ceil(adjustedStart.z) + m_epsilon; z < adjustedStop.z; z += 1)
            {
                float scale = (z - adjustedStart.z) / dir.z;
                hitKeys.Add(ComputeVoxelHashKey(adjustedStart + (scale * dir)));
            }
        } 
        else 
        {
            for (float z = Mathf.Floor(adjustedStart.z) - m_epsilon; z > adjustedStop.z; z -= 1)
            {
                float scale = (z - adjustedStart.z) / dir.z;
                hitKeys.Add(ComputeVoxelHashKey(adjustedStart + (scale * dir)));
            }
        }

        foreach (int key in hitKeys) 
        {
            Voxel v = voxelStorage.Query(key);
            if (v != null)
            {
                hits.Add(v);
            }
        }

        return hits;
    }

    /// <summary>
    /// Query if a voxel exists at the query location.
    /// </summary>
    /// <returns>The voxel if it exists, null if it does not exist.</returns>
    /// <param name="x">X query index.</param>
    /// <param name="y">Y query index.</param>
    /// <param name="z">Z query index.</param>
    public Voxel QueryVoxel(int x, int y, int z)
    {
        return voxelStorage.Query(ComputeVoxelHashKey(x, y, z));
    }

    /// <summary>
    /// Query if a voxel exists at the query location.  If it does not exist, create it.
    /// </summary>
    /// <returns>The created voxel.</returns>
    /// <param name="x">X query index.</param>
    /// <param name="y">Y query index.</param>
    /// <param name="z">Z query index.</param>
    public Voxel QueryCreateVoxel(int x, int y, int z)
    {
        int hashKey = ComputeVoxelHashKey(x, y, z);
        Voxel v = voxelStorage.Query(hashKey);
        if (v == null)
        {
            v = InitializeVoxel(x, y, z, m_voxelSize, m_initialVoxelValue, 0);
            voxelStorage.Insert(v, hashKey);
        }
        return v;
    }
    
    /// <summary>
    /// Insert point into this meshing cube, with output position index of the affected voxel.
    /// </summary>
    /// <returns>The final value of the adjusted voxel.</returns>
    /// <param name="p">Point to be inserted.</param>
    /// <param name="obs">Observation direction of this point.</param>
    /// <param name="weight">Weight of the observation.</param>
    /// <param name="index">Index output position index of the voxel.</param>
    public float InsertPoint(Vector3 p, Vector3 obs, float weight, ref int[] index)
    {
        if (m_isRegenerating)
        {
            return -1;
        }
        index[0] = Mathf.FloorToInt((p.x - transform.position.x) / m_voxelSize);
        index[1] = Mathf.FloorToInt((p.y - transform.position.y) / m_voxelSize);
        index[2] = Mathf.FloorToInt((p.z - transform.position.z) / m_voxelSize);

        return AdjustWeight(QueryCreateVoxel(index[0], index[1], index[2]), p - transform.position, obs, weight);
    }

    /// <summary>
    /// Insert point into this meshing cube.
    /// </summary>
    /// <returns>The point.</returns>
    /// <param name="queryPoint">Query point.</param>
    /// <param name="p">Point to be inserted.</param>
    /// <param name="obs">Observation direction of this point.</param>
    /// <param name="weight">Weight of the observation.</param>
    public float InsertPoint(Vector3 queryPoint, Vector3 p, Vector3 obs, float weight)
    {
        if (m_isRegenerating)
        {
            return -1;
        }

        int x = Mathf.FloorToInt((queryPoint.x - transform.position.x) / m_voxelSize);
        int y = Mathf.FloorToInt((queryPoint.y - transform.position.y) / m_voxelSize);
        int z = Mathf.FloorToInt((queryPoint.z - transform.position.z) / m_voxelSize);

        return AdjustWeight(QueryCreateVoxel(x, y, z), p - transform.position, obs, weight);
    }

    /// <summary>
    /// Clears data from the meshing cube.
    /// </summary>
    public void Clear()
    {
        if (m_vertices != null)
        {
            m_vertices.Clear();
        }
        if (m_triangles != null)
        {
            m_triangles.Clear();
        }
        if (m_normals != null)
        {
            m_normals.Clear();
        }
        if (m_uvs != null)
        {
            m_uvs.Clear();
        }
        if (voxelStorage != null)
        {
            voxelStorage.Clear();
        }
        if (m_meshFilter != null)
        {
            if (m_meshFilter.sharedMesh != null)
            {
                m_meshFilter.sharedMesh.Clear();
            }
        }

        isDirty = true;
    }

    /// <summary>
    /// EXPERIMENTAL - simplfying planar sections of the mesh.
    /// </summary>
    /// <param name="v0">Voxel 0.</param>
    /// <param name="v1">Voxel 1.</param>
    /// <param name="v2">Voxel 2.</param>
    /// <param name="v3">Voxel 3.</param>
    public void SimplifyPlanarGroup(Voxel v0, Voxel v1, Voxel v2, Voxel v3)
    {
        Voxel[] voxels = new Voxel[4];
        voxels[0] = v0;
        voxels[1] = v1;
        voxels[2] = v2;
        voxels[3] = v3;
        
        int totalTriangles = 0;
        Vector3 averageNormal = new Vector3(0, 0, 0);
        List<int> triIndicies = new List<int>();
        
        foreach (Voxel v in voxels)
        {
            if (v == null)
            {
                return;
            }
            
            // only do planar groups right now
            if (v.trianglesIndicies.Count != 2)
            {
                return;
            }
            totalTriangles += v.trianglesIndicies.Count;
            foreach (int triIndex in v.trianglesIndicies)
            {
                averageNormal += m_normals[m_triangles[triIndex]];
            }
        }
        
        averageNormal /= totalTriangles;
        
        // too contentious
        if (averageNormal.magnitude < 0.75f)
        {
            return;
        }
        
        // we have 4 voxel neighors with triangles
        // see if the normals are within tolernace
        float variance = 0;
        foreach (Voxel v in voxels)
        {
            foreach (int triIndex in v.trianglesIndicies)
            {
                variance += Vector3.SqrMagnitude(m_normals[m_triangles[triIndex]] - averageNormal);
                triIndicies.Add(triIndex);
            }
        }
        variance /= totalTriangles;
        
        if (variance > 0.05)
        {
            return;
        }
        
        // we have triangles in a plane
        Vector3 avg = new Vector3();
        foreach (int index in triIndicies)
        {
            avg += m_vertices[m_triangles[index]];
            avg += m_vertices[m_triangles[index + 1]];
            avg += m_vertices[m_triangles[index + 2]];
        }
        avg /= totalTriangles * 3;
        
        foreach (int index in triIndicies)
        {
            bool remove = false;
            if (Vector3.SqrMagnitude(m_vertices[m_triangles[index]] - avg) < 0.05f)
            {
                remove = true;
            }
            if (Vector3.SqrMagnitude(m_vertices[m_triangles[index + 1]] - avg) < 0.05f)
            {
                remove = true;
            }
            if (Vector3.SqrMagnitude(m_vertices[m_triangles[index + 2]] - avg) < 0.05f)
            {
                remove = true;
            }
            
            if (remove)
            {
                m_triangles.RemoveAt(index);
                m_triangles.RemoveAt(index + 1);
                m_triangles.RemoveAt(index + 2);
            }
        }
        
        DebugDrawing.CrossX(avg + transform.position, 0.01f, Color.magenta);
        
        // lets create the neighbor graph
    }
    
    /// <summary>
    /// EXPERIMENTAL - simpligy planar parts of the mesh.
    /// </summary>
    /// <param name="v">Voxel to simplify around.</param>
    public void SimplifyByVoxel(Voxel v)
    {
        // this conditions te input to the polygonize, and make sure all the neighbors have valid entries
        // if all input voxels are gauranteed to not be edge voxels, this may not be necessary.
        Voxel v0 = v;
        Voxel v1 = QueryVoxel(v.xID + 1, v.yID, v.zID);
        Voxel v2 = QueryVoxel(v.xID + 1, v.yID + 1, v.zID);
        Voxel v3 = QueryVoxel(v.xID, v.yID + 1, v.zID);
        Voxel v4 = QueryVoxel(v.xID, v.yID, v.zID + 1);
        Voxel v5 = QueryVoxel(v.xID + 1, v.yID, v.zID + 1);
        Voxel v6 = QueryVoxel(v.xID, v.yID + 1, v.zID + 1);
        
        // three planar directions.. diagonals aren't necessary?
        SimplifyPlanarGroup(v0, v1, v2, v3);
        SimplifyPlanarGroup(v0, v1, v4, v5);
        SimplifyPlanarGroup(v0, v2, v3, v6);
    }
    
    /// <summary>
    /// Rebuild the Unity mesh.
    /// </summary>
    /// <returns>1 if successful, -1 if there was an error.</returns>
    public int RegenerateMesh()
    {
        if (!isDirty)
        {
            return 0;
        }
        if (m_isRegenerating)
        {
            return 0;
        }
        
        if (m_meshFilter == null)
        {
            Debug.Log("mesh filter is null");
            return -1;
        }
        
        Mesh mesh = m_meshFilter.sharedMesh;
        if (mesh == null)
        {
            Debug.Log("shared mesh was null, creating new");
            m_meshFilter.mesh = new Mesh();
            mesh = m_meshFilter.sharedMesh;
        }
        
        if (mesh == null)
        {
            Debug.Log("mesh is null");
            return -1;
        }
        
        m_isRegenerating = true;
        
        mesh.Clear();
        m_vertices.Clear();
        m_triangles.Clear();
        m_normals.Clear();
        m_uvs.Clear();
        
        PrepareVoxels();
        
        foreach (VoxelHashTree t in voxelStorage.GetEnumerable())
        {
            if (t.Voxel == null)
            {
                Debug.Log("Error: VoxelTree has null voxel");
                continue;
            }
            
            if (Polygonize(t.Voxel) == 0)
            {
                // no triangles were created, consider deleting this voxel?
                // just delecting, causes a lot of re-creation.  need o be smarter
            }
        }
        
        SetMesh();
        isDirty = false;
        m_isRegenerating = false;
        
        return 1;
    }
    
    /// <summary>
    /// Update the Unity Meshfilter data.
    /// </summary>
    public void SetMesh()
    {
        if (m_vertices == null)
        {
            Debug.Log("vertices null");
            return;
        }
        if (m_normals == null)
        {
            Debug.Log("normals null");
            return;
        }
        if (m_uvs == null)
        {
            Debug.Log("uvs null");
            return;
        }
        if (m_triangles == null)
        {
            Debug.Log("triangles null");
            return;
        }
        
        if (m_meshFilter == null)
        {
            Debug.Log("mf is null");
            return;
        }
        
        Mesh mesh = m_meshFilter.sharedMesh;
        if (mesh == null)
        {
            m_meshFilter.mesh = new Mesh();
            mesh = m_meshFilter.sharedMesh;
        }
        
        if (mesh == null)
        {
            Debug.Log("mesh is null");
            return;
        }
        mesh.Clear();
        mesh.MarkDynamic();
        mesh.vertices = m_vertices.ToArray();
        mesh.normals = m_normals.ToArray();
        mesh.uv = m_uvs.ToArray();
        
        if (mesh.vertices == null)
        {
            Debug.Log("mesh vertices null");
            return;
        }
        
        if (mesh.normals == null)
        {
            Debug.Log("mesh normals null");
            return;
        }
        
        if (mesh.uv == null)
        {
            Debug.Log("mesh uv null");
            return;
        }
        
        mesh.triangles = m_triangles.ToArray();
        
        if (mesh.triangles == null)
        {
            Debug.Log("mesh triangles null");
            return;
        }
        
        mesh.RecalculateBounds();
        mesh.Optimize();
        
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    
    /// <summary>
    /// Debug Draw the normals.
    /// </summary>
    public void DebugDrawNormals()
    {
        for (int i = 0; i < m_vertices.Count; i++)
        {
            Vector3 p = transform.position + m_vertices[i];
            Debug.DrawLine(p, p + (m_normals[i] * m_voxelSize), Color.red);
        }
    }

    /// <summary>
    /// Initialize a new voxel at a given location.
    /// </summary>
    /// <returns>The voxel.</returns>
    /// <param name="xID">X index.</param>
    /// <param name="yID">Y index.</param>
    /// <param name="zID">Z index.</param>
    /// <param name="voxelSize">Size of the voxel.</param>
    /// <param name="initialValue">Initial value of the voxel.</param>
    /// <param name="initialWeight">Initial weight of the voxel.</param>
    private Voxel InitializeVoxel(int xID, int yID, int zID, float voxelSize, float initialValue, float initialWeight)
    {
        Voxel v = new Voxel();
        v.size = voxelSize;
        v.value = initialValue;
        v.weight = initialWeight;
        v.normal = new Vector3(0, 1, 0);
        v.parent = transform;
        v.xID = xID;
        v.yID = yID;
        v.zID = zID;
        
        v.anchor = new Vector3(xID * v.size, yID * v.size, zID * v.size);
        v.anchor.x += v.size / 2;
        v.anchor.y += v.size / 2;
        v.anchor.z += v.size / 2;
        
        v.neighborsCreated = false;
        
        return v;
    }

    /// <summary>
    /// Adjust the weight of the voxel given the 3D point observation.
    /// </summary>
    /// <returns>The final value of the adjusted voxel.</returns>
    /// <param name="v">The Voxel to be adjusted.</param>
    /// <param name="p">The point inserted.</param>
    /// <param name="obs">Observation direction of the point.</param>
    /// <param name="weight">Weight of the observation .</param>
    private float AdjustWeight(Voxel v, Vector3 p, Vector3 obs, float weight)
    {
        if (v.weight > m_maximumVoxelWeight)
        {
            v.weight += weight; // keep accumulating, so we know when to subdivide
            return v.value;
        }
        
        float penetration = Vector3.Dot(obs, v.anchor - p);
        v.value = ((v.value * v.weight) + (penetration * weight)) / (v.weight + weight);
        v.weight += weight;
        
        if (v.value < -1)
        {
            v.value = -1;
            v.weight = 0;
        }
        
        ////        if (v.value > 1)
        ////            v.value = 1;
        ////        if (v.weight > maximumWeight)
        ////            v.weight = maximumWeight;
        
        if (Mathf.Abs(v.lastMeshedValue - v.value) > m_minimumVoxelChangeForDirty)
        {
            isDirty = true;
        }
        return v.value;
    }

    /// <summary>
    /// Walks through the voxel space prior to meshing to ensure all populated voxel have neighbors that are below
    /// the isosurface. Having populated voxels surrounded by neighbors below the isosurface is required to create
    /// valid meshes.
    /// </summary>
    private void PrepareVoxels()
    {
        // for each voxel above ISO, making sure neighboring lower voxels exist
        foreach (VoxelHashTree t in voxelStorage.GetEnumerable())
        {
            Voxel v = t.Voxel;

            if (v == null)
            {
                Debug.Log("Error: Prepare Voxels - VoxelTree has null voxel");
                continue;
            }

            // clear the stored triangle data used to compute voxel normal
            v.trianglesIndicies.Clear();

            if (v.neighborsCreated)
            {
                continue;
            }

            // create lower padding voxels as needed, so marching cubes will work correctly
            if (v.value > m_meshingIsolevel)
            {
                // neighboring 8
                QueryCreateVoxel(v.xID - 1, v.yID - 1, v.zID);
                QueryCreateVoxel(v.xID - 1, v.yID, v.zID);
                QueryCreateVoxel(v.xID - 1, v.yID + 1, v.zID);

                QueryCreateVoxel(v.xID, v.yID - 1, v.zID);
                QueryCreateVoxel(v.xID, v.yID + 1, v.zID);

                QueryCreateVoxel(v.xID + 1, v.yID - 1, v.zID);
                QueryCreateVoxel(v.xID + 1, v.yID, v.zID);
                QueryCreateVoxel(v.xID + 1, v.yID + 1, v.zID);

                // upper 9
                QueryCreateVoxel(v.xID - 1, v.yID - 1, v.zID + 1);
                QueryCreateVoxel(v.xID - 1, v.yID, v.zID + 1);
                QueryCreateVoxel(v.xID - 1, v.yID + 1, v.zID + 1);
                
                QueryCreateVoxel(v.xID, v.yID + 1, v.zID + 1);
                QueryCreateVoxel(v.xID, v.yID, v.zID + 1);
                QueryCreateVoxel(v.xID, v.yID - 1, v.zID + 1);
                
                QueryCreateVoxel(v.xID + 1, v.yID - 1, v.zID + 1);
                QueryCreateVoxel(v.xID + 1, v.yID, v.zID + 1);
                QueryCreateVoxel(v.xID + 1, v.yID + 1, v.zID + 1);

                // lower 9
                QueryCreateVoxel(v.xID - 1, v.yID - 1, v.zID - 1);
                QueryCreateVoxel(v.xID - 1, v.yID, v.zID - 1);
                QueryCreateVoxel(v.xID - 1, v.yID + 1, v.zID - 1);
                
                QueryCreateVoxel(v.xID, v.yID + 1, v.zID - 1);
                QueryCreateVoxel(v.xID, v.yID, v.zID - 1);
                QueryCreateVoxel(v.xID, v.yID - 1, v.zID - 1);
                
                QueryCreateVoxel(v.xID + 1, v.yID - 1, v.zID - 1);
                QueryCreateVoxel(v.xID + 1, v.yID, v.zID - 1);
                QueryCreateVoxel(v.xID + 1, v.yID + 1, v.zID - 1);

                v.neighborsCreated = true;
            }
        }
    }

    /// <summary>
    /// Calculate the UV coordinates for a triangle.
    /// </summary>
    /// <param name="ia">First triangle vertex index.</param>
    /// <param name="ib">Second triangle vertex index.</param>
    /// <param name="ic">Third triangle vertex index.</param>
    private void CalculateUVs(int ia, int ib, int ic)
    {
        // likely bug, if it winds down to the last face
        // it forces it to be valid, which will alter 
        // an existing face without checking that face
        // may not be resolvable without doing a full
        // 3-color graph solver
        if (m_uvs[ia] == uvOptions[0])
        {
            if (m_uvs[ib] == uvOptions[0])
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ia] = uvOptions[1];
                    m_uvs[ib] = uvOptions[2];
                    m_uvs[ic] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[1])
                {
                    m_uvs[ia] = uvOptions[2];
                    m_uvs[ib] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[2])
                {
                    m_uvs[ia] = uvOptions[1];
                    m_uvs[ib] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[3])
                {
                    m_uvs[ia] = uvOptions[1];
                    m_uvs[ib] = uvOptions[2];
                }
            }
            else if (m_uvs[ib] == uvOptions[1])
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ia] = uvOptions[2];
                    m_uvs[ic] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[2])
                {
                    m_uvs[ia] = uvOptions[1];
                }
                else if (m_uvs[ic] == uvOptions[3])
                {
                    m_uvs[ia] = uvOptions[2];
                }
            }
            else if (m_uvs[ib] == uvOptions[2])
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ia] = uvOptions[1];
                    m_uvs[ic] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[1])
                {
                    m_uvs[ia] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[3])
                {
                    m_uvs[ia] = uvOptions[1];
                }
            }
            else if (m_uvs[ib] == uvOptions[3])
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ia] = uvOptions[1];
                    m_uvs[ic] = uvOptions[2];
                }
                else if (m_uvs[ic] == uvOptions[1])
                {
                    m_uvs[ia] = uvOptions[2];
                }
                else if (m_uvs[ic] == uvOptions[2])
                {
                    m_uvs[ia] = uvOptions[1];
                }
            }
        }
        else if (m_uvs[ia] == uvOptions[1])
        {
            if ((m_uvs[ib] == uvOptions[0]) || (m_uvs[ib] == uvOptions[1]))
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ib] = uvOptions[2];
                    m_uvs[ic] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[2])
                {
                    m_uvs[ib] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[3])
                {
                    m_uvs[ib] = uvOptions[2];
                }
            }
            else if (m_uvs[ib] == uvOptions[2])
            {
                m_uvs[ic] = uvOptions[3];
            }
            else if (m_uvs[ib] == uvOptions[3])
            {
                m_uvs[ic] = uvOptions[2];
            }
        }
        else if (m_uvs[ia] == uvOptions[2])
        {
            if ((m_uvs[ib] == uvOptions[0]) || (m_uvs[ib] == uvOptions[2]))
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ib] = uvOptions[1];
                    m_uvs[ic] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[1])
                {
                    m_uvs[ib] = uvOptions[3];
                }
                else if (m_uvs[ic] == uvOptions[3])
                {
                    m_uvs[ib] = uvOptions[1];
                }
            }
            else if (m_uvs[ib] == uvOptions[1])
            {
                m_uvs[ic] = uvOptions[3];
            }
            else if (m_uvs[ib] == uvOptions[3])
            {
                m_uvs[ic] = uvOptions[1];
            }
        }
        if (m_uvs[ia] == uvOptions[3])
        {
            if ((m_uvs[ib] == uvOptions[0]) || (m_uvs[ib] == uvOptions[3]))
            {
                if (m_uvs[ic] == uvOptions[0])
                {
                    m_uvs[ib] = uvOptions[1];
                    m_uvs[ic] = uvOptions[2];
                }
                else if (m_uvs[ic] == uvOptions[1])
                {
                    m_uvs[ib] = uvOptions[2];
                }
                else if (m_uvs[ic] == uvOptions[2])
                {
                    m_uvs[ib] = uvOptions[1];
                }
            }
            else if (m_uvs[ib] == uvOptions[1])
            {
                m_uvs[ic] = uvOptions[2];
            }
            else if (m_uvs[ib] == uvOptions[2])
            {
                m_uvs[ic] = uvOptions[1];
            }
        }
    }

    /// <summary>
    /// For a given voxel, execute the polygonization algorithm to generate mesh vertices, triangles, normals, and UVs.
    /// </summary>
    /// <param name="v">The voxel to mesh.</param>
    /// <returns>Number of created triangles.</returns>
    private int Polygonize(Voxel v)
    {
        if (v == null)
        {
            return 0;
        }

        // allows us to track if the mesh is dirty and needs updating
        // needs to be above the early exit checks, otherwise voxels 
        // keep getting marked as dirty
        v.lastMeshedValue = v.value; 

        // keep the meshing within the volume bounds
        if (v.xID >= m_voxelResolution)
        {
            return 0;
        }
        if (v.yID >= m_voxelResolution)
        {
            return 0;
        }
        if (v.zID >= m_voxelResolution)
        {
            return 0;
        }
        if (v.xID < 0)
        {
            return 0;
        }
        if (v.yID < 0)
        {
            return 0;
        }
        if (v.zID < 0)
        {
            return 0;
        }

        Voxel v0 = v;

        // this conditions te input to the polygonize, and make sure all the neighbors have valid entries
        // if all input voxels are gauranteed to not be edge voxels, this may not be necessary.

        // OPIMTIZATION - voxel can store meshing neighbors, rather than query each
        // skip if they are null
        Voxel v1 = QueryVoxel(v.xID + 1, v.yID, v.zID);
        if (v1 == null)
        {
            return 0;
        }
        Voxel v2 = QueryVoxel(v.xID + 1, v.yID + 1, v.zID);
        if (v2 == null)
        {
            return 0;
        }
        Voxel v3 = QueryVoxel(v.xID, v.yID + 1, v.zID);
        if (v3 == null) 
        {
            return 0;
        }
        Voxel v4 = QueryVoxel(v.xID, v.yID, v.zID + 1);
        if (v4 == null)
        {
            return 0;
        }
        Voxel v5 = QueryVoxel(v.xID + 1, v.yID, v.zID + 1);
        if (v5 == null) 
        {
            return 0;
        }
        Voxel v6 = QueryVoxel(v.xID + 1, v.yID + 1, v.zID + 1);
        if (v6 == null) 
        {
            return 0;
        }
        Voxel v7 = QueryVoxel(v.xID, v.yID + 1, v.zID + 1);
        if (v7 == null) 
        {
            return 0;
        }

        int triangleStart = m_triangles.Count;
        int createdTriangles = Polygonizer.Process(m_meshingIsolevel,
                                                   v0.value,
                                                   v1.value,
                                                   v2.value,
                                                   v3.value,
                                                   v4.value,
                                                   v5.value,
                                                   v6.value,
                                                   v7.value,
                                                   v0.anchor,
                                                   v1.anchor,
                                                   v2.anchor,
                                                   v3.anchor,
                                                   v4.anchor,
                                                   v5.anchor,
                                                   v6.anchor,
                                                   v7.anchor,
                                                   ref m_vertices,
                                                   ref m_triangles);

        // compute per vertex normal
        int addedVertCount = 0;
        while (m_normals.Count < m_vertices.Count)
        {
            m_normals.Add(new Vector3(0, 1, 0));
            m_uvs.Add(new Vector2(-1, -1));
            addedVertCount++;
        }

        for (int i = triangleStart; i < m_triangles.Count; i += 3)
        {
            v.trianglesIndicies.Add(i); // add it to each voxel this impacted?

            int ia = m_triangles[i];
            int ib = m_triangles[i + 1];
            int ic = m_triangles[i + 2];
            Vector3 a = m_vertices[ia];
            Vector3 b = m_vertices[ib];
            Vector3 c = m_vertices[ic];
            
            Vector3 n = Vector3.Cross(a - b, b - c).normalized;
            m_normals[ia] = n;
            m_normals[ib] = n;
            m_normals[ic] = n;

            CalculateUVs(ia, ib, ic);
        }

        // adjust per voxel normal
        return createdTriangles;
    }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    private void Update()
    {
    }
}
