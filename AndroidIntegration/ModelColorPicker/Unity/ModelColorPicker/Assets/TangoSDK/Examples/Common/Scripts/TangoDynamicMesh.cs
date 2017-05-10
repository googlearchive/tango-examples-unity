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
    /// If set, grid indices will stop meshing when they have been sufficiently observed.
    /// </summary>
    public bool m_enableSelectiveMeshing = false;

    /// <summary>
    /// Notifies about newly allocated Mesh Segments. The update delegate is not called for the same Mesh Segment.
    /// The created Mesh Segment and its index are passed to the hander.
    /// </summary>
    public MeshSegmentDelegate m_meshSegmentCreated;

    /// <summary>
    /// Notifies when a Mesh Segment is updated. The updated Mesh Segment and its index are passed to the hander.
    /// </summary>
    public MeshSegmentDelegate m_meshSegmentUpdated;

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
    /// Used for selective meshing, number of sufficient observations necessary to consider a grid index as complete.
    /// </summary>
    private const int NUM_OBSERVATIONS_TO_COMPLETE = 25;

    /// <summary>
    /// Used for selective meshing, byte representation of the completed observation directions.
    /// 
    /// The mesh needs to be observed every 90 degrees around the mesh to be completed for a total of 4 directions.
    /// The first 4 bits must be on, i.e. 00001111.
    /// </summary>
    private const byte DIRECTIONS_COMPLETE = 15;

    /// <summary>
    /// Used for selective meshing, the minimum value of the dot product between the camera forward direction and a
    /// grid index's direction check. 
    /// 
    /// If the calculated dot product meets this value, the grid index is considered to have been viewed from the given direction.
    /// </summary>
    private readonly float m_minDirectionCheck = Mathf.Cos(Mathf.PI / 4);

    /// <summary>
    /// The TangoApplication for the scene.
    /// </summary>
    private ITangoApplication m_tangoApplication;

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
    /// The list of grid index configuration sets (each represented as a list of grid indices) to
    /// be checked for observation count during selective meshing.
    /// </summary>
    private Vector3[][] m_gridIndexConfigs;

    /// <summary>
    /// The bounding box of the mesh.
    /// </summary>
    private Bounds m_bounds;

    /// <summary>
    /// Constructor of TangoDynamicMesh.
    /// </summary>
    public TangoDynamicMesh()
    {
        m_meshes = new Dictionary<Tango3DReconstruction.GridIndex, TangoSingleDynamicMesh>(100);
        m_gridIndexToUpdate = new List<Tango3DReconstruction.GridIndex>(100);
        m_gridUpdateBacklog = new HashSet<Tango3DReconstruction.GridIndex>();
    }

    /// <summary>
    /// Delegate for a method that processes a single Mesh Segment. Mesh Segments are spacially disjoint Game Objects 
    /// that contain meshes, renderes and colliders. TangoDynamicMesh generates and manages multiple Mesh Segments as 
    /// children. See <url>https://developers.google.com/tango/apis/unity/unity-howto-mesh-color#mesh_segments</url>.
    /// </summary>
    /// <param name="meshSegment">Game Object that contains a part of Tango Dynamic Mesh.</param>
    /// <param name="index">3D integer index of the Mesh Segment.</param>
    public delegate void MeshSegmentDelegate(GameObject meshSegment, Tango3DReconstruction.GridIndex index);

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

        if (m_enableSelectiveMeshing)
        {
            _InitGridIndexConfigs();
        }
    }

    /// <summary>
    /// Unity destroy function.
    /// </summary>
    public void OnDestroy()
    {
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Unregister(this);
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
    /// Called when the 3D Reconstruction is dirty.
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
    /// Exports the constructed mesh to an OBJ file format. The file will include info
    /// based on the enabled options in TangoApplication.
    /// </summary>
    /// <param name="filepath">File path to output the OBJ.</param>
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
                int v1 = triangles[j + 2] + 1 + startVertex;
                int v2 = triangles[j + 1] + 1 + startVertex;
                int v3 = triangles[j] + 1 + startVertex;

                // Filter out single vertex index triangles which cause Maya to not be able to
                // import the mesh.
                if (v1 != v2 || v2 != v3)
                {
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", v1, v2, v3));
                }
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
    /// Gets each single dynamic mesh and fills out arrays with properties. Each mesh corresponds to the same index in each array.
    /// </summary>
    /// <param name="gridIndices">Filled out with grid index of each mesh.</param>
    /// <param name="completed">Filled out with completion state of each mesh.</param>
    /// <param name="completionScale">Filled out with amount that each mesh has been completed.</param>
    /// <param name="directions">Filled out with a byte representation of the observed directions of each mesh.</param>
    public void GetSingleMeshProperties(out Tango3DReconstruction.GridIndex[] gridIndices, out bool[] completed, out float[] completionScale, out byte[] directions)
    {
        int numIndices = m_meshes.Count;
        gridIndices = new Tango3DReconstruction.GridIndex[numIndices];
        completed = new bool[numIndices];
        completionScale = new float[numIndices];
        directions = new byte[numIndices];

        // Assign mesh properties to each index of the arrays.
        m_meshes.Keys.CopyTo(gridIndices, 0);
        for (int i = 0; i < numIndices; i++)
        {
            TangoSingleDynamicMesh mesh = m_meshes[gridIndices[i]];
            completed[i] = mesh.m_completed;
            completionScale[i] = 1.0f * mesh.m_observations / NUM_OBSERVATIONS_TO_COMPLETE;
            directions[i] = mesh.m_directions;
        }
    }

    /// <summary>
    /// Gets the highest point on the dynamic mesh through at a given position.
    /// 
    /// Raycast against a subset of TangoSingleDynamicMesh colliders and find the highest point. The subset
    /// is defined by all the meshes intersected by a downward-pointing ray that passes through a position.
    /// </summary>
    /// <returns>The highest raycast hit point.</returns>
    /// <param name="position">The position to cast a ray through.</param>
    /// <param name="maxDistance">The max distance of the ray.</param>
    public Vector3 GetHighestRaycastHitPoint(Vector3 position, float maxDistance)
    {
        if (GetComponent<Collider>() == null)
        {
            return position;
        }

        Vector3 topHitPoint = position;
        Ray ray = new Ray(position + (Vector3.up * (maxDistance / 2)), Vector3.down);

        // Find the starting grid index X and Y components.
        float gridIndexSize = m_tangoApplication.ReconstructionMeshResolution * 16;
        int gridIndexX = Mathf.FloorToInt(position.x / gridIndexSize);
        int gridIndexY = Mathf.FloorToInt(position.z / gridIndexSize);

        // Find the top and bottom grid indices that are overlapped by a raycast downward through the position.
        int topZ = Mathf.FloorToInt(ray.origin.y / gridIndexSize);
        int btmZ = Mathf.FloorToInt((ray.origin.y - maxDistance) / gridIndexSize);

        // Perform a raycast on each TangoSingleDynamicMesh collider the ray passes through.
        for (int i = btmZ; i <= topZ; i++)
        {
            Tango3DReconstruction.GridIndex newGridIndex = new Tango3DReconstruction.GridIndex();
            newGridIndex.x = gridIndexX;
            newGridIndex.y = gridIndexY;
            newGridIndex.z = i;

            // Find the mesh associated with the grid index if available. Raycast to the attached collider.
            TangoSingleDynamicMesh singleDynamicMesh;
            if (m_meshes.TryGetValue(newGridIndex, out singleDynamicMesh))
            {
                Collider c = singleDynamicMesh.GetComponent<Collider>();
                RaycastHit hit;
                if (c.Raycast(ray, out hit, maxDistance))
                {
                    // Update the highest position if the new raycast hit is above. Reject the hit if the normal is orthogonal to the up 
                    // direction (to prevent the object from unintentionally climbing up walls).
                    if ((hit.point.y > topHitPoint.y) && (Vector3.Dot(hit.normal, Vector3.up) > 0.1f))
                    {
                        topHitPoint = hit.point;
                    }
                }
            }
        }

        return topHitPoint;
    }

    /// <summary>
    /// Gets the bounds of the mesh.
    /// </summary>
    /// <returns>The bounds.</returns>
    public Bounds GetBounds()
    {
        return m_bounds;
    }

    /// <summary>
    /// Calls a provided function once for each currently existing Mesh Segment.
    /// </summary>
    /// <param name="callback">The method to be called for each Mesh Segment.</param>
    public void ForEachMeshSegment(MeshSegmentDelegate callback)
    {
        foreach (var mesh in m_meshes)
        {
            callback(mesh.Value.gameObject, mesh.Key);
        }
    }

    /// <summary>
    /// Set concrete instance of ITangoApplication. Unregisters from the current ITangoApplication if such exists.
    /// </summary>
    /// <param name="tangoApplication">The instance of ITangoApplication to use.</param>
    internal void SetTangoApplication(ITangoApplication tangoApplication)
    {
        if (tangoApplication == null)
        {
            Debug.LogError("[TangoDynamicMesh] Cannot set Aango application to null.");
            return;
        }

        if (m_tangoApplication != null)
        {
            m_tangoApplication.Unregister(this);
        }

        m_tangoApplication = tangoApplication;

        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);
        }
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
    /// <param name="needsResize">List to which indices needing a future resize will be added.</param>
    private void _UpdateMeshAtGridIndex(Tango3DReconstruction.GridIndex gridIndex, List<Tango3DReconstruction.GridIndex> needsResize)
    {
        TangoSingleDynamicMesh dynamicMesh;
        bool createNewMeshSegment = !m_meshes.TryGetValue(gridIndex, out dynamicMesh);
        if (createNewMeshSegment)
        {
            // build a dynamic mesh as a child of this game object.
            GameObject newObj = new GameObject();
            newObj.transform.parent = transform;
            newObj.name = string.Format("{0},{1},{2}", gridIndex.x, gridIndex.y, gridIndex.z);
            newObj.layer = gameObject.layer;
            dynamicMesh = newObj.AddComponent<TangoSingleDynamicMesh>();
            dynamicMesh.m_vertices = new Vector3[INITIAL_VERTEX_COUNT];
            if (m_tangoApplication.Enable3DReconstructionTexCoords)
            {
                dynamicMesh.m_uv = new Vector2[INITIAL_VERTEX_COUNT];
            }
            
            if (m_tangoApplication.Enable3DReconstructionColors)
            {
                dynamicMesh.m_colors = new Color32[INITIAL_VERTEX_COUNT];
            }
            
            dynamicMesh.m_triangles = new int[INITIAL_INDEX_COUNT];
            
            // Update debug info too.
            m_debugTotalVertices = dynamicMesh.m_vertices.Length;
            m_debugTotalTriangles = dynamicMesh.m_triangles.Length;
            
            // Add the other necessary objects
            MeshFilter meshFilter = newObj.AddComponent<MeshFilter>();
            if (Application.isPlaying)
            {
                dynamicMesh.m_mesh = meshFilter.mesh;
            }
            else
            {
                // When running tests in Unity Editor, instantiate meshes explicitly to avoid Unity errors.
                dynamicMesh.m_mesh = new Mesh();
                meshFilter.mesh = dynamicMesh.m_mesh;
            }

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
            _UpdateBounds(gridIndex);
        }

        // Skip updating this grid index if it is considered completed.
        if (m_enableSelectiveMeshing) 
        {
            if (dynamicMesh.m_completed)
            {
                return;
            }

            _ObserveGridIndex(gridIndex, dynamicMesh);
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
            if (m_tangoApplication.Enable3DReconstructionTexCoords)
            {
                dynamicMesh.m_uv = new Vector2[newVertexSize];
            }
            
            if (m_tangoApplication.Enable3DReconstructionColors)
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
        if (m_tangoApplication.Enable3DReconstructionNormals)
        {
            dynamicMesh.m_mesh.RecalculateNormals();
        }
        
        if (dynamicMesh.m_meshCollider != null)
        {
            // Force the mesh collider to update too.
            dynamicMesh.m_meshCollider.sharedMesh = null;
            dynamicMesh.m_meshCollider.sharedMesh = dynamicMesh.m_mesh;
        }

        if (createNewMeshSegment)
        {
            if (m_meshSegmentCreated != null)
            {
                m_meshSegmentCreated(dynamicMesh.gameObject, gridIndex);
            }
        }
        else
        {
            if (m_meshSegmentUpdated != null)
            {
                m_meshSegmentUpdated(dynamicMesh.gameObject, gridIndex);
            }
        }
    }

    /// <summary>
    /// When the grid index has been updated, also determine whether it should be considered completed
    /// based on its neighboring grid indices, number of observations, and mesh completeness.
    /// 
    /// When checking a grid index for completeness, the observation count of neighboring grid indices is checked.
    /// If all grid indices contained in one of the configurations have a sufficient number of observations,
    /// the grid index is considered complete.
    /// </summary>
    /// <param name="gridIndex">Grid index to observe.</param>
    /// <param name="singleMesh">TangoSingleDynamicMesh to update and observe.</param>
    private void _ObserveGridIndex(Tango3DReconstruction.GridIndex gridIndex, TangoSingleDynamicMesh singleMesh)
    {
        // Increment the observations made for this grid index.
        singleMesh.m_observations++;

        // Add observation based on the direction of the observation.
        _ViewGridIndex(gridIndex);

        // Exit if the grid index has not been observed from all 8 directions.
        if (singleMesh.m_directions != DIRECTIONS_COMPLETE)
        {
            return;
        }

        // Run through each grid index configuration and check if the grid index is complete.
        for (int i = 0; i < m_gridIndexConfigs.Length; i++)
        {
            Vector3[] config = m_gridIndexConfigs[i];
            bool neighborsObserved = true;
            foreach (Vector3 nPosition in config)
            {
                Tango3DReconstruction.GridIndex neighbor = new Tango3DReconstruction.GridIndex();
                neighbor.x = (Int32)(nPosition.x + gridIndex.x);
                neighbor.y = (Int32)(nPosition.y + gridIndex.y);
                neighbor.z = (Int32)(nPosition.z + gridIndex.z);
                TangoSingleDynamicMesh nSingleMesh;
                if (m_meshes.TryGetValue(neighbor, out nSingleMesh))
                {
                    if (nSingleMesh.m_observations < NUM_OBSERVATIONS_TO_COMPLETE)
                    {
                        neighborsObserved = false;
                        break;
                    }
                }
            }

            // Complete using this configurations of the neighbors with sufficient observations.
            if (neighborsObserved)
            {
                // Add the grid index to the completed list, so it will be skipped during next mesh update.
                singleMesh.m_completed = true;
                return;
            }
        }
    }

    /// <summary>
    /// Initialize the sets of grid index neighbor configurations to check when performing selective meshing.
    /// </summary>
    private void _InitGridIndexConfigs()
    {
        // Grid indices use the Right Hand Local Level coordinate system. The diagrams below show which grid
        // indices are checked in each configuration for sufficient observations. The following layouts show 
        // each config represented as 3x3 Vector3 matrices, separated into z-planes.
        // (-1,1,0)  (0,1,0)  (1,1,0)
        // (-1,0,0)  (0,0,0)  (1,0,0)
        // (-1,-1,0) (0,-1,0) (1,-1,0)
        // 'x' is a grid index that is checked, '-' is not checked.

        // Wall-Corner-Floor configuration.
        // z = 0    z = 1
        // - x x    - x -
        // x x x    x x -
        // x x x    - - -
        Vector3[] wallCornerFloor = new Vector3[]
        {
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(-1, -1, 0),
            new Vector3(0, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(0, 1, 1),
            new Vector3(-1, 0, 1),
            new Vector3(0, 0, 1),
        };

        // Wall-Floor configuration.
        // z = 0    z = 1
        // - - -    - - -
        // x x x    x x x
        // x x x    - - -
        Vector3[] wallFloor = new Vector3[]
        {
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(-1, -1, 0),
            new Vector3(0, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(-1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1),
        };

        // Wall-Corner configuration.
        // z = -1  z = 0   z = 1
        // - x -   - x -   - x -
        // x x -   x x -   x x -
        // - - -   - - -   - - -
        Vector3[] wallCorner = new Vector3[]
        {
            new Vector3(0, 1, -1),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 1, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 1),
            new Vector3(-1, 0, 1),
            new Vector3(0, 0, 1),
        };

        // Wall configuration.
        // z = -1  z = 0   z = 1
        // - - -   - - -   - - -
        // x x x   x x x   x x x
        // - - -   - - -   - - -
        Vector3[] wall = new Vector3[]
        {
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, -1),
        };

        // Floor configuration.
        // z = 0
        // x x x
        // x x x
        // x x x
        Vector3[] floor = new Vector3[]
        {
            new Vector3(-1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(-1, -1, 0),
            new Vector3(0, -1, 0),
            new Vector3(1, -1, 0),
        };

        // Rotate each configuration for different orientations and add to the list of configs to check for completeness.
        m_gridIndexConfigs = new Vector3[15][];
        m_gridIndexConfigs[0] = wallCornerFloor;
        m_gridIndexConfigs[1] = _GetRotatedGridIndexConfig(wallCornerFloor, 90);
        m_gridIndexConfigs[2] = _GetRotatedGridIndexConfig(wallCornerFloor, 180);
        m_gridIndexConfigs[3] = _GetRotatedGridIndexConfig(wallCornerFloor, 270);
        m_gridIndexConfigs[4] = wallFloor;
        m_gridIndexConfigs[5] = _GetRotatedGridIndexConfig(wallFloor, 90);
        m_gridIndexConfigs[6] = _GetRotatedGridIndexConfig(wallFloor, 180);
        m_gridIndexConfigs[7] = _GetRotatedGridIndexConfig(wallFloor, 270);
        m_gridIndexConfigs[8] = wallCorner;
        m_gridIndexConfigs[9] = _GetRotatedGridIndexConfig(wallCorner, 90);
        m_gridIndexConfigs[10] = _GetRotatedGridIndexConfig(wallCorner, 180);
        m_gridIndexConfigs[11] = _GetRotatedGridIndexConfig(wallCorner, 270);
        m_gridIndexConfigs[12] = wall;
        m_gridIndexConfigs[13] = _GetRotatedGridIndexConfig(wall, 90);
        m_gridIndexConfigs[14] = floor;
    }

    /// <summary>
    /// Helper function to get a copy of the grid index config after it has been rotated around the z-axis.
    /// </summary>
    /// <returns>The rotated grid index config.</returns>
    /// <param name="config">List of grid indices in the config.</param>
    /// <param name="zRotation">Amount of rotation to apply around the z-axis.</param>
    private Vector3[] _GetRotatedGridIndexConfig(Vector3[] config, float zRotation)
    {
        Vector3[] finalConfig = new Vector3[config.Length];
        for (int j = 0; j < config.Length; j++)
        {
            finalConfig[j] = Quaternion.Euler(0, 0, zRotation) * config[j];
        }

        return finalConfig;
    }

    /// <summary>
    /// Set the grid index as having been observed from the given direction.
    /// </summary>
    /// <param name="gridIndex">Grid index to observe.</param>
    private void _ViewGridIndex(Tango3DReconstruction.GridIndex gridIndex)
    {
        // This update may occur somewhat later than the actual time of the camera pose observation.
        Vector3 dir = Camera.main.transform.forward;
        dir = new Vector3(dir.x, 0.0f, dir.z).normalized;
        Vector3[] directions = new Vector3[]
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0)
        };
        
        for (int i = 0; i < 4; i++)
        {
            // If the camera is facing one of 4 directions (every 90 degrees) within a 45 degree spread,
            // set that direction as seen.
            float dot = Vector3.Dot(dir, directions[i]);
            if (dot > m_minDirectionCheck)
            {
                // Bitwise OR new and old directions to show that the mesh has been observed from the new direction.
                byte direction = (byte)(1 << i);
                m_meshes[gridIndex].m_directions = (byte)(m_meshes[gridIndex].m_directions | direction);
                break;
            }
        }
    }

    /// <summary>
    /// Update the bounding box.
    /// </summary>
    /// <param name="gridIndex">Grid index to include in bounds.</param>
    private void _UpdateBounds(Tango3DReconstruction.GridIndex gridIndex)
    {
        float gridIndexSize = m_tangoApplication.ReconstructionMeshResolution * 16;
        Vector3 pointToCompare = gridIndexSize * new Vector3(gridIndex.x, gridIndex.y, gridIndex.z);

        Vector3 min = m_bounds.min;
        Vector3 max = m_bounds.max;

        if (m_bounds.min.x > pointToCompare.x)
        {
            min.x = pointToCompare.x;
        }

        if (m_bounds.min.y > pointToCompare.y)
        {
            min.y = pointToCompare.y;
        }

        if (m_bounds.min.z > pointToCompare.z)
        {
            min.z = pointToCompare.z;
        }

        if (m_bounds.max.x < pointToCompare.x)
        {
            max.x = pointToCompare.x;
        }

        if (m_bounds.max.y < pointToCompare.y)
        {
            max.y = pointToCompare.y;
        }

        if (m_bounds.max.z < pointToCompare.z)
        {
            max.z = pointToCompare.z;
        }

        m_bounds.SetMinMax(min, max);
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
        /// Cache for <c>Mesh.vertices</c>.
        /// </summary>
        [HideInInspector]
        public Vector3[] m_vertices;

        /// <summary>
        /// Cache for <c>Mesh.uv</c>.
        /// </summary>
        [HideInInspector]
        public Vector2[] m_uv;

        /// <summary>
        /// Cache for <c>Mesh.colors</c>.
        /// </summary>
        [HideInInspector]
        public Color32[] m_colors;

        /// <summary>
        /// Cache to <c>Mesh.triangles</c>.
        /// </summary>
        [HideInInspector]
        public int[] m_triangles;

        /// <summary>
        /// Set as <c>true</c> if the grid index is considered complete.
        /// </summary>
        [NonSerialized]
        public bool m_completed;
        
        /// <summary>
        /// The number of times the grid index has been observed.
        /// </summary>
        [NonSerialized]
        public int m_observations = 1;
        
        /// <summary>
        /// The directions from which that the grid index has been observed.
        /// </summary>
        [NonSerialized]
        public byte m_directions;
    }
}
