//-----------------------------------------------------------------------
// <copyright file="TangoPointCloud.cs" company="Google">
//   
// Copyright 2015 Google Inc. All Rights Reserved.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;
using Tango;

/// <summary>
/// Point cloud visualize using depth frame API.
/// </summary>
public class TangoPointCloud : MonoBehaviour, ITangoDepth
{
    /// <summary>
    /// If set, the point cloud's mesh gets updated (much slower, useful for debugging).
    /// </summary>
    public bool m_updatePointsMesh;

    /// <summary>
    /// The points of the point cloud, in world space.
    /// 
    /// Note that not every member of this array will be filled out, see m_pointsCount.
    /// </summary>
    [HideInInspector]
    public Vector3[] m_points;

    /// <summary>
    /// The number of points in m_points.
    /// </summary>
    [HideInInspector]
    public int m_pointsCount = 0;

    /// <summary>
    /// The average depth (relative to the depth camera).
    /// </summary>
    [HideInInspector]
    public float m_overallZ = 0.0f;

    /// <summary>
    /// Time between the last two depth events.
    /// </summary>
    [HideInInspector]
    public float m_depthDeltaTime = 0.0f;

    /// <summary>
    /// The maximum points displayed.  Just some const value.
    /// </summary>
    private const int MAX_POINT_COUNT = 61440;
    
    private TangoApplication m_tangoApplication;
    
    // Matrices for transforming pointcloud to world coordinates.
    // This equation will take account of the camera sensors extrinsic.
    // Full equation is:
    //   Matrix4x4 uwTc = m_uwTss * m_ssTd * Matrix4x4.Inverse(m_imuTd) * m_imuTc
    private Matrix4x4 m_uwTss = new Matrix4x4 ();
    private Matrix4x4 m_ssTd = new Matrix4x4 ();
    private Matrix4x4 m_imuTd = new Matrix4x4 ();
    private Matrix4x4 m_imuTc = new Matrix4x4();
    
    /// <summary>
    /// Mesh this script will modify.
    /// </summary>
    private Mesh m_mesh;
    
    // Logging data.
    private double m_previousDepthDeltaTime = 0.0;
    private bool m_isExtrinsicQuerable = false;

    private Renderer m_renderer;
    
    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start() 
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);
        
        m_uwTss.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        m_uwTss.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        
        // Assign triangles, note: this is just for visualizing point in the mesh data.
        m_points = new Vector3[MAX_POINT_COUNT];

        m_mesh = GetComponent<MeshFilter>().mesh;
        m_mesh.Clear();

        m_renderer = GetComponent<Renderer>();
    }
    
    /// <summary>
    /// Callback that gets called when depth is available from the Tango Service.
    /// </summary>
    /// <param name="tangoDepth">Depth information from Tango.</param>
    public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
    {
        // Calculate the time since the last successful depth data
        // collection.
        if (m_previousDepthDeltaTime == 0.0)
        {
            m_previousDepthDeltaTime = tangoDepth.m_timestamp;
        }
        else
        {
            m_depthDeltaTime = (float)((tangoDepth.m_timestamp - m_previousDepthDeltaTime) * 1000.0);
            m_previousDepthDeltaTime = tangoDepth.m_timestamp;
        }
        
        // Fill in the data to draw the point cloud.
        if (tangoDepth != null && tangoDepth.m_points != null)
        {
            m_pointsCount = tangoDepth.m_pointCount;
            if (m_pointsCount > 0)
            {   
                _SetUpExtrinsics();
                TangoCoordinateFramePair pair;
                TangoPoseData poseData = new TangoPoseData();

                // Query pose to transform point cloud to world coordinates, here we are using the timestamp
                // that we get from depth.
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime(poseData, m_previousDepthDeltaTime, pair);
                if (poseData.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID) {
                    return;
                }
                Vector3 position = new Vector3((float)poseData.translation[0],
                                               (float)poseData.translation[1],
                                               (float)poseData.translation[2]);
                Quaternion quat = new Quaternion((float)poseData.orientation[0],
                                                 (float)poseData.orientation[1],
                                                 (float)poseData.orientation[2],
                                                 (float)poseData.orientation[3]);
                m_ssTd = Matrix4x4.TRS(position, quat, Vector3.one);
                
                // The transformation matrix that represents the pointcloud's pose. 
                // Explanation: 
                // The pointcloud which is in RGB's camera frame, is put in unity world's 
                // coordinate system(wrt unit camera).
                // Then we are extracting the position and rotation from uwTuc matrix and applying it to 
                // the PointCloud's transform.
                Matrix4x4 uwTc = m_uwTss * m_ssTd * Matrix4x4.Inverse(m_imuTd) * m_imuTc;
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                       
                // Converting points array to world space.
                m_overallZ = 0;
                for (int i = 0; i < m_pointsCount; ++i)
                {
                    float x = tangoDepth.m_points[(i * 3) + 0];
                    float y = tangoDepth.m_points[(i * 3) + 1];
                    float z = tangoDepth.m_points[(i * 3) + 2];

                    m_points[i] = uwTc.MultiplyPoint(new Vector3(x, y, z));
                    m_overallZ += z;
                }
                m_overallZ = m_overallZ / m_pointsCount;

                if (m_updatePointsMesh)
                {
                    // Need to update indicies too!
                    int[] indices = new int[m_pointsCount];
                    for (int i = 0; i < m_pointsCount; ++i)
                    {
                        indices[i] = i;
                    }

                    m_mesh.Clear();
                    m_mesh.vertices = m_points;
                    m_mesh.SetIndices(indices, MeshTopology.Points, 0);
                }

                // The color should be pose relative, we need to store enough info to go back to pose values.
                m_renderer.material.SetMatrix("ucTuw", uwTc.inverse);
            }
            else
            {
                m_overallZ = 0;
            }
        }
    }

    /// <summary>
    /// Finds the closest point from a point cloud to a position on screen.
    /// 
    /// NOTE: This is slow because it looks at every single point in the point cloud.  Avoid
    /// calling this more than once a frame.
    /// </summary>
    /// <returns>The index of the closest point, or -1 if not found.</returns>
    /// <param name="cam">The current camera.</param>
    /// <param name="pos">Position on screen (in pixels).</param>
    /// <param name="maxDist">The maximum pixel distance to allow.</param>
    public int FindClosestPoint(Camera cam, Vector2 pos, int maxDist)
    {
        int bestIndex = -1;
        float bestDistSqr = 0;
        
        for (int it = 0; it < m_pointsCount; ++it)
        {
            Vector3 screenPos3 = cam.WorldToScreenPoint(m_points[it]);
            Vector2 screenPos = new Vector2(screenPos3.x, screenPos3.y);
            
            float distSqr = Vector2.SqrMagnitude(screenPos - pos);
            if (distSqr > maxDist * maxDist)
            {
                continue;
            }
            
            if (bestIndex == -1 || distSqr < bestDistSqr)
            {
                bestIndex = it;
                bestDistSqr = distSqr;
            }
        }
        
        return bestIndex;
    }

    /// <summary>
    /// Sets up extrinsic matrixces for this hardware.
    /// </summary>
    private void _SetUpExtrinsics()
    {
        double timestamp = 0.0;
        TangoCoordinateFramePair pair;
        TangoPoseData poseData = new TangoPoseData();

        // Query the extrinsics between IMU and device frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        Vector3 position = new Vector3((float)poseData.translation[0],
                                       (float)poseData.translation[1],
                                       (float)poseData.translation[2]);
        Quaternion quat = new Quaternion((float)poseData.orientation[0],
                                         (float)poseData.orientation[1],
                                         (float)poseData.orientation[2],
                                         (float)poseData.orientation[3]);
        m_imuTd = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));
        
        // Query the extrinsics between IMU and color camera frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        position = new Vector3((float)poseData.translation[0],
                               (float)poseData.translation[1],
                               (float)poseData.translation[2]);
        quat = new Quaternion((float)poseData.orientation[0],
                              (float)poseData.orientation[1],
                              (float)poseData.orientation[2],
                              (float)poseData.orientation[3]);
        m_imuTc = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));
    }
}
