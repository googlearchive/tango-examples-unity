// <copyright file="TangoPointCloud.cs" company="Google">
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
    /// If set, m_updatePointsMesh also gets set at start. Then PointCloud material's renderqueue is set to background
    /// (which is same as YUV2RGB Shader) so that PointCloud data gets written to Z buffer for Depth test with virtual
    /// objects in scene. Note this is a very rudimentary way of doing occlusion and limited by the capabilities of
    /// depth camera.
    /// </summary>
    [HideInInspector]
    public bool m_enableOcclusion;

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
    
    /// <summary>
    /// The Background renderqueue's number.
    /// </summary>
    private const int BACKGROUND_RENDER_QUEUE = 1000;

    /// <summary>
    /// Point size of PointCloud data when projected on to image plane.
    /// </summary>
    private const int POINTCLOUD_SPLATTER_UPSAMPLE_SIZE = 30;

    private TangoApplication m_tangoApplication;
    
    // Matrices for transforming pointcloud to world coordinates.
    // This equation will take account of the camera sensors extrinsic.
    // Full equation is:
    // Matrix4x4 unityWorldTDepthCamera = 
    // m_unityWorldTStartService * m_startServiceTDevice * Matrix4x4.Inverse(m_imuTDevice) * m_imuTDepthCamera;
    private Matrix4x4 m_unityWorldTStartService = new Matrix4x4();
    private Matrix4x4 m_startServiceTDevice = new Matrix4x4();
    private Matrix4x4 m_imuTDevice = new Matrix4x4();
    private Matrix4x4 m_imuTDepthCamera = new Matrix4x4();
    
    /// <summary>
    /// Mesh this script will modify.
    /// </summary>
    private Mesh m_mesh;
    
    // Logging data.
    private double m_previousDepthDeltaTime = 0.0;
    private bool m_isExtrinsicQuerable = false;

    private Renderer m_renderer;
    private System.Random m_rand;
    
    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start() 
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);
        
        m_unityWorldTStartService.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_unityWorldTStartService.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        m_unityWorldTStartService.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_unityWorldTStartService.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        
        // Assign triangles, note: this is just for visualizing point in the mesh data.
        m_points = new Vector3[MAX_POINT_COUNT];

        m_mesh = GetComponent<MeshFilter>().mesh;
        m_mesh.Clear();

        m_renderer = GetComponent<Renderer>();
        m_rand = new System.Random();
        if (m_enableOcclusion) 
        {
            m_renderer.enabled = true;
            m_renderer.material.renderQueue = BACKGROUND_RENDER_QUEUE;
            m_renderer.material.SetFloat("point_size", POINTCLOUD_SPLATTER_UPSAMPLE_SIZE);
            m_updatePointsMesh = true;
        }
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
                if (poseData.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
                {
                    return;
                }

                Vector3 position = new Vector3((float)poseData.translation[0],
                                               (float)poseData.translation[1],
                                               (float)poseData.translation[2]);
                Quaternion quat = new Quaternion((float)poseData.orientation[0],
                                                 (float)poseData.orientation[1],
                                                 (float)poseData.orientation[2],
                                                 (float)poseData.orientation[3]);
                m_startServiceTDevice = Matrix4x4.TRS(position, quat, Vector3.one);
                
                // The transformation matrix that represents the pointcloud's pose. 
                // Explanation: 
                // The pointcloud which is in Depth camera's frame, is put in unity world's 
                // coordinate system(wrt unity world).
                // Then we are extracting the position and rotation from uwTuc matrix and applying it to 
                // the PointCloud's transform.
                Matrix4x4 unityWorldTDepthCamera = m_unityWorldTStartService * m_startServiceTDevice * Matrix4x4.Inverse(m_imuTDevice) * m_imuTDepthCamera;
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                       
                // Converting points array to world space.
                m_overallZ = 0;
                for (int i = 0; i < m_pointsCount; ++i)
                {
                    float x = tangoDepth.m_points[(i * 3) + 0];
                    float y = tangoDepth.m_points[(i * 3) + 1];
                    float z = tangoDepth.m_points[(i * 3) + 2];

                    m_points[i] = unityWorldTDepthCamera.MultiplyPoint(new Vector3(x, y, z));
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
                m_renderer.material.SetMatrix("depthCameraTUnityWorld", unityWorldTDepthCamera.inverse);
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
    /// Finds all points within a certain radius of a point on the screen.
    /// 
    /// NOTE: This is slow because it looks at every single point in the point cloud.  Avoid
    /// calling this more than once a frame.
    /// </summary>
    /// <returns>A list of point indices for points within the radius.</returns>
    /// <param name="cam">The current camera.</param>
    /// <param name="pos">Position on screen (in pixels).</param>
    /// <param name="maxDist">The maximum pixel distance to allow.</param>
    public List<int> FindPointsWithinDistance(Camera cam, Vector2 pos, float maxDist)
    {
        List<int> closePoints = new List<int>();
        float sqMaxDist = maxDist * maxDist;

        for (int it = 0; it < m_pointsCount; ++it)
        {
            Vector3 screenPos3 = cam.WorldToScreenPoint(m_points[it]);
            Vector2 screenPos = new Vector2(screenPos3.x, screenPos3.y);

            float distSqr = Vector2.SqrMagnitude(screenPos - pos);
            if (distSqr > sqMaxDist)
            {
                continue;
            }
            closePoints.Add(it);
        }

        return closePoints;
    }

    /// <summary>
    /// Finds the average point from a set of point indices.
    /// </summary>
    /// <returns>The average point value.</returns>
    /// <param name="points">The points to compute the average for.</param>
    public Vector3 GetAverageFromFilteredPoints(List<int> points)
    {
        Vector3 averagePoint = new Vector3(0, 0, 0);

        for (int i = 0; i < points.Count; i++)
        {
            averagePoint += m_points[points[i]];
        }

        averagePoint /= points.Count;

        return averagePoint;
    }

    /// <summary>
    /// Given a screen coordinate and search radius, finds a plane that most closely
    /// fits depth values in that area.
    /// </summary>
    /// <returns>True if a plane was found, false otherwise.</returns>
    /// <param name="cam">The Unity camera.</param>
    /// <param name="pos">The point in screen space to perform detection on.</param>
    /// <param name="maxPixelDist">The search radius to use in pixels.</param>
    /// <param name="minInlierPercentage">The minimum percentage for inliers when fitting a plane.</param>
    /// <param name="planeCenter">Filled in with the center of the plane in Unity world space.</param>
    /// <param name="plane">Filled in with a model of the plane in Unity world space.</param>
    public bool FindPlane(Camera cam, Vector2 pos,
            float maxPixelDist, float minInlierPercentage,
            out Vector3 planeCenter, out Plane plane)
    {
        List<int> closestPoints = FindPointsWithinDistance(cam, pos, maxPixelDist);
        planeCenter = GetAverageFromFilteredPoints(closestPoints);
        List<int> inliers;
        if (!GetPlaneUsingRANSAC(cam, closestPoints, minInlierPercentage, out inliers, out plane))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Given a set of points find the best fit plane with RANSAC.
    /// TODO(@eitanm): refine with SVD after this.
    /// </summary>
    /// <returns>True if the plane fit succeeds, false otherwise.</returns>
    /// <param name="cam">The unity camera.</param>
    /// <param name="points">The points to compute the plane for.</param>
    /// <param name="minPercentage">The minimum percentage of inliers to be considered a plane.</param>
    /// <param name="inliers">Filled in with the indices of the plane's inliers.</param>
    /// <param name="plane">Filled in with the model of the best fit plane.</param> 
    public bool GetPlaneUsingRANSAC(Camera cam, List<int> points, double minPercentage,
            out List<int> inliers, out Plane plane)
    {
        inliers = new List<int>();
        plane = new Plane();

        if (points.Count < 3)
        {
            return false;
        }

        // Max number of iterations
        int maxIterations = 50;

        // Threshold to define if a point belongs to a plane or not
        // Distance in meters from point to plane
        double threshold = 0.02;

        int maxFittedPoints = 0;
        double percentageFitted = 0;

        // RANSAC algorithm to determine inliers
        for (int i = 0; i < maxIterations; i++)
        {
            List<int> candidateInliers = new List<int>();

            Plane candidatePlane = MakeRandomPlane(cam, points);

            // See for every point if it belongs to that Plane or not
            for (int j = 0; j < points.Count; j++)
            {
                float distToPlane = candidatePlane.GetDistanceToPoint(m_points[points[j]]);
                if (distToPlane < threshold)
                {
                    candidateInliers.Add(points[j]);
                }
            }
            if (candidateInliers.Count > maxFittedPoints)
            {
                maxFittedPoints = candidateInliers.Count;
                inliers = candidateInliers;
                plane = candidatePlane;
            }

            percentageFitted = maxFittedPoints / points.Count;
            if (percentageFitted > minPercentage)
            {
                break;
            }
        }

        // If we couldn't reach the minimum points to be fitted with RANSAC, return false
        if (percentageFitted < minPercentage)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Create a plane from a list of points at random.
    /// </summary>
    /// <returns>A random plane.</returns>
    /// <param name="cam">The Unity camera so we can make sure the plane orientation is correct.</param>
    /// <param name="points">The points to compute a random plane from.</param>
    private Plane MakeRandomPlane(Camera cam, List<int> points)
    {
        if (points.Count < 3)
        {
            return new Plane();
        }

        // Choose 3 points randomly.
        int r0 = m_rand.Next(points.Count);
        int r1 = m_rand.Next(points.Count - 1);

        // Make sure we handle collisions.
        if (r1 == r0)
        {
            r1++;
        }
        else if (r1 < r0)
        {
            // We'll make sure to keep r0 and r1 in sorted order.
            int temp = r0;
            r0 = r1;
            r1 = temp;
        }

        int r2 = m_rand.Next(points.Count - 2);

        // Handle collisions.
        if (r2 == r0)
        {
            ++r2;
        }
        if (r2 == r1)
        {
            ++r2;
        }

        int idx0 = points[r0];
        int idx1 = points[r1];
        int idx2 = points[r2];

        Vector3 p0 = m_points[idx0];
        Vector3 p1 = m_points[idx1];
        Vector3 p2 = m_points[idx2];

        // Define the plane
        Plane plane = new Plane(p0, p1, p2);

        // Make sure that the normal of the plane points towards the camera.
        if (Vector3.Dot(cam.transform.forward, plane.normal) > 0)
        {
            plane.SetNormalAndPosition(plane.normal * -1.0f, p0);
        }
        return plane;
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
        m_imuTDevice = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));

        // Query the extrinsics between IMU and color camera frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        position = new Vector3((float)poseData.translation[0],
                               (float)poseData.translation[1],
                               (float)poseData.translation[2]);
        quat = new Quaternion((float)poseData.orientation[0],
                              (float)poseData.orientation[1],
                              (float)poseData.orientation[2],
                              (float)poseData.orientation[3]);
        m_imuTDepthCamera = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));
    }
}
