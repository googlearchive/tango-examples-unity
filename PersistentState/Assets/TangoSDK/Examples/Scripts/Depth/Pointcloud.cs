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
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using UnityEngine;
using Tango;

/// <summary>
/// Point cloud visualize using depth frame API.
/// </summary>
public class Pointcloud : DepthListener
{
    public SampleController m_poseController;
    [HideInInspector]
    public float m_overallZ = 0.0f;
    [HideInInspector]
    public int m_pointsCount = 0;

    // Matrices for transforming pointcloud to world coordinates.
    private Matrix4x4 m_deviceToStartServiceMatrix = new Matrix4x4 ();
    private Matrix4x4 m_startServiceToUnityWorldMatrix = new Matrix4x4 ();
    private Matrix4x4 m_unityCameraToCameraMatrix = new Matrix4x4 ();

    // Some const value.
    private const int DEPTH_BUFFER_WIDTH = 320;
    private const int DEPTH_BUFFER_HEIGHT = 180;
    private const float MILLIMETER_TO_METER = 0.001f;
    private const float INCH_TO_METER = 0.0254f;
    private const int VERT_COUNT = 61440;
    private const int FOCUS_LENGTH = 312;//half of 624.
    
    // m_vertices will be assigned to this mesh.
    private Mesh m_mesh;
    private MeshCollider m_meshCollider;
    
    // Mesh data.
    private Vector3[] m_vertices;
    private int[] m_triangles;
    private bool m_isDirty;
    private double m_timeSinceLastDepthFrame = 0.0;
    private int m_numberOfDepthSamples = 0;
    private double m_previousDepthDeltaTime = 0.0;

	private TangoApplication m_tangoApplication;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start() 
    {
        m_startServiceToUnityWorldMatrix.SetColumn (0, new Vector4 (1.0f, 0.0f, 0.0f, 0.0f));
        m_startServiceToUnityWorldMatrix.SetColumn (1, new Vector4 (0.0f, 0.0f, 1.0f, 0.0f));
        m_startServiceToUnityWorldMatrix.SetColumn (2, new Vector4 (0.0f, 1.0f, 0.0f, 0.0f));
        m_startServiceToUnityWorldMatrix.SetColumn (3, new Vector4 (0.0f, 0.0f, 0.0f, 1.0f));

        m_unityCameraToCameraMatrix.SetColumn (0, new Vector4 (1.0f, 0.0f, 0.0f, 0.0f));
        m_unityCameraToCameraMatrix.SetColumn (1, new Vector4 (0.0f, 1.0f, 0.0f, 0.0f));
        m_unityCameraToCameraMatrix.SetColumn (2, new Vector4 (0.0f, 0.0f, -1.0f, 0.0f));
        m_unityCameraToCameraMatrix.SetColumn (3, new Vector4 (0.0f, 0.0f, 0.0f, 1.0f));

        // get the reference of mesh
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null) 
        {
            MeshFilter meshFilter = (MeshFilter)gameObject.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = m_mesh = new Mesh();
            MeshRenderer renderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material.shader = Shader.Find("Tango/PointCloud");
        } 
        else 
        {
            m_mesh = mf.mesh;
        }
        m_isDirty = false;
        _CreateMesh();
        transform.localScale = new Vector3(transform.localScale.x,
                                           transform.localScale.y,
                                           transform.localScale.z);

		m_tangoApplication = FindObjectOfType<TangoApplication>();
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update() 
    {
        if (m_isDirty)
        {
            // Query pose to transform point cloud to world coordinates.
            TangoPoseData poseData = new TangoPoseData();
            TangoCoordinateFramePair pair;
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            PoseProvider.GetPoseAtTime(poseData, m_previousDepthDeltaTime, pair);
            Vector3 position = new Vector3((float)poseData.translation[0], (float)poseData.translation[1], (float)poseData.translation[2]);
            Quaternion quat = new Quaternion((float)poseData.orientation[0], (float)poseData.orientation[1], (float)poseData.orientation[2], (float)poseData.orientation[3]);
            m_deviceToStartServiceMatrix = Matrix4x4.TRS(position, quat, new Vector3 (1.0f, 1.0f, 1.0f));

            Matrix4x4 pointcloudTRS = m_startServiceToUnityWorldMatrix * m_deviceToStartServiceMatrix * 
                                      Matrix4x4.Inverse(m_poseController.m_deviceToIMUMatrix) * m_poseController.m_cameraToIMUMatrix * m_unityCameraToCameraMatrix;

            renderer.material.SetMatrix ("local_transformation", pointcloudTRS);

            // Update the point cloud.
            _UpdateMesh();
            m_isDirty = false;

        }
    }

    /// <summary>
    /// Gets the time since last depth data collection.
    /// </summary>
    /// <returns>The time since last frame.</returns>
    public double GetTimeSinceLastFrame()
    {
        return (m_timeSinceLastDepthFrame * 1000.0);
    }

    /// <summary>
    /// Callback that gets called when depth is available
    /// from the Tango Service.
    /// DO NOT USE THE UNITY API FROM INSIDE THIS FUNCTION!
    /// </summary>
    /// <param name="callbackContext">Callback context.</param>
    /// <param name="xyzij">Xyzij.</param>
    protected override void _OnDepthAvailable(IntPtr callbackContext, TangoXYZij xyzij)
    {
        // Calculate the time since the last successful depth data
        // collection.
        if (m_previousDepthDeltaTime == 0.0)
        {
            m_previousDepthDeltaTime = xyzij.timestamp;
        }
        else
        {
            m_numberOfDepthSamples++;
            m_timeSinceLastDepthFrame = xyzij.timestamp - m_previousDepthDeltaTime;
            m_previousDepthDeltaTime = xyzij.timestamp;
        }

        // Fill in the data to draw the point cloud.
        if (xyzij != null && m_vertices != null)
        {
            int numberOfActiveVertices = xyzij.xyz_count;
            m_pointsCount = numberOfActiveVertices;

            if(numberOfActiveVertices > 0)
            {
                float[] allPositions = new float[numberOfActiveVertices * 3];
                Marshal.Copy(xyzij.xyz[0], allPositions, 0, allPositions.Length);
                
                for(int i = 0; i < m_vertices.Length; ++i)
                {
                    if( i < xyzij.xyz_count )
                    {
                        m_vertices[i].x = allPositions[i * 3];
                        m_vertices[i].y = allPositions[(i * 3) + 1];
                        m_vertices[i].z = allPositions[(i * 3) + 2];
                    }
                    else
                    {
                        m_vertices[i].x = m_vertices[i].y = m_vertices[i].z = 0.0f;
                    }
                }
                m_isDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Create the mesh to visualize the point cloud
    /// data.
    /// </summary>
    private void _CreateMesh()
    {
        m_vertices = new Vector3[VERT_COUNT];
        m_triangles = new int[VERT_COUNT];
        // Assign triangles, note: this is just for visualizing point in the mesh data.
        for (int i = 0; i < VERT_COUNT; i++)
        {
            m_triangles[i] = i;
        }

        m_mesh.Clear();
        m_mesh.vertices = m_vertices;
        m_mesh.triangles = m_triangles;
        m_mesh.RecalculateBounds();
        m_mesh.RecalculateNormals();
    }

    /// <summary>
    /// Update the mesh m_vertices and m_triangles.
    /// </summary>
    private void _UpdateMesh()
    {
        _UpdateMeshFromGetPointcloud();

        // update the m_vertices
        m_mesh.Clear();
        m_mesh.vertices = m_vertices;
        m_mesh.triangles = m_triangles;
        m_mesh.SetIndices(m_triangles, MeshTopology.Points, 0);

        // Here we make the bounding box occupies the whole camera render frustum to
        // avoid fustum culling.
        Transform camTransform = Camera.main.transform;
        float distToCenter = (Camera.main.farClipPlane - Camera.main.nearClipPlane) / 2.0f;
        Vector3 center = camTransform.position + camTransform.forward * distToCenter;
        float extremeBound = 500.0f;
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.sharedMesh.bounds = new Bounds (center, Vector3.one * extremeBound);
    }

    /// <summary>
    /// Update the mesh.
    /// </summary>
    private void _UpdateMeshFromGetPointcloud()
    {
        float validPointCount = 0;
        m_overallZ = 0.0f;

        // Calculate the average z depth
        for (int i = 0; i<m_vertices.Length; i++)
        {
            if(m_vertices[i].z != 0.0f)
            {
                m_overallZ += m_vertices[i].z;
                ++validPointCount;
            }
        }


        // Don't divide by zero!
        if (validPointCount != 0)
        {
            m_overallZ = m_overallZ / (validPointCount);
        } 
        else
        {
            m_overallZ = 0;
        }
    }

    /// <summary>
    /// GUI for switch getting data API and status.
    /// </summary>
    private void OnGUI()
    {
        if(m_tangoApplication.HasRequestedPermissions())
        {
            Color oldColor = GUI.color;
            GUI.color = Color.black;
            
            GUI.Label(new Rect(Common.UI_LABEL_START_X, 
                               Common.UI_DEPTH_LABLE_START_Y + Common.UI_LABEL_OFFSET, 
                               Common.UI_LABEL_SIZE_X , 
                               Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + "Average Depth (m): " + m_overallZ.ToString() + "</size>");
            
            GUI.Label(new Rect(Common.UI_LABEL_START_X, 
                               Common.UI_DEPTH_LABLE_START_Y + Common.UI_LABEL_OFFSET * 2.0f, 
                               Common.UI_LABEL_SIZE_X , 
                               Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + "Point Count: " + m_pointsCount.ToString() + "</size>");
            
            
            GUI.Label(new Rect(Common.UI_LABEL_START_X, 
                               Common.UI_DEPTH_LABLE_START_Y + Common.UI_LABEL_OFFSET * 3.0f, 
                               Common.UI_LABEL_SIZE_X , 
                               Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + "Frame delta time (ms): " + GetTimeSinceLastFrame().ToString(Common.UI_FLOAT_FORMAT) + "</size>");
            
            GUI.color = oldColor;
        }
    }
}
