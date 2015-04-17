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
	public GameObject occupancyManagerObject;
	private DynamicMeshManager occupancyManager;

	public Camera mainCamera;
	public int insertionCount = 10;

	// Mesh data.
	private bool isDirty;
	private int maxPoints = 20000;
	private float[] depthPoints;
	private TangoXYZij currXYZij = new TangoXYZij();
	private TangoPoseData poseAtDepthTimestamp = new TangoPoseData();

	public string loadSessionID = "2014_10_26_031617";

	string sessionTimestamp = "None";
	private BinaryWriter fileWriter = null;
	private BinaryReader fileReader = null;
		
	GameObject[] quads = null;
	int quadIndex = 0;
	
	List<Vector3> positionHistory = new List<Vector3>();


	Vector3 initCameraPosition = new Vector3();
	
	string debugText;
	public bool recordData = false;
	public bool playbackData = false;
	private bool pause = false;
	private bool step = false;

	TangoCoordinateFramePair coordinatePair;
	private TangoApplication m_tangoApplication;

	/// <summary>
	/// Use this for initialization.
	/// </summary>
	public void Start() 
	{
		occupancyManager = occupancyManagerObject.GetComponent<DynamicMeshManager> ();

		depthPoints = new float[maxPoints * 3];		
		isDirty = false;


		initCameraPosition = mainCamera.transform.position;

		coordinatePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
		coordinatePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;//FIX should be depth sensor

		quads = new GameObject[insertionCount];
		float size = 0.02f;
		for (int i =0; i < insertionCount; i++) {
			quads[i] = (GameObject)GameObject.CreatePrimitive (PrimitiveType.Cube);
			quads[i].transform.localScale = new Vector3(size, size, size);
			quads[i].transform.parent = transform;
		}


#if UNITY_ANDROID && !UNITY_EDITOR
		if (recordData) {
			PrepareRecording();
		}
#endif			
		if (playbackData) {
			recordData = false;
			string filename = loadSessionID +".dat";
			fileReader = new BinaryReader(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Open));
			debugText = "Loading from: " + filename + " " + fileReader.ToString();
		}

		m_tangoApplication = FindObjectOfType<TangoApplication>();
		m_tangoApplication.Register(this);
	}

	void PrepareRecording() {
		sessionTimestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
		string filename = sessionTimestamp+".dat";
		if(fileWriter != null) {
			fileWriter.Close();
			fileWriter = null;
		}
		fileWriter = new BinaryWriter(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Create));
		debugText = "Saving to: " + filename + " " + fileWriter.ToString();
	}

	void DrawDebugLines() {
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
			pause = !pause;

		if (Input.GetKeyDown (KeyCode.Slash))
			step = true;

		//history
		for (int i =0; i < positionHistory.Count-1; i++) {
			Debug.DrawLine(positionHistory[i], positionHistory[i+1], Color.gray);
		}

		if (pause && !step) {
			DrawDebugLines();
			return;
		}

		step = false;

		if (playbackData) {
			ReadPoseFromFile(fileReader,ref poseAtDepthTimestamp);
			ReadDepthFromFile(fileReader, ref currXYZij, ref depthPoints);
			isDirty = true;
		}

		if (isDirty)
		{
			if(recordData) {
				WritePoseToFile(fileWriter, poseAtDepthTimestamp);
				WriteDepthToFile(fileWriter, currXYZij, depthPoints);
				debugText = "Recording Session: " + sessionTimestamp + " Points: " + currXYZij.xyz_count;
			}
			ClearQuads();
			//debugText = currXYZij.timestamp.ToString("F3");
			SetTransformUsingPose(transform, poseAtDepthTimestamp);

			if(playbackData) {
				mainCamera.transform.position = transform.position;
				mainCamera.transform.rotation = transform.rotation;
			}

			positionHistory.Add(transform.position);
			DrawDebugLines();
			float start = Time.realtimeSinceStartup;

			for (int i = 0; i < insertionCount; i++) {
				if(i > currXYZij.xyz_count)
					break;
				//randomly sub sample
				int index = i;
				//need to be more graceful than this, does not behave continuously
				if(insertionCount < currXYZij.xyz_count);
					index = UnityEngine.Random.Range(0,currXYZij.xyz_count);

				Vector3 p = new Vector3(depthPoints[3*index],-depthPoints[3*index+1],depthPoints[3*index+2]);

				Vector3 tp = transform.TransformPoint(p);

				float mag = Vector3.Magnitude(p);
				//less weight for things farther away, because of noise
				if(recordData)
					CreateQuad(p);
				else
					occupancyManager.InsertPoint (tp,transform.forward, 0.2f/(mag + 1.0f));
			}

			float stop = Time.realtimeSinceStartup;
			occupancyManager.InsertionTime = occupancyManager.InsertionTime*occupancyManager.Smoothing + (1.0f-occupancyManager.Smoothing)*(stop - start);
			occupancyManager.QueueDirtyMeshesForRegeneration ();
			isDirty = false;
		}
	}

	private void ClearQuads() {
		for (int i = 0; i < insertionCount; i++) {
			quads[i].SetActive(false);
		}
		quadIndex = 0;
	}

	private void CreateQuad(Vector3 p) {
		quads[quadIndex].transform.localPosition = p;
		//quads[quadIndex].transform.rotation = Quaternion.LookRotation(transform.position);

		quads[quadIndex].SetActive (true);
		quadIndex++;
	}

	private void SetTransformUsingPose(Transform xform, TangoPoseData pose){
		xform.position = new Vector3((float)pose.translation [0],
		                                            (float)pose.translation [2],
		                                            (float)pose.translation [1]) + initCameraPosition;
		
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


	public void Reset() {
		ClearQuads ();
		positionHistory.Clear ();
		occupancyManager.Clear ();
	}
	/// <summary>
	/// Callback that gets called when depth is available
	/// from the Tango Service.
	/// DO NOT USE THE UNITY API FROM INSIDE THIS FUNCTION!
	/// </summary>
	/// <param name="callbackContext">Callback context.</param>
	/// <param name="xyzij">Xyzij.</param>
	public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
	{
		// Fill in the data to draw the point cloud.
		if (tangoDepth != null)
		{
			occupancyManager.depthPointCount = tangoDepth.m_pointCount;
			for (int i = 0; i < tangoDepth.m_pointCount; i+=3) {
				depthPoints[3*i] = tangoDepth.m_vertices[i].x;
				depthPoints[3*i + 1] = tangoDepth.m_vertices[i].y;
				depthPoints[3*i + 2] = tangoDepth.m_vertices[i].z;
			}
			currXYZij.timestamp = tangoDepth.m_timestamp;
			currXYZij.xyz_count = tangoDepth.m_pointCount;

			PoseProvider.GetPoseAtTime(poseAtDepthTimestamp, currXYZij.timestamp,coordinatePair);

			//minimize things happening in the callback
			isDirty = true;
		}
		return;
	}	

	

	public void WritePoseToFile(BinaryWriter writer, TangoPoseData pose) {
		if(writer == null)
			return;
		
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
	
	
	public int ReadPoseFromFile(BinaryReader reader, ref TangoPoseData pose) {
		
		if(reader == null)
			return -1;
		
		string frameMarker;
		try {
			frameMarker = reader.ReadString();
		} catch (EndOfStreamException x) {
			reader.BaseStream.Position = 0;
			Reset();
			print ("Restarting log file: " + x.ToString());
			frameMarker = reader.ReadString();
		}
		
		if(frameMarker.CompareTo("poseframe\n") != 0) {
			debugText = "Failed to read pose";
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
	
	
	public void WriteDepthToFile(BinaryWriter writer, TangoXYZij depth, float[] pointData) {

		if(writer == null)
			return;

		writer.Write("depthframe\n");
		writer.Write(depth.timestamp+"\n");
		writer.Write(depth.xyz_count+"\n");
		
		for(int i = 0; i < depth.xyz_count; i++) {
			writer.Write(pointData[3*i]);
			writer.Write(pointData[3*i+1]);
			writer.Write(pointData[3*i+2]);
		}
		writer.Flush();
	}
	
	
	public int ReadDepthFromFile(BinaryReader reader, ref TangoXYZij depthFrame, ref float[] points) {
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
			debugText = "Failed to read depth";
			return -1;
		}
		depthFrame.timestamp = double.Parse(reader.ReadString());
		depthFrame.xyz_count = int.Parse(reader.ReadString());

		//load up the data
		for(int i = 0; i < depthFrame.xyz_count; i++) {
			points[3*i] = reader.ReadSingle();
			points[3*i+1] = reader.ReadSingle();
			points[3*i+2] = reader.ReadSingle();
		}
		
		return 0;
	}
	
	void OnGUI() {
		GUI.Label(new Rect(10,200,1000,30), "Debug: " + debugText);

		if (!recordData) {
			if (GUI.Button (new Rect (Screen.width - 160, 120, 140, 80), "Start Record")) {
				occupancyManager.Clear();
				PrepareRecording ();
				recordData = true;
			}
		} else {
			if (GUI.Button (new Rect (Screen.width - 160, 120, 140, 80), "Stop Record")) {
				recordData = false;
				fileWriter.Close();
				fileWriter = null;
				debugText = "Stopped Recording";

			}
		}


	}

}     