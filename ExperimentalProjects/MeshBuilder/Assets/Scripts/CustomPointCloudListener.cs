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
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Tango;
using System.IO;

/// <summary>
/// Point cloud visualize using depth frame API.
/// </summary>
public class CustomPointCloudListener : MonoBehaviour, ITangoDepth
{
    public bool m_recordData = false;
    public bool m_playbackData = false;
    public GameObject m_occupancyManagerObject;
    public Camera m_mainCamera;
    public int m_insertionCount = 10;
    public string m_loadSessionID = "2014_10_26_031617";

    private DynamicMeshManager m_occupancyManager;
   
    // Mesh data.
    private bool m_isDirty;
    private int m_maxPoints = 20000;
    private float[] m_depthPoints;
    private TangoUnityDepth m_currTangoDepth = new TangoUnityDepth();
    private TangoPoseData m_poseAtDepthTimestamp = new TangoPoseData();

    private string m_sessionTimestamp = "None";
    private BinaryWriter m_fileWriter = null;
    private BinaryReader m_fileReader = null;
        
    private GameObject[] m_quads = null;
    private int m_quadIndex = 0;
    
    private List<Vector3> m_positionHistory = new List<Vector3>();

    private Vector3 m_initCameraPosition = new Vector3();
    
    private string m_debugText;

    private bool m_pause = false;
    private bool m_step = false;

    private TangoCoordinateFramePair m_coordinatePair;
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start() 
    {
        m_occupancyManager = m_occupancyManagerObject.GetComponent<DynamicMeshManager> ();

        m_depthPoints = new float[m_maxPoints * 3];        
        m_isDirty = false;

        m_initCameraPosition = m_mainCamera.transform.position;

        m_coordinatePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
        m_coordinatePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;//FIX should be depth sensor

        m_quads = new GameObject[m_insertionCount];
        float size = 0.02f;
        for (int i =0; i < m_insertionCount; i++)
        {
            m_quads[i] = (GameObject)GameObject.CreatePrimitive (PrimitiveType.Cube);
            m_quads[i].transform.localScale = new Vector3(size, size, size);
            m_quads[i].transform.parent = transform;
        }


#if UNITY_ANDROID && !UNITY_EDITOR
        if (m_recordData)
        {
            PrepareRecording();
        }
#endif
        if (m_playbackData)
        {
            m_recordData = false;
            string filename = m_loadSessionID +".dat";
            m_fileReader = new BinaryReader(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Open));
            m_debugText = "Loading from: " + filename + " " + m_fileReader.ToString();
        }

        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);
    }

    void PrepareRecording()
    {
        m_sessionTimestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
        string filename = m_sessionTimestamp+".dat";
        if (m_fileWriter != null)
        {
            m_fileWriter.Close();
            m_fileWriter = null;
        }
        m_fileWriter = new BinaryWriter(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Create));
        m_debugText = "Saving to: " + filename + " " + m_fileWriter.ToString();
    }

    void DrawDebugLines()
    {
        float frustumSize = 3;
        Color frustumColor = Color.red;
        Debug.DrawLine(transform.position, transform.position + frustumSize*transform.forward+transform.right+transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + frustumSize*transform.forward-transform.right+transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + frustumSize*transform.forward-transform.right-transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + frustumSize*transform.forward+transform.right-transform.up, frustumColor);
        Debug.DrawLine(transform.position + frustumSize*transform.forward+transform.right+transform.up, transform.position + frustumSize*transform.forward+transform.right-transform.up, frustumColor);
        Debug.DrawLine(transform.position + frustumSize*transform.forward+transform.right+transform.up, transform.position + frustumSize*transform.forward-transform.right+transform.up, frustumColor);
        Debug.DrawLine(transform.position + frustumSize*transform.forward-transform.right-transform.up, transform.position + frustumSize*transform.forward-transform.right+transform.up, frustumColor);
        Debug.DrawLine(transform.position + frustumSize*transform.forward-transform.right-transform.up, transform.position + frustumSize*transform.forward+transform.right-transform.up, frustumColor);
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void LateUpdate() 
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            m_pause = !m_pause;
        }

        if (Input.GetKeyDown (KeyCode.Slash))
        {
            m_step = true;
        }

        //history
        for (int i =0; i < m_positionHistory.Count-1; i++) {
            Debug.DrawLine(m_positionHistory[i], m_positionHistory[i+1], Color.gray);
        }

        if (m_pause && !m_step) {
            DrawDebugLines();
            return;
        }

        m_step = false;

        if (m_playbackData) {
            ReadPoseFromFile(m_fileReader,ref m_poseAtDepthTimestamp);
            ReadDepthFromFile(m_fileReader, ref m_currTangoDepth, ref m_depthPoints);
            m_isDirty = true;
        }

        if (m_isDirty)
        {
            if(m_recordData) {
                WritePoseToFile(m_fileWriter, m_poseAtDepthTimestamp);
                WriteDepthToFile(m_fileWriter, m_currTangoDepth, m_depthPoints);
                m_debugText = "Recording Session: " + m_sessionTimestamp + " Points: " + m_currTangoDepth.m_timestamp;
            }
            ClearQuads();
            SetTransformUsingPose(transform, m_poseAtDepthTimestamp);

            if(m_playbackData) {
                m_mainCamera.transform.position = transform.position;
                m_mainCamera.transform.rotation = transform.rotation;
            }

            m_positionHistory.Add(transform.position);
            DrawDebugLines();
            float start = Time.realtimeSinceStartup;

            for (int i = 0; i < m_insertionCount; i++)
            {
                if(i > m_currTangoDepth.m_pointCount)
                {
                    break;
                }
                //randomly sub sample
                int index = i;
                //need to be more graceful than this, does not behave continuously
                if(m_insertionCount < m_currTangoDepth.m_pointCount);
                    index = UnityEngine.Random.Range(0,m_currTangoDepth.m_pointCount);

                Vector3 p = new Vector3(m_depthPoints[3*index],-m_depthPoints[3*index+1],m_depthPoints[3*index+2]);

                Vector3 tp = transform.TransformPoint(p);

                float mag = Vector3.Magnitude(p);
                //less weight for things farther away, because of noise
                if (m_recordData)
                {
                    CreateQuad(p);
                }
                else
                {
                    m_occupancyManager.InsertPoint (tp, transform.forward, 0.2f / (mag + 1.0f));
                }
            }

            float stop = Time.realtimeSinceStartup;
            m_occupancyManager.InsertionTime = m_occupancyManager.InsertionTime * m_occupancyManager.Smoothing + (1.0f - m_occupancyManager.Smoothing) * (stop - start);
            m_occupancyManager.QueueDirtyMeshesForRegeneration ();
            m_isDirty = false;
        }
    }

    private void ClearQuads()
    {
        for (int i = 0; i < m_insertionCount; i++) {
            m_quads[i].SetActive(false);
        }
        m_quadIndex = 0;
    }

    private void CreateQuad(Vector3 p)
    {
        m_quads[m_quadIndex].transform.localPosition = p;
        m_quads[m_quadIndex].SetActive (true);
        m_quadIndex++;
    }

    private void SetTransformUsingPose(Transform xform, TangoPoseData pose)
    {
        xform.position = new Vector3((float)pose.translation [0],
                                                    (float)pose.translation [2],
                                                    (float)pose.translation [1]) + m_initCameraPosition;
        
        Quaternion quat = new Quaternion((float)pose.orientation [0],
                                                       (float)pose.orientation [2], // these rotation values are swapped on purpose
                                                       (float)pose.orientation [1],
                                                       (float)pose.orientation [3]);

        Quaternion axisFix = Quaternion.Euler(-quat.eulerAngles.x,
                                              -quat.eulerAngles.z,
                                              quat.eulerAngles.y);

        //should query API for depth camera extrinsics
        Quaternion extrinsics = Quaternion.Euler(-12.0f,0,0);

        xform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f) * axisFix*extrinsics;
    }


    public void Reset()
    {
        ClearQuads ();
        m_positionHistory.Clear ();
        m_occupancyManager.Clear ();
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
            m_occupancyManager.depthPointCount = tangoDepth.m_pointCount;
            for (int i = 0; i < tangoDepth.m_pointCount; i+=3)
            {
                m_depthPoints[3 * i] = tangoDepth.m_points[i * 3];
                m_depthPoints[3 * i + 1] = tangoDepth.m_points[i * 3 + 1];
                m_depthPoints[3 * i + 2] = tangoDepth.m_points[i * 3 + 2];
            }
            m_currTangoDepth.m_timestamp = tangoDepth.m_timestamp;
            m_currTangoDepth.m_pointCount = tangoDepth.m_pointCount;

            PoseProvider.GetPoseAtTime(m_poseAtDepthTimestamp, m_currTangoDepth.m_timestamp,m_coordinatePair);

            m_isDirty = true;
        }
        return;
    }    
    

    public void WritePoseToFile(BinaryWriter writer, TangoPoseData pose) {
        if(writer == null)
        {
            return;
        }
        
        writer.Write("poseframe\n");
        writer.Write(pose.timestamp+"\n");
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
    
    
    public int ReadPoseFromFile(BinaryReader reader, ref TangoPoseData pose)
    {
        if(reader == null)
        {
            return -1;
        }
        
        string frameMarker;
        try {
            frameMarker = reader.ReadString();
        } catch (EndOfStreamException x) {
            reader.BaseStream.Position = 0;
            Reset();
            print ("Restarting log file: " + x.ToString());
            frameMarker = reader.ReadString();
        }
        
        if(frameMarker.CompareTo("poseframe\n") != 0)
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
    
    public void WriteDepthToFile(BinaryWriter writer, TangoUnityDepth depth, float[] pointData)
    {
        if(writer == null)
        {
            return;
        }

        writer.Write("depthframe\n");
        writer.Write(depth.m_timestamp+"\n");
        writer.Write(depth.m_pointCount+"\n");
        
        for(int i = 0; i < depth.m_pointCount; i++)
        {
            writer.Write(pointData[3*i]);
            writer.Write(pointData[3*i+1]);
            writer.Write(pointData[3*i+2]);
        }
        writer.Flush();
    }
    
    
    public int ReadDepthFromFile(BinaryReader reader, ref TangoUnityDepth depthFrame, ref float[] points)
    {
        string frameMarker;
        try {
            frameMarker = reader.ReadString();
        } catch (EndOfStreamException x) {
            reader.BaseStream.Position = 0;
            Reset();

            print ("Restating log file: " + x.ToString());
            frameMarker = reader.ReadString();
        }
        
        if(frameMarker.CompareTo("depthframe\n") != 0) {
            m_debugText = "Failed to read depth";
            return -1;
        }
        depthFrame.m_timestamp = double.Parse(reader.ReadString());
        depthFrame.m_pointCount = int.Parse(reader.ReadString());

        //load up the data
        for(int i = 0; i < depthFrame.m_pointCount; i++)
        {
            points[3*i] = reader.ReadSingle();
            points[3*i+1] = reader.ReadSingle();
            points[3*i+2] = reader.ReadSingle();
        }
        
        return 0;
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10,200,1000,30), "Debug: " + m_debugText);
        if (!m_recordData)
        {
            if (GUI.Button (new Rect (Screen.width - 160, 120, 140, 80), "Start Record"))
            {
                m_occupancyManager.Clear();
                PrepareRecording ();
                m_recordData = true;
            }
        } 
        else
        {
            if (GUI.Button (new Rect (Screen.width - 160, 120, 140, 80), "Stop Record"))
            {
                m_recordData = false;
                m_fileWriter.Close();
                m_fileWriter = null;
                m_debugText = "Stopped Recording";
            }
        }
    }
}
