//-----------------------------------------------------------------------
// <copyright file="CustomPointCloudListener.cs" company="Google">
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
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Tango;

/// <summary>
/// Manages points cloud data either from the API, playback file, synthetic room, or test generation.
/// </summary>
public class CustomPointCloudListener : MonoBehaviour, ITangoDepth
{
    /**
     * Main Camera
     */    
    public Camera m_mainCamera;

    /**
     * Dynamic Mesh Manager
     */    
    public DynamicMeshManager m_meshManager;

    /**
     * Synthetic Room for raycasting into
     */    
    public GameObject syntheticRoom;

    /**
     * Number of points to insert per depth frame update
     */    
    public int m_insertionCount = 1000;

    /**
     * file name of recorded session used for playback
     */    
    public string m_recordingID = "2014_10_26_031617";

    /**
     * Enable playback
     */    
    public bool m_playbackData = false;

    /**
     * Enable synthetic data generation using synthetic room
     */    
    public bool m_syntheticData = false;

    /**
     * Enable Generated Test Data
     */    
    public bool m_testData = false;

    /**
     * Enable Recording
     */    
    private bool m_recordData = false;

    /**
     * Flag for updated depth data
     */        
    private bool m_isDirty;

    /**
     * Copy of the detph data
     */    
    private TangoUnityDepth m_currTangoDepth = new TangoUnityDepth();

    /**
     * Pose of the device when depth data arrived
     */    
    private TangoPoseData m_poseAtDepthTimestamp = new TangoPoseData();

    /**
     * Time stamp used to create unique file recording names
     */        
    private string m_sessionTimestamp = "None";

    /**
     * File writer
     */    
    private BinaryWriter m_fileWriter = null;

    /**
     * File reader
     */    
    private BinaryReader m_fileReader = null;
        
    /**
     * Cubes for live depth preview
     */    
    private GameObject[] m_cubes = null;

    /**
     * Index for live preview cubes
     */    
    private int m_cubeIndex = 0;

    /**
     * Motion trail history for depth camera pose
     */    
    private List<Vector3> m_positionHistory = new List<Vector3>();

    /**
     * Initial camera position offset
     */    
    private Vector3 m_initCameraPosition = new Vector3();

    /**
     * Scratch space for raycasting synthetic data
     */    
    private RaycastHit hitInfo = new RaycastHit();

    /**
     * Raycast layer for synthetic room
     */    
    private int syntheticRoomLayer = 1 << 8;

    /**
     * Raycast distance for synthetic data
     */    
    private float syntheticRaycastMaxDistance = 4;

    /**
     * Debug text field
     */    
    private string m_debugText;

    /**
     * pause playback or depth accumulation
     */    
    private bool m_pause = false;

    /**
     * step playback
     */    
    private bool m_step = false;

    /**
     * Track frame count
     */    
    private int m_frameCount;

    /**
     * Minimum square distance to insert depth data.  
     * Sensor may produce values at 0, should be rejected
     */    
    private float m_sqrMinimumDepthDistance = 0.0625f;

    /**
     * frame of reference for depth data
     */    
    private TangoCoordinateFramePair m_coordinatePair;

    /**
     * Reference to main Tango Application
     */    
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start() 
    {
        m_isDirty = false;

        m_initCameraPosition = m_mainCamera.transform.position;

        m_coordinatePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
        m_coordinatePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE; // FIX should be depth sensor

        m_cubes = new GameObject[m_insertionCount];
        float size = 0.02f;
        for (int i = 0; i < m_insertionCount; i++)
        {
            m_cubes[i] = (GameObject)GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_cubes[i].transform.localScale = new Vector3(size, size, size);
            m_cubes[i].transform.parent = transform;
        }

        syntheticRoom.SetActive(m_syntheticData);
        ClearLivePreviewCubes();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (m_recordData)
        {
            PrepareRecording();
        }
#endif
        if (m_playbackData)
        {
            m_recordData = false;
            string filename = m_recordingID + ".dat";
            m_fileReader = new BinaryReader(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Open));
            m_debugText = "Loading from: " + filename + " " + m_fileReader.ToString();
        }

        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);

        if (m_testData)
        {
            GenerateTestData();
        }
    }

    /// <summary>
    /// Generate synthetic test point cloud data for performance and debugging.
    /// </summary>
    public void GenerateTestData()
    {
        Debug.Log("Generating Test Data");

        Vector3 obs = new Vector3(0, -1, 0);
        float range = 6.0f;
        float step = 0.025f;
        for (float x = -range; x < range; x += step) 
        {
            for (float z = -range; z < range; z += step) 
            {
                float y = 0.5f * Mathf.Sin(2 * Mathf.Sqrt((x * x) + (z * z)));
                m_meshManager.InsertPoint(new Vector3(x, y, z + range), obs, 1.0f);
            }
        }
        
        m_meshManager.QueueDirtyMeshesForRegeneration();
    }

    /// <summary>
    /// Reset Point Cloud preview, history, and mesh data.
    /// </summary>
    public void Reset()
    {
        ClearLivePreviewCubes();
        m_positionHistory.Clear();
        m_meshManager.Clear();
    }
    
    /// <summary>
    /// Write pose data to file.
    /// </summary>
    /// <param name="writer">File writer.</param>
    /// <param name="pose">Tango pose data.</param>
    public void WritePoseToFile(BinaryWriter writer, TangoPoseData pose)
    {
        if (writer == null)
        {
            return;
        }
        
        writer.Write("poseframe\n");
        writer.Write(pose.timestamp + "\n");
        writer.Write((int)pose.framePair.baseFrame);
        writer.Write((int)pose.framePair.targetFrame);
        writer.Write((int)pose.status_code);
        writer.Write(pose.translation[0]);
        writer.Write(pose.translation[1]);
        writer.Write(pose.translation[2]);
        writer.Write(pose.orientation[0]);
        writer.Write(pose.orientation[1]);
        writer.Write(pose.orientation[2]);
        writer.Write(pose.orientation[3]);
        writer.Flush();
    }

    /// <summary>
    /// Read pose from file.
    /// </summary>
    /// <returns>The pose from file.</returns>
    /// <param name="reader">File reader.</param>
    /// <param name="pose">Tango pose data.</param>
    public int ReadPoseFromFile(BinaryReader reader, ref TangoPoseData pose)
    {
        if (reader == null)
        {
            return -1;
        }
        
        string frameMarker;
        try
        {
            frameMarker = reader.ReadString();
        }
        catch (EndOfStreamException x) 
        {
            reader.BaseStream.Position = 0;
            Reset();
            print("Restarting log file: " + x.ToString());
            frameMarker = reader.ReadString();
        }
        
        if (frameMarker.CompareTo("poseframe\n") != 0)
        {
            m_debugText = "Failed to read pose";
            return -1;
        }
        
        pose.timestamp = double.Parse(reader.ReadString());
        
        TangoCoordinateFramePair pair = new TangoCoordinateFramePair();
        pair.baseFrame = (Tango.TangoEnums.TangoCoordinateFrameType)reader.ReadInt32();
        pair.targetFrame = (Tango.TangoEnums.TangoCoordinateFrameType)reader.ReadInt32();
        pose.framePair = pair;
        
        pose.status_code = (Tango.TangoEnums.TangoPoseStatusType)reader.ReadInt32();
        pose.translation[0] = reader.ReadDouble();
        pose.translation[1] = reader.ReadDouble();
        pose.translation[2] = reader.ReadDouble();
        pose.orientation[0] = reader.ReadDouble();
        pose.orientation[1] = reader.ReadDouble();
        pose.orientation[2] = reader.ReadDouble();
        pose.orientation[3] = reader.ReadDouble();
        return 0;
    }

    /// <summary>
    /// Write depth data to file.
    /// </summary>
    /// <param name="writer">File writer.</param>
    /// <param name="depthFrame">Tango depth data.</param>
    public void WriteDepthToFile(BinaryWriter writer, TangoUnityDepth depthFrame)
    {
        if (writer == null)
        {
            return;
        }
        
        writer.Write("depthframe\n");
        writer.Write(depthFrame.m_timestamp + "\n");
        writer.Write(depthFrame.m_pointCount + "\n");
        
        for (int i = 0; i < depthFrame.m_pointCount; i++)
        {
            writer.Write(depthFrame.m_points[(3 * i) + 0]);
            writer.Write(depthFrame.m_points[(3 * i) + 1]);
            writer.Write(depthFrame.m_points[(3 * i) + 2]);
        }
        writer.Flush();
    }

    /// <summary>
    /// Read depth data from file.
    /// </summary>
    /// <returns>The depth from file.</returns>
    /// <param name="reader">File reader.</param>
    /// <param name="depthFrame">Tango depth data.</param>
    public int ReadDepthFromFile(BinaryReader reader, ref TangoUnityDepth depthFrame)
    {
        string frameMarker;
        try
        {
            frameMarker = reader.ReadString();
        }
        catch (EndOfStreamException x)
        {
            reader.BaseStream.Position = 0;
            Reset();
            
            print("Restarting log file: " + x.ToString());
            frameMarker = reader.ReadString();
        }
        
        if (frameMarker.CompareTo("depthframe\n") != 0)
        {
            m_debugText = "Failed to read depth";
            return -1;
        }
        depthFrame.m_timestamp = double.Parse(reader.ReadString());
        depthFrame.m_pointCount = int.Parse(reader.ReadString());
        if (depthFrame.m_pointCount > depthFrame.m_points.Length)
        {
            depthFrame.m_points = new float[3 * (int)(1.5f * depthFrame.m_pointCount)];
        }
        
        // load up the data
        for (int i = 0; i < depthFrame.m_pointCount; i++)
        {
            depthFrame.m_points[(3 * i) + 0] = reader.ReadSingle();
            depthFrame.m_points[(3 * i) + 1] = reader.ReadSingle();
            depthFrame.m_points[(3 * i) + 2] = reader.ReadSingle();
        }
        
        return 0;
    }

    /// <summary>
    /// An event notifying when new depth data is available. OnTangoDepthAvailable events are thread safe.
    /// </summary>
    /// <param name="tangoDepth">Depth data that we get from API.</param>
    public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
    {
        // Fill in the data to draw the point cloud.
        if (tangoDepth != null)
        {
            if (tangoDepth.m_points == null) 
            {
                Debug.Log("Depth points are null");
                return;
            }
            
            if (tangoDepth.m_pointCount > m_currTangoDepth.m_points.Length)
            {
                m_currTangoDepth.m_points = new float[3 * (int)(1.5f * tangoDepth.m_pointCount)];
            }
            
            for (int i = 0; i < tangoDepth.m_pointCount; i += 3)
            {
                m_currTangoDepth.m_points[(3 * i) + 0] = tangoDepth.m_points[(i * 3) + 0];
                m_currTangoDepth.m_points[(3 * i) + 1] = tangoDepth.m_points[(i * 3) + 1];
                m_currTangoDepth.m_points[(3 * i) + 2] = tangoDepth.m_points[(i * 3) + 2];
            }
            m_currTangoDepth.m_timestamp = tangoDepth.m_timestamp;
            m_currTangoDepth.m_pointCount = tangoDepth.m_pointCount;
            
            PoseProvider.GetPoseAtTime(m_poseAtDepthTimestamp, m_currTangoDepth.m_timestamp, m_coordinatePair);
            
            if (m_poseAtDepthTimestamp.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                return;
            }
            
            m_isDirty = true;
        }
        return;
    }

    /// <summary>
    /// Create file for recording.
    /// </summary>
    private void PrepareRecording()
    {
        m_sessionTimestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
        string filename = m_sessionTimestamp + ".dat";
        if (m_fileWriter != null)
        {
            m_fileWriter.Close();
            m_fileWriter = null;
        }
        m_fileWriter = new BinaryWriter(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Create));
        m_debugText = "Saving to: " + filename + " " + m_fileWriter.ToString();
    }

    /// <summary>
    /// Draw debug frustum lines.
    /// </summary>
    private void DrawDebugLines()
    {
        float frustumSize = 2;
        Color frustumColor = Color.red;
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) + transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) - transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) - transform.right - transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) + transform.right - transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) + transform.right + transform.up, transform.position + (frustumSize * transform.forward) + transform.right - transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) + transform.right + transform.up, transform.position + (frustumSize * transform.forward) - transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) - transform.right - transform.up, transform.position + (frustumSize * transform.forward) - transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) - transform.right - transform.up, transform.position + (frustumSize * transform.forward) + transform.right - transform.up, frustumColor);
    }

    /// <summary>
    /// Update is called once per frame
    /// It processes input keyfor pausing, steping.  It will also playback/record data or generate synthetic data.
    /// If running on Tango device it will process depth data that was copied for the depth callback.
    /// </summary>
    private void Update() 
    {
        m_frameCount++;

        if (Input.GetKeyDown(KeyCode.P))
        {
            m_pause = !m_pause;
        }

        if (Input.GetKeyDown(KeyCode.Slash))
        {
            m_step = true;
        }

        // history
        for (int i = 0; i < m_positionHistory.Count - 1; i++)
        {
            Debug.DrawLine(m_positionHistory[i], m_positionHistory[i + 1], Color.white);
        }

        if (m_pause && !m_step) 
        {
            DrawDebugLines();
            return;
        }

        m_step = false;

        if (m_recordData && !m_playbackData) 
        {
            WritePoseToFile(m_fileWriter, m_poseAtDepthTimestamp);
            WriteDepthToFile(m_fileWriter, m_currTangoDepth);
            m_debugText = "Recording Session: " + m_sessionTimestamp + " Points: " + m_currTangoDepth.m_timestamp;
        }

        if (m_playbackData) 
        {
            ReadPoseFromFile(m_fileReader, ref m_poseAtDepthTimestamp);
            ReadDepthFromFile(m_fileReader, ref m_currTangoDepth);
            m_mainCamera.transform.position = transform.position;
            m_mainCamera.transform.rotation = transform.rotation;
            m_isDirty = true;
        }

        if (m_syntheticData) 
        {
            // FIX - proper simulation should populate m_poseAtDepthTimestamp and m_currTangoDepth
            // and the let the inset loop at the bottom execute normally.
            // This implementation overrides the insert loop.
            transform.position = m_mainCamera.transform.position;
            transform.rotation = m_mainCamera.transform.rotation;

            m_currTangoDepth.m_pointCount = 0;
////            m_currTangoDepth.m_timestamp = UnityEngine.Time.realtimeSinceStartup;
////            m_poseAtDepthTimestamp.timestamp = UnityEngine.Time.realtimeSinceStartup;
////            m_poseAtDepthTimestamp.status_code = TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;

            if ((m_frameCount % 6) == 0)
            {
                float start = Time.realtimeSinceStartup;
                
                for (int i = 0; i < m_insertionCount; i++)
                {
                    int x = UnityEngine.Random.Range(0, Screen.width);
                    int y = UnityEngine.Random.Range(0, Screen.height);
                    Ray ray = m_mainCamera.ScreenPointToRay(new Vector3(x, y, 0));
                    if (Physics.Raycast(ray, out hitInfo, syntheticRaycastMaxDistance, syntheticRoomLayer))
                    {
                        m_meshManager.InsertPoint(hitInfo.point, ray.direction, 1.0f / (hitInfo.distance + 1));
                    }
                }
                m_meshManager.QueueDirtyMeshesForRegeneration();
                float stop = Time.realtimeSinceStartup;
                m_meshManager.InsertionTime = (m_meshManager.InsertionTime * m_meshManager.TimeSmoothing) + ((1.0f - m_meshManager.TimeSmoothing) * (stop - start));
            }
        }
        else if (m_isDirty)
        {
            ClearLivePreviewCubes();

            SetTransformUsingTangoPose(transform, m_poseAtDepthTimestamp);

            m_positionHistory.Add(transform.position);

            DrawDebugLines();

            float insertionStartTime = Time.realtimeSinceStartup;

            for (int i = 0; i < m_insertionCount; i++)
            {
                if (i > m_currTangoDepth.m_pointCount)
                {
                    break;
                }

                // randomly sub sample
                int index = i;

                // need to be more graceful than this, does not behave continuously
                if (m_insertionCount < m_currTangoDepth.m_pointCount)
                {
                    index = UnityEngine.Random.Range(0, m_currTangoDepth.m_pointCount);
                }

                Vector3 p = new Vector3(m_currTangoDepth.m_points[3 * index], -m_currTangoDepth.m_points[(3 * index) + 1], m_currTangoDepth.m_points[(3 * index) + 2]);
                float sqrmag = p.sqrMagnitude;

                if (sqrmag < m_sqrMinimumDepthDistance)
                {
                    continue;
                }

                Vector3 tp = transform.TransformPoint(p);

                // less weight for things farther away, because of noise
                if (m_recordData)
                {
                    SetLivePreviewCube(tp);
                }
                else
                {
                    m_meshManager.InsertPoint(tp, transform.forward, 1.0f / (sqrmag + 1.0f));
                }
            }

            float insertionStopTime = Time.realtimeSinceStartup;
            m_meshManager.InsertionTime = (m_meshManager.InsertionTime * m_meshManager.TimeSmoothing) + ((1.0f - m_meshManager.TimeSmoothing) * (insertionStopTime - insertionStartTime));
            m_meshManager.QueueDirtyMeshesForRegeneration();
            m_isDirty = false;
        }
    }

    /// <summary>
    /// Clear live preview cubes.
    /// </summary>
    private void ClearLivePreviewCubes()
    {
        for (int i = 0; i < m_insertionCount; i++)
        {
            m_cubes[i].SetActive(false);
        }
        m_cubeIndex = 0;
    }

    /// <summary>
    /// Set Live preview cube position.
    /// </summary>
    /// <param name="p">Position to set the preview cube.</param>
    private void SetLivePreviewCube(Vector3 p)
    {
        m_cubes[m_cubeIndex].transform.position = p;
        m_cubes[m_cubeIndex].SetActive(true);
        m_cubeIndex++;
    }

    /// <summary>
    /// Tranform Tango pose data to Unity transform.
    /// </summary>
    /// <param name="xform">Unity Transform output.</param>
    /// <param name="pose">Tango Pose Data.</param>
    private void SetTransformUsingTangoPose(Transform xform, TangoPoseData pose)
    {
        xform.position = new Vector3((float)pose.translation[0],
                                     (float)pose.translation[2],
                                     (float)pose.translation[1]) + m_initCameraPosition;
        
        Quaternion quat = new Quaternion((float)pose.orientation[0],
                                         (float)pose.orientation[2], // these rotation values are swapped on purpose
                                         (float)pose.orientation[1],
                                         (float)pose.orientation[3]);

        Quaternion axisFixedQuat = Quaternion.Euler(-quat.eulerAngles.x,
                                              -quat.eulerAngles.z,
                                              quat.eulerAngles.y);

        // FIX - should query API for depth camera extrinsics
        Quaternion extrinsics = Quaternion.Euler(-12.0f, 0, 0);

        xform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f) * axisFixedQuat * extrinsics;
    }

    /// <summary>
    /// Display some on screen debug infromation and handles the record button.
    /// </summary>
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 180, 1000, 30), "Depth Points: " + m_currTangoDepth.m_pointCount);
        GUI.Label(new Rect(10, 200, 1000, 30), "Debug: " + m_debugText);
        if (!m_recordData)
        {
            if (GUI.Button(new Rect(Screen.width - 160, 120, 140, 80), "Start Record"))
            {
                m_meshManager.Clear();
                PrepareRecording();
                m_recordData = true;
            }
        } 
        else
        {
            if (GUI.Button(new Rect(Screen.width - 160, 120, 140, 80), "Stop Record"))
            {
                m_recordData = false;
                m_fileWriter.Close();
                m_fileWriter = null;
                m_debugText = "Stopped Recording";
            }
        }

        string buttonName = "Pause";
        if (m_pause)
        {
            buttonName = "Resume";
        }
        if (GUI.Button(new Rect(Screen.width - 160, 220, 140, 80), buttonName))
        {
            m_pause = !m_pause;
        }
    }
}
