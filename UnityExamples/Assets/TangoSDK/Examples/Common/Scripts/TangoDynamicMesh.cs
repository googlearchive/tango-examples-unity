//-----------------------------------------------------------------------
// <copyright file="TangoDynamicMesh.cs" company="Google">
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tango;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Updates a mesh dynamically based on the ITango3DReconstruction callbacks.
/// 
/// The "mesh" that is updated by TangoDynamicMesh is actually a collection of children split along grid boundaries.
/// If you want these children to draw or participate in physics, attach a MeshRenderer or MeshCollider to this object.
/// Any generated children will get copies of the MeshRenderer or MeshCollider or both.
/// </summary>
public class TangoDynamicMesh : MonoBehaviour, ITango3DReconstruction
{
    /// <summary>
    /// If set, debugging info is displayed.
    /// </summary>
    public bool m_enableDebugUI = true;

    /// <summary>
    /// How much the dynamic mesh should grow its internal arrays.
    /// </summary>
    private const float GROWTH_FACTOR = 1.5f;

    /// <summary>
    /// Maximum amount of time to spend each frame extracting meshes.
    /// </summary>
    private const int TIME_BUDGET_MS = 10;

    /// <summary>
    /// The initial amount of vertices for a single dynamic mesh.
    /// </summary>
    private const int INITIAL_VERTEX_COUNT = 100;

    /// <summary>
    /// The initial amount of indexes for a single dynamic mesh.
    /// </summary>
    private const int INITIAL_INDEX_COUNT = 99;

    /// <summary>
    /// How much the texture coordinates change relative to the actual distance.
    /// </summary>
    private const float UV_PER_METERS = 10;

    /// <summary>
    /// The TangoApplication for the scene.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// The mesh renderer on this object.  This mesh renderer will get used on all the DynamicMesh objects created.
    /// </summary>
    private MeshRenderer m_meshRenderer;

    /// <summary>
    /// The mesh collider on this object.  This mesh collider will get used on all the DynamicMesh objects created.
    /// </summary>
    private MeshCollider m_meshCollider;

    /// <summary>
    /// Hash table to quickly get access to a dynamic mesh based on its position.
    /// </summary>
    private Dictionary<Tango3DReconstruction.GridIndex, TangoSingleDynamicMesh> m_meshes;

    /// <summary>
    /// List of grid indices that need to get extracted.
    /// </summary>
    private List<Tango3DReconstruction.GridIndex> m_gridIndexToUpdate;

    /// <summary>
    /// Backlog of grid indices we haven't had time to process.
    /// </summary>
    private HashSet<Tango3DReconstruction.GridIndex> m_gridUpdateBacklog;

    /// <summary>
    /// Debug info: Total number of vertices in the dynamic mesh.
    /// </summary>
    private int m_debugTotalVertices;

    /// <summary>
    /// Debug info: Total number of triangle indexes in the dynamic mesh.
    /// </summary>
    private int m_debugTotalTriangles;

    /// <summary>
    /// Debug info: Amount of time spent most recently updating the meshes.
    /// </summary>
    private float m_debugRemeshingTime;

    /// <summary>
    /// Debug info: Amount of meshes updated most recently.
    /// </summary>
    private int m_debugRemeshingCount;

    /// <summary>
    /// Gets the number of queued mesh updates still waiting for processing.
    /// 
    /// May be slightly overestimated if there have been too many updates to process and some
    /// have been pushed to the backlog.
    /// </summary>
    /// <value>The number of queued mesh updates.</value>
    public int NumQueuedMeshUpdates
    {
        get
        {
            return m_gridIndexToUpdate.Count + m_gridUpdateBacklog.Count;
        }
    }

    /// <summary>
    /// Unity Awake callback.
    /// </summary>
    public void Awake()
    {
        m_tangoApplication = GameObject.FindObjectOfType<TangoApplication>();
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);
        }

        m_meshes = new Dictionary<Tango3DReconstruction.GridIndex, TangoSingleDynamicMesh>(100);
        m_gridIndexToUpdate = new List<Tango3DReconstruction.GridIndex>(100);
        m_gridUpdateBacklog = new HashSet<Tango3DReconstruction.GridIndex>();

        // Cache the renderer and collider on this object.
        m_meshRenderer = GetComponent<MeshRenderer>();
        if (m_meshRenderer != null)
        {
            m_meshRenderer.enabled = false;
        }

        m_meshCollider = GetComponent<MeshCollider>();
        if (m_meshCollider != null)
        {
            m_meshCollider.enabled = false;
        }
    }

    /// <summary>
    /// Unity Update callback.
    /// </summary>
    public void Update()
    {
        List<Tango3DReconstruction.GridIndex> needsResize = new List<Tango3DReconstruction.GridIndex>();

        int it;
        int startTimeMS = (int)(Time.realtimeSinceStartup * 1000);
        for (it = 0; it < m_gridIndexToUpdate.Count; ++it)
        {
            Tango3DReconstruction.GridIndex gridIndex = m_gridIndexToUpdate[it];

            if (_GoneOverTimeBudget(startTimeMS))
            {
                Debug.Log(string.Format(
                    "TangoDynamicMesh.Update() ran over budget with {0}/{1} grid indexes processed.",
                    it, m_gridIndexToUpdate.Count));
                break;
            }

            _UpdateMeshAtGridIndex(gridIndex, needsResize);
            m_gridUpdateBacklog.Remove(gridIndex);
        }

        // While we have time left over, go through backlog of unprocessed indices.
        int numBacklogGridIndicesProcessed = 0;
        if (!_GoneOverTimeBudget(startTimeMS))
        {
            List<Tango3DReconstruction.GridIndex> processedBacklog = new List<Tango3DReconstruction.GridIndex>();
            foreach (Tango3DReconstruction.GridIndex gridIndex in m_gridUpdateBacklog)
            {
                _UpdateMeshAtGridIndex(gridIndex, needsResize);

                processedBacklog.Add(gridIndex);
                ++numBacklogGridIndicesProcessed;

                if (_GoneOverTimeBudget(startTimeMS))
                {
                    break;
                }
            }

            m_gridUpdateBacklog.ExceptWith(processedBacklog);
        }

        m_debugRemeshingTime = Time.realtimeSinceStartup - (startTimeMS * 0.001f);
        m_debugRemeshingCount = it + numBacklogGridIndicesProcessed;

        // Any leftover grid indices also need to get processed next frame.
        while (it < m_gridIndexToUpdate.Count)
        {
            needsResize.Add(m_gridIndexToUpdate[it]);
            ++it;
        }

        m_gridIndexToUpdate = needsResize;
    }

    /// <summary>
    /// Displays statistics and diagnostics information about the meshing cubes.
    /// </summary>
    public void OnGUI()
    {
        if (!m_enableDebugUI)
        {
            return;
        }

        GUI.color = Color.black;
        string str = string.Format(
            "<size=30>Total Verts/Triangles: {0}/{1} Volumes: {2} UpdateQueue: {3}</size>",
            m_debugTotalVertices, m_debugTotalTriangles, m_meshes.Count, m_gridIndexToUpdate.Count);
        GUI.Label(new Rect(40, 40, 1000, 40), str);

        str = string.Format("<size=30>Remeshing Time: {0:F6} Remeshing Count: {1}</size>",
                            m_debugRemeshingTime, m_debugRemeshingCount);
        GUI.Label(new Rect(40, 80, 1000, 40), str);

        str = string.Format("<size=30>Backlog Size: {0}</size>", m_gridUpdateBacklog.Count);
        GUI.Label(new Rect(40, 120, 1000, 40), str);
    }

    /// <summary>
    /// Called when the 3D reconstruction is dirty.
    /// </summary>
    /// <param name="gridIndexList">List of GridIndex objects that are dirty and should be updated.</param>
    public void OnTango3DReconstructionGridIndicesDirty(List<Tango3DReconstruction.GridIndex> gridIndexList)
    {
        // It's more important to be responsive than to handle all indexes.  Add unprocessed indices to the 
        // backlog and clear the current list if we have fallen behind in processing.
        m_gridUpdateBacklog.UnionWith(m_gridIndexToUpdate);
        m_gridIndexToUpdate.Clear();
        m_gridIndexToUpdate.AddRange(gridIndexList);
    }

    /// <summary>
    /// Clear the dynamic mesh's internal meshes.
    /// 
    /// NOTE: This does not clear the 3D Reconstruction's state.  To do that call TangoApplication.Tango3DRClear().
    /// </summary>
    public void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        m_meshes.Clear();
    }

    /// <summary>
    /// Exports the constructed mesh to a Wavefront OBJ file format. The file will include info
    /// based on the enabled options in TangoApplication.
    /// </summary>
    /// <param name="filepath">Filepath to output the OBJ.</param>
    public void ExportMeshToObj(string filepath)
    {
        AndroidHelper.ShowAndroidToastMessage("Exporting mesh...");

        StringBuilder sb = new StringBuilder();

        int startVertex = 0;

        foreach (TangoSingleDynamicMesh tmesh in m_meshes.Values)
        {
            Mesh mesh = tmesh.m_mesh;
            int meshVertices = 0;

            sb.Append(string.Format("g {0}\n", tmesh.name));

            // Vertices.
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                meshVertices++;

                Vector3 v = tmesh.transform.TransformPoint(mesh.vertices[i]);

                // Include vertex colors as part of vertex point for applications that support it.
                if (mesh.colors32.Length > 0)
                {
                    float r = mesh.colors32[i].r / 255.0f;
                    float g = mesh.colors32[i].g / 255.0f;
                    float b = mesh.colors32[i].b / 255.0f;
                    sb.Append(string.Format("v {0} {1} {2} {3} {4} {5} 1.0\n", v.x, v.y, -v.z, r, g, b));
                }
                else
                {
                    sb.Append(string.Format("v {0} {1} {2} 1.0\n", v.x, v.y, -v.z));
                }
            }

            sb.Append("\n");

            // Normals.
            if (mesh.normals.Length > 0)
            {
                foreach (Vector3 n in mesh.normals)
                {
                    sb.Append(string.Format("vn {0} {1} {2}\n", n.x, n.y, -n.z));
                }

                sb.Append("\n");
            }

            // Texture coordinates.
            if (mesh.uv.Length > 0)
            {
                foreach (Vector3 uv in mesh.uv)
                {
                    sb.Append(string.Format("vt {0} {1}\n", uv.x, uv.y));
                }

                sb.Append("\n");
            }

            // Faces.
            int[] triangles = mesh.triangles;
            for (int j = 0; j < triangles.Length; j += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[j + 2] + 1 + startVertex, triangles[j + 1] + 1 + startVertex, triangles[j] + 1 + startVertex));
            }

            sb.Append("\n");

            startVertex += meshVertices;
        }

        StreamWriter sw = new StreamWriter(filepath);
        sw.AutoFlush = true;
        sw.Write(sb.ToString());

        AndroidHelper.ShowAndroidToastMessage(string.Format("Exported: {0}", filepath));
    }

    /// <summary>
    /// Given a time value indicating when meshing started this frame,
    /// returns a value indicating whether this frame's time budget for meshing has been exceeded.
    /// </summary>
    /// <returns><c>true</c>, if this frame's meshing time budget has been exceeded, <c>false</c> otherwise.</returns>
    /// <param name="startTimeMS">Meshing start timestamp this frame (in milliseconds).</param>
    private bool _GoneOverTimeBudget(int startTimeMS)
    {
        return (Time.realtimeSinceStartup * 1000) - startTimeMS > TIME_BUDGET_MS;
    }

    /// <summary>
    /// Extract and update (or create, if it doesn't exist) the mesh at the given grid index.
    /// </summary>
    /// <param name="gridIndex">Grid index.</param>
    /// <param name="needsResize">List to which indicies needing a future resize will be added.</param>
    private void _UpdateMeshAtGridIndex(Tango3DReconstruction.GridIndex gridIndex, List<Tango3DReconstruction.GridIndex> needsResize)
    {
        TangoSingleDynamicMesh dynamicMesh;
        if (!m_meshes.TryGetValue(gridIndex, out dynamicMesh))
        {
            // build a dynamic mesh as a child of this game object.
            GameObject newObj = new GameObject();
            newObj.transform.parent = transform;
            newObj.name = string.Format("{0},{1},{2}", gridIndex.x, gridIndex.y, gridIndex.z);
            newObj.layer = gameObject.layer;
            dynamicMesh = newObj.AddComponent<TangoSingleDynamicMesh>();
            dynamicMesh.m_vertices = new Vector3[INITIAL_VERTEX_COUNT];
            if (m_tangoApplication.m_3drGenerateTexCoord)
            {
                dynamicMesh.m_uv = new Vector2[INITIAL_VERTEX_COUNT];
            }
            
            if (m_tangoApplication.m_3drGenerateColor)
            {
                dynamicMesh.m_colors = new Color32[INITIAL_VERTEX_COUNT];
            }
            
            dynamicMesh.m_triangles = new int[INITIAL_INDEX_COUNT];
            
            // Update debug info too.
            m_debugTotalVertices = dynamicMesh.m_vertices.Length;
            m_debugTotalTriangles = dynamicMesh.m_triangles.Length;
            
            // Add the other necessary objects
            MeshFilter meshFilter = newObj.AddComponent<MeshFilter>();
            dynamicMesh.m_mesh = meshFilter.mesh;
            
            if (m_meshRenderer != null)
            {
                MeshRenderer meshRenderer = newObj.AddComponent<MeshRenderer>();
                #if UNITY_5
                meshRenderer.shadowCastingMode = m_meshRenderer.shadowCastingMode;
                meshRenderer.receiveShadows = m_meshRenderer.receiveShadows;
                meshRenderer.sharedMaterials = m_meshRenderer.sharedMaterials;
                meshRenderer.useLightProbes = m_meshRenderer.useLightProbes;
                meshRenderer.reflectionProbeUsage = m_meshRenderer.reflectionProbeUsage;
                meshRenderer.probeAnchor = m_meshRenderer.probeAnchor;
                #elif UNITY_4_6
                meshRenderer.castShadows = m_meshRenderer.castShadows;
                meshRenderer.receiveShadows = m_meshRenderer.receiveShadows;
                meshRenderer.sharedMaterials = m_meshRenderer.sharedMaterials;
                meshRenderer.useLightProbes = m_meshRenderer.useLightProbes;
                meshRenderer.lightProbeAnchor = m_meshRenderer.lightProbeAnchor;
                #endif
            }
            
            if (m_meshCollider != null)
            {
                MeshCollider meshCollider = newObj.AddComponent<MeshCollider>();
                meshCollider.convex = m_meshCollider.convex;
                meshCollider.isTrigger = m_meshCollider.isTrigger;
                meshCollider.sharedMaterial = m_meshCollider.sharedMaterial;
                meshCollider.sharedMesh = dynamicMesh.m_mesh;
                dynamicMesh.m_meshCollider = meshCollider;
            }
            
            m_meshes.Add(gridIndex, dynamicMesh);
        }
        
        // Last frame the mesh needed more space.  Give it more room now.
        if (dynamicMesh.m_needsToGrow)
        {
            int newVertexSize = (int)(dynamicMesh.m_vertices.Length * GROWTH_FACTOR);
            int newTriangleSize = (int)(dynamicMesh.m_triangles.Length * GROWTH_FACTOR);
            newTriangleSize -= newTriangleSize % 3;
            
            // Remove the old size, add the new size.
            m_debugTotalVertices += newVertexSize - dynamicMesh.m_vertices.Length;
            m_debugTotalTriangles += newTriangleSize - dynamicMesh.m_triangles.Length;
            
            dynamicMesh.m_vertices = new Vector3[newVertexSize];
            if (m_tangoApplication.m_3drGenerateTexCoord)
            {
                dynamicMesh.m_uv = new Vector2[newVertexSize];
            }
            
            if (m_tangoApplication.m_3drGenerateColor)
            {
                dynamicMesh.m_colors = new Color32[newVertexSize];
            }
            
            dynamicMesh.m_triangles = new int[newTriangleSize];
            dynamicMesh.m_needsToGrow = false;
        }
        
        int numVertices;
        int numTriangles;
        Tango3DReconstruction.Status status = m_tangoApplication.Tango3DRExtractMeshSegment(
            gridIndex, dynamicMesh.m_vertices, null, dynamicMesh.m_colors, dynamicMesh.m_triangles,
            out numVertices, out numTriangles);
        if (status != Tango3DReconstruction.Status.INSUFFICIENT_SPACE
            && status != Tango3DReconstruction.Status.SUCCESS)
        {
            Debug.Log("Tango3DR extraction failed, status code = " + status + Environment.StackTrace);
            return;
        }
        else if (status == Tango3DReconstruction.Status.INSUFFICIENT_SPACE)
        {
            // We already spent the time extracting this mesh, let's not spend any more time this frame
            // to extract the mesh.
            Debug.Log(string.Format(
                "TangoDynamicMesh.Update() extraction ran out of space with room for {0} vertexes, {1} indexes.",
                dynamicMesh.m_vertices.Length, dynamicMesh.m_triangles.Length));
            dynamicMesh.m_needsToGrow = true;
            needsResize.Add(gridIndex);
        }
        
        // Make any leftover triangles degenerate.
        for (int triangleIt = numTriangles * 3; triangleIt < dynamicMesh.m_triangles.Length; ++triangleIt)
        {
            dynamicMesh.m_triangles[triangleIt] = 0;
        }
        
        if (dynamicMesh.m_uv != null)
        {
            // Add texture coordinates.
            for (int vertexIt = 0; vertexIt < numVertices; ++vertexIt)
            {
                Vector3 vertex = dynamicMesh.m_vertices[vertexIt];
                dynamicMesh.m_uv[vertexIt].x = vertex.x * UV_PER_METERS;
                dynamicMesh.m_uv[vertexIt].y = (vertex.z + vertex.y) * UV_PER_METERS;
            }
        }
        
        dynamicMesh.m_mesh.Clear();
        dynamicMesh.m_mesh.vertices = dynamicMesh.m_vertices;
        dynamicMesh.m_mesh.uv = dynamicMesh.m_uv;
        dynamicMesh.m_mesh.colors32 = dynamicMesh.m_colors;
        dynamicMesh.m_mesh.triangles = dynamicMesh.m_triangles;
        if (m_tangoApplication.m_3drGenerateNormal)
        {
            dynamicMesh.m_mesh.RecalculateNormals();
        }
        
        if (dynamicMesh.m_meshCollider != null)
        {
            // Force the mesh collider to update too.
            dynamicMesh.m_meshCollider.sharedMesh = null;
            dynamicMesh.m_meshCollider.sharedMesh = dynamicMesh.m_mesh;
        }
    }

    /// <summary>
    /// Component for a dynamic, resizable mesh.
    /// 
    /// This caches the arrays for vertices, normals, colors, etc. to avoid putting extra pressure on the
    /// garbage collector.
    /// </summary>
    private class TangoSingleDynamicMesh : MonoBehaviour
    {
        /// <summary>
        /// The single mesh.
        /// </summary>
        public Mesh m_mesh = null;

        /// <summary>
        /// If set, the mesh collider for this mesh.
        /// </summary>
        public MeshCollider m_meshCollider = null;

        /// <summary>
        /// If true, then this should grow all arrays at some point in the future.
        /// </summary>
        public bool m_needsToGrow;

        /// <summary>
        /// Cache for Mesh.vertices.
        /// </summary>
        [HideInInspector]
        public Vector3[] m_vertices;

        /// <summary>
        /// Cache for Mesh.uv.
        /// </summary>
        [HideInInspector]
        public Vector2[] m_uv;

        /// <summary>
        /// Cache for Mesh.colors.
        /// </summary>
        [HideInInspector]
        public Color32[] m_colors;

        /// <summary>
        /// Cache to Mesh.triangles.
        /// </summary>
        [HideInInspector]
        public int[] m_triangles;
    }
}
