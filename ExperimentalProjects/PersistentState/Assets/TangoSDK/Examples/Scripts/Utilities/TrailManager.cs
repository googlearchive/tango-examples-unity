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
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Tango;

/// <summary>
/// Manages the trail data
/// Contains functions for saving, loading trail file.
/// </summary>
public class TrailManager : MonoBehaviour
{
    // Distance between each trail point saved, higher distance = lower accuracy
    public float m_distanceFactor = 0.5f;

    // material of the line which was recorded
    public Material m_RecordedDataMaterial;
    
    // material of the line which is being drawn
    public Material m_LiveDataMaterial;

    // List of positions you save during trail formation
    // this is public so that you can access in other scripts
    public List<Vector3> m_savePositionData;

    // flag for checking if recording is started
    private bool m_startRecording;

    // material of the line
    private Material m_lineMaterial;

    // List of game objects generated when a trail is loaded from a file
    private List<GameObject> m_trailGameObjects;

    // Stores temporary previous point to add trail points
    private Vector3 m_previousPosition;

    private TrailObject m_recordedTrail;
    private TrailObject m_liveDataTrail;

    /// <summary>
    /// Starts recording trails by setting m_startRecording to true.
    /// </summary>
    public void StartTrailBuilding()
    {
        m_previousPosition = Vector3.zero;
        m_startRecording = true;
    }

    /// <summary>
    /// Stops recording trails by setting m_startRecording to false.
    /// Calls the function to write to a file.
    /// </summary>
    /// <param name="fileName"> Name of file you want to save trail vector3 data in.</param>
    public void StopTrailBuilding(string fileName)
    {
        m_startRecording = false;
        _WriteToFile(fileName);
    }

    /// <summary>
    /// Read the file and creates reinitialize positions array.
    /// </summary>
    /// <param name="fileName"> Name of file you want to save trail vector3 data in.</param>
    /// <returns> Returns true if load successful.</returns>
    public bool LoadTrailFromFile(string fileName)
    {
        char[] delimeters = 
        {
            ',',
            ')',
            '('
        };
        FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        if (fs == null)
        {
            return false;
        }
        _ReInitializeTrail();
        StreamReader fileToRead = new StreamReader(fs);
        while (!fileToRead.EndOfStream)
        {
            Vector3 position;
            string fileData = fileToRead.ReadLine();
            string[] xyzCordinate = fileData.Split(delimeters);

            // xyzCordinate[0] has ' ' , reason = '(' is a delimiter too
            // so we ignore element at index 0
            position.x = float.Parse(xyzCordinate[1]); 
            position.y = float.Parse(xyzCordinate[2]);
            position.z = float.Parse(xyzCordinate[3]);
            m_savePositionData.Add(position);
        }
        return true;
    }
    
    /// <summary>
    /// Clean up / reinitialize function.
    /// </summary>
    public void _ReInitializeTrail()
    {
        foreach (GameObject trailObj in m_trailGameObjects)
        {
            Destroy(trailObj);
        }
        m_savePositionData.Clear();
        m_savePositionData = new List<Vector3>();
        m_trailGameObjects = new List<GameObject>();
    }

    /// <summary>
    /// Creates a trail using line renderer.
    /// </summary>
    public void CreateTrailFromList()
    {
        m_recordedTrail.ResetTrailVertex();
        m_liveDataTrail.ResetTrailVertex();
        for (int i = 0; i < m_savePositionData.Count; i++)
        {
            m_recordedTrail.AddVertexToLine(m_savePositionData[i]);
        }
    }
        
    /// <summary>
    /// Use this for initialization.
    /// </summary>
    private void Start()
    {
        m_savePositionData = new List<Vector3>();
        m_trailGameObjects = new List<GameObject>();

        m_recordedTrail = new TrailObject(m_RecordedDataMaterial);
        m_liveDataTrail = new TrailObject(m_LiveDataMaterial);
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update()
    {
        // checking these two m_TangoApplication.GetComponent<Tango.VIOProvider>() != null
//        if (m_startRecording && TangoApplication.Instance.IsInitialized())
//        {
////            VIOProvider.VIOStatus vioStatus = new VIOProvider.VIOStatus();
////            VIOProvider.GetLatestPose(ref vioStatus);
////
////            // check if distance has been changed more than the distanceFactor
////            if (Vector3.Distance(m_previousPosition, vioStatus.translation) > m_distanceFactor)
////            {
////                m_previousPosition = vioStatus.translation;
////                m_liveDataTrail.AddVertexToLine(m_previousPosition);
////                m_savePositionData.Add(m_previousPosition);
////            }
//        }
    }
     
    /// <summary>
    /// Writes vector3 trail to a file. 
    /// </summary>
    /// <param name="fileName"> Name of file you want to save trail vector3 data in.</param>
    private void _WriteToFile(string fileName)
    {
        FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);

        // if there is an error creating a file
        if (fs == null)
        {
            return;
        }
        StreamWriter textRecorder = new StreamWriter(fs);
        for (int i = 0; i < m_savePositionData.Count; i++)
        {
            textRecorder.WriteLine(m_savePositionData[i].ToString());
        }
        textRecorder.Close();
        fs.Close();
    }
}

/// <summary>
/// Encapsulates trail related parameters.
/// </summary>
public class TrailObject
{
    public GameObject m_gameObj;
    public LineRenderer m_lineRendererRef;
    public int m_vertexCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrailObject"/> class.
    /// </summary>
    /// <param name="lineMaterial">Line material.</param>
    public TrailObject(Material lineMaterial)
    {
        m_gameObj = new GameObject();
        m_lineRendererRef = m_gameObj.AddComponent<LineRenderer>();
        m_vertexCount = 0;
        m_lineRendererRef.SetVertexCount(m_vertexCount);
        m_lineRendererRef.sharedMaterial = lineMaterial;
        m_lineRendererRef.useWorldSpace = false;
        float lineSize = 0.1f;
        m_lineRendererRef.SetWidth(lineSize, lineSize);
    }

    /// <summary>
    /// Resets the trail vertex.
    /// </summary>
    public void ResetTrailVertex()
    {
        m_lineRendererRef.SetVertexCount(0);
    }

    /// <summary>
    /// Adds the vertex to line renderer.
    /// </summary>
    /// <param name="newPos">New position.</param>
    public void AddVertexToLine(Vector3 newPos)
    {
        m_vertexCount++;
        m_lineRendererRef.SetVertexCount(m_vertexCount);
        m_lineRendererRef.SetPosition(m_vertexCount - 1, newPos);
    }
}
