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
 * DynamicMeshManager
 * This class handles all of the administrative work of inserting points, creating new meshes, 
 * queueing meshes to be regenerated.  Meshing volumes are allocated dynamically in a unit cube grid.
 * When points are inserted into the mesh manager, it creates and updates the appropriate mesh cube.
 * Mesh cubes that are marked dirty, at processed in the queue each frame.  If the user spends a lot 
 * of time in the same space, the number of meshing cubes that need to be updated should slowly approach zero.
 * The mesh geometery is available to any other Unity tool such as hit testing of path planning.
 */
public class DynamicMeshManager : MonoBehaviour {

    /**
     * Prefab that gets instantiated when new cube volumes are needed.
     * It has the DynamicMeshingCube script
     */
    public GameObject m_meshingCubePrefab;

    /**
     * Resolution of the cube meshes.  Specifies divisions per meter.
     */
    public int m_voxelResolution = 10;

    /**
     * The amount of time per frame allowed to be spend on mesh regeneration.
     */
    public float m_meshingTimeBudgetMS = 10;

    /**
     * Keeps track of total vertices in the mesh system.
     */
    private int m_totalVertices = 0;

    /**
     * Keeps track of the total triangles in the system.
     */
    private int m_totalTriangles = 0;

    /**
     * Keeps track of the total points that have been inserted.
     */
    private int m_insertCount = 0;

    /**
     * Keeps track of the total number of meshing cubes created.
     */
    private int m_totalMeshCubes = 0;

    /**
     * Flag to let the update and insertion threads know it is being cleared.
     */
    private bool m_isClearing = false;

    /**
     * Queue for tracking which meshing cubes need to be regenerated.
     */    
    private Queue m_regenerationQueue = new Queue ();

    /**
     * HashTree datastructure for storing the meshing cubes.
     */
    private VolumetricHashTree m_meshStorage = new VolumetricHashTree(null, 0);
    
    /**
     * Keeps track of how long was spent remeshing each frame.
     */
    private float m_remeshingTime = 0;

    /**
     * Limit on the number of meshes that can be remeshed each frame.
     */
    private int m_maximumRemeshingCountPerFrame = 1;

    /**
     * Keeps track of the time spend inserting points each depth frame update
     */
    private float m_pointInsertionTime = 0;

    /**
     * Smoothing variable for FPS-like measurements
     */
    private float m_frameRateSmoothing = 0.97f;

    /**
     * Keeps track of the last time the meshing system receive an Update
     */
    private float m_lastUpdateTime = 0;

    /**
     * Keeps track of the number of rendering frames updated.
     */
    private int m_frameCount = 0;

    /**
     * Handle for the main camera, primarily to set position when in dataset playback.
     */
    public Camera m_mainCamera;

    /**
     * Storage of the voxels that are intersected with a raycast test.
     */
    List<Voxel> m_raycastHits =  null;
    
    /**
     * Flag for raycast development testing.
     */
    public bool m_raycastTesting;

    /**
     * Testing for raycast development.
     */
    Vector3 m_raycastStart;

    /**
     * Testing for raycast development.
     */
    Vector3 m_raycastStop;


    float m_meshingStart = 0;
    float m_meshingStop = 0;

    /**
     * Used for initialization.
     */
    void Start () {

    }

    /**
     * Gets the time smoothing parameter.
     */
    public float TimeSmoothing {
        get {
            return m_frameRateSmoothing;
        }
        set {
            m_frameRateSmoothing = value;
        }
    }

    /**
     * Gets the point insertion time.
     */
    public float InsertionTime {
        get {
            return m_pointInsertionTime;
        }
        set {
            m_pointInsertionTime = value;
        }
    }

    /**
     * Insert a point into the meshing volumes.
     * @param p the 3D point to be inserted
     * @param obs the direction of the observation vector from the camera toward the point
     * @param w weight of the observation
     */
    public void InsertPoint(Vector3 p, Vector3 obs, float weight) {
        if (m_isClearing)
            return;
        m_meshStorage.InsertPoint(p,obs,weight, m_meshingCubePrefab, transform, m_voxelResolution);
        m_insertCount++;
    }

    /**
     * Searches for meshing cubes that have been mark dirty and adds them to the queue for remeshing.
     */
    public void QueueDirtyMeshesForRegeneration() {
        if (m_isClearing)
            return;

        //enque dirty meshes
        int count = 0;
        foreach (VolumetricHashTree o in m_meshStorage.GetEnumerable()) {
            count += 1;
            if(o.DynamicMeshCube == null)
                continue;

            if(o.DynamicMeshCube.IsDirty) {
                if(!m_regenerationQueue.Contains(o.DynamicMeshCube))
                    m_regenerationQueue.Enqueue(o.DynamicMeshCube);
            }
        }
    }

    /**
     * Print out debug information for each of the meshing cubes.
     */
    public void PrintDebugInfo() {
        foreach (VolumetricHashTree o in m_meshStorage.GetEnumerable()) {
            if(o.DynamicMeshCube == null)
                continue;
            o.DynamicMeshCube.PrintDebugInfo();
        }
    }

    /**
     * Recompute statistics about the meshing cubes.
     */
    public void UpdateStats() {
        m_totalVertices = 0;
        m_totalTriangles = 0;
        m_totalMeshCubes = 0;
        m_meshStorage.ComputeStats (ref m_totalVertices, ref m_totalTriangles, ref m_totalMeshCubes);
    }


    /**
     * Clears all meshing data.
     */
    public void Clear() {
        //i think this causes a thread contention because we are updating the mesh in the Update call
        m_isClearing = true;
        m_insertCount = 0;
        m_regenerationQueue.Clear ();
        m_meshStorage.Clear ();    
        m_isClearing = false;
    }

    /**
     * Displays statistics and diagnostics information about the meshing cubes
     */
    void OnGUI()
    {
        GUI.Label(new Rect(10,20,1000,30), "Persistent Path: " + Application.persistentDataPath);
        GUI.Label(new Rect(10,40,1000,30), "Total Verts/Triangles: " + m_totalVertices + "/" + m_totalTriangles + " Volumes: " + m_totalMeshCubes + " UpdateQueue:" + m_regenerationQueue.Count);
        GUI.Label(new Rect(10,60,1000,30), "Insert Count: " + m_insertCount);
        GUI.Label(new Rect(10,80,1000,30), "RemeshingTime: " + m_remeshingTime.ToString("F6") + " Remeshing Count: " + m_maximumRemeshingCountPerFrame);
        GUI.Label(new Rect(10,100,1000,30), "InsertionTime: " + m_pointInsertionTime.ToString("F6"));
        GUI.Label(new Rect(10,120,1000,30), "Last Update Time: " + m_lastUpdateTime.ToString("F6"));
        GUI.Label(new Rect(10,140,1000,30), "Version: " + "15.06.05");

        if (GUI.Button (new Rect (Screen.width - 160, 20, 140, 80), "Clear")) {
            Clear();
        }
    }
    
    /**
     * Update is called once per frame and progressively remeshes volumes that are in the queue.
     */
    void Update () {
        m_frameCount++;


        int meshUpdateCount = 0;

        //update 1 cell in the top of queue
        if (m_maximumRemeshingCountPerFrame < 1)
            m_maximumRemeshingCountPerFrame = 1;
        if (m_maximumRemeshingCountPerFrame > 10)
            m_maximumRemeshingCountPerFrame = 10;

        {
            for(int i = 0; i < m_maximumRemeshingCountPerFrame; i++) {
                if(m_regenerationQueue.Count == 0) {
                    if((m_meshingStart != 0)&&(m_meshingStop == 0)) {
                        m_meshingStop = UnityEngine.Time.realtimeSinceStartup;
                        Debug.Log("Meshing time: " + (m_meshingStop - m_meshingStart)); 
                    }
                    break;
                }

                if(m_meshingStart == 0)
                    m_meshingStart = UnityEngine.Time.realtimeSinceStartup;

                float start = Time.realtimeSinceStartup;
                ((DynamicMeshCube)m_regenerationQueue.Dequeue ()).RegenerateMesh();
                float stop = Time.realtimeSinceStartup;
                m_remeshingTime = m_frameRateSmoothing*m_remeshingTime + (1.0f-m_frameRateSmoothing)*(stop - start);
                meshUpdateCount++;
            }
            if(m_remeshingTime > float.Epsilon)
                m_maximumRemeshingCountPerFrame = (int)(m_meshingTimeBudgetMS*0.001f / m_remeshingTime);
            UpdateStats ();
        }

        if (m_raycastTesting) {
            m_raycastStart = m_mainCamera.transform.position;
            m_raycastStop = m_mainCamera.transform.position + m_mainCamera.transform.forward*5;

            m_raycastHits = m_meshStorage.RaycastVoxelHitlist(m_raycastStart, m_raycastStop);

            if(m_raycastHits == null) {
                Debug.Log("Error Dynamic Mesh - Raycast returned null");
            }

            if(m_raycastHits.Count == 0) {
                Debug.DrawLine(m_raycastStart, m_raycastStop,Color.red);
            }else {
                foreach(Voxel v in m_raycastHits) {
                    Vector3 voxelSize = new Vector3(v.size,v.size,v.size)/2;
                    Vector3 min = v.anchor + v.parent.position - voxelSize;
                    Vector3 max = v.anchor + v.parent.position + voxelSize;
                    DebugDrawing.Box(min, max,Color.green);
                }
                Debug.DrawLine(m_raycastStart, m_raycastStop,Color.green);
            } 
        }

        if (Input.GetKeyDown (KeyCode.C))
            Clear ();

    }
}
