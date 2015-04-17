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
using System.Collections;
using System.IO;
using System;
using Tango;
using System.Runtime.InteropServices;

public class GlobalState : MonoBehaviour {

	public static GlobalState state;
	public GameObject mainCamera;

//	private CustomTangoController cameraController;

	public string loadID = "2014_10_26_031617";

	[HideInInspector]
	public string applicationPath;
	[HideInInspector]
	public string debugText;
	[HideInInspector]
	public string debugText2;
	[HideInInspector]
	public string debugText3;
	[HideInInspector]
	public string debugText4;


	private BinaryWriter depthFileWriter = null;
	private BinaryReader depthFileReader = null;

	private BinaryWriter poseFileWriter = null;
	private BinaryReader poseFileReader = null;

	private bool recordData = false;
	[HideInInspector]
	public bool playbackData = false;

	public int renderCount = 1000;

	float[] loadedRawPoints = new float[20000*3]; //get dynamcially allocated larger if there are more points
	TangoXYZij depthFrame = new TangoXYZij();
	float[] depthPoints;
	TangoPoseData currPose = new TangoPoseData();
	TangoPoseData tempPose = new TangoPoseData();

	int maximumLinePositions = 120;
	ArrayList linePositions = new ArrayList();

	string sessionTimestamp;


	void Awake() {
		if(state == null) {
			DontDestroyOnLoad(gameObject);
			state = this;
		}
		else if(state != this) {
			Destroy(gameObject);
		}



		sessionTimestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");

//		cameraController = mainCamera.GetComponent<CustomTangoController>();

		depthPoints = new float[3*renderCount];

		/*
		float quadscale = 0.02f;
		/*
		for(int i = 0; i < renderCount; i++) {
			quads[i] = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quads[i].transform.parent = mainCamera.transform;
			quads[i].transform.localScale = new Vector3(quadscale, quadscale, quadscale);
			quads[i].renderer.material.color = new Color(1,0,0);
			quads[i].renderer.material.shader = Shader.Find("Custom/UnlitColor");
		}*/

		if(recordData) {
			CreateLogFiles();
		} 

		if(playbackData) {
			LoadLogFiles();
			StepLoad();
		}


	}

	// Use this for initialization
	void Start () {
	}



	public Color DistanceToColor(float d) {

		float sectionSize = 1;
		if(d < sectionSize) {
			return new Color(1, d/sectionSize, 0);
		}
		d -= sectionSize;
		if(d < sectionSize) {
			return new Color(1-d/sectionSize, 1, 0);
		}
		d -= sectionSize;
		if(d < sectionSize) {
			return new Color(0, 1, d/sectionSize);
		}

		d -= sectionSize;
		if(d < sectionSize) {
			return new Color(0, 1-d/sectionSize,1);
		}
		return new Color(0,0,1);

	}

	public void CreateLogFiles()
	{
		string depthFilename = sessionTimestamp+"_depth.dat";
		depthFileWriter = new BinaryWriter(File.Open(Application.persistentDataPath + "/" + depthFilename, FileMode.Create));

		string poseFilename = sessionTimestamp+"_pose.dat";
		poseFileWriter = new BinaryWriter(File.Open(Application.persistentDataPath + "/" + poseFilename, FileMode.Create));

		debugText = "Saving to: " + depthFilename + " " + depthFileWriter.ToString();
	}

	public void WritePoseToLogFile(TangoPoseData pose) {
		if(poseFileWriter == null)
			return;

		poseFileWriter.Write("poseframe\n");
		poseFileWriter.Write(pose.timestamp+"\n");
		poseFileWriter.Write((int)pose.framePair.baseFrame);
		poseFileWriter.Write((int)pose.framePair.targetFrame);
		poseFileWriter.Write((int)pose.status_code);
		poseFileWriter.Write(pose.translation[0]);
		poseFileWriter.Write(pose.translation[1]);
		poseFileWriter.Write(pose.translation[2]);
		poseFileWriter.Write(pose.orientation[0]);
		poseFileWriter.Write(pose.orientation[1]);
		poseFileWriter.Write(pose.orientation[2]);
		poseFileWriter.Write(pose.orientation[3]);
		poseFileWriter.Flush();
	}


	public int LoadPoseFromLogFile(BinaryReader reader, ref TangoPoseData pose) {

		if(reader == null)
			return -1;

		string frameMarker;
		try {
			frameMarker = reader.ReadString();
		} catch (EndOfStreamException x) {
			reader.BaseStream.Position = 0;
			print ("Restarting log file: " + x.ToString());
			frameMarker = reader.ReadString();
		}

		if(frameMarker.CompareTo("poseframe\n") != 0) {
			debugText = "Failed to load";
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


	public void WriteDepthToLogFile(TangoXYZij depth, float[] pointData, int[] indices, int rows, int cols) {

		if(!playbackData)
			UpdatePoints(pointData, depth.xyz_count);

		if(depthFileWriter == null)
			return;
		debugText2 = "Writing Depth: " + depth.xyz_count.ToString();

		depthFileWriter.Write("depthframe\n");
		depthFileWriter.Write(depth.timestamp+"\n");
		depthFileWriter.Write(depth.xyz_count+"\n");

		for(int i = 0; i < depth.xyz_count; i++) {
			depthFileWriter.Write(pointData[3*i]);
			depthFileWriter.Write(pointData[3*i+1]);
			depthFileWriter.Write(pointData[3*i+2]);
		}
		depthFileWriter.Flush();
	}


	public int LoadDepthFromLogFile(BinaryReader reader, ref TangoXYZij depthFrame, ref float[] points) {
		string frameMarker;
		try {
			frameMarker = reader.ReadString();
		} catch (EndOfStreamException x) {
			reader.BaseStream.Position = 0;
			print ("Restating log file: " + x.ToString());
			frameMarker = reader.ReadString();
		}

		if(frameMarker.CompareTo("depthframe\n") != 0) {
			debugText = "Failed to load";
			return -1;
		}
		depthFrame.timestamp = double.Parse(reader.ReadString());
		depthFrame.xyz_count = int.Parse(reader.ReadString());
		debugText2 = depthFrame.timestamp.ToString();
		debugText3 = depthFrame.xyz_count.ToString();


		//if we are too small increase the memory array with some room to spare
		if(depthFrame.xyz_count*3 > loadedRawPoints.Length)
			loadedRawPoints = new float[2*3*depthFrame.xyz_count];

		//load up the data
		for(int i = 0; i < depthFrame.xyz_count; i++) {
			loadedRawPoints[3*i] = reader.ReadSingle();
			loadedRawPoints[3*i+1] = reader.ReadSingle();
			loadedRawPoints[3*i+2] = reader.ReadSingle();
		}

		//randomly subsample
		for(int i = 0; i < renderCount; i++) {
			int srcIndex = UnityEngine.Random.Range (0,depthFrame.xyz_count-1);
			points[3*i] = loadedRawPoints[3*srcIndex];
			points[3*i+1] = loadedRawPoints[3*srcIndex+1];
			points[3*i+2] = loadedRawPoints[3*srcIndex+2];
		}
		return 0;
	}

	public void LoadLogFiles() {

		string depthFilename = loadID + "_depth.dat";
		depthFileReader = new BinaryReader(File.Open(Application.persistentDataPath + "/" + depthFilename, FileMode.Open));
		string poseFilename = loadID + "_pose.dat";
		poseFileReader = new BinaryReader(File.Open(Application.persistentDataPath + "/" + poseFilename, FileMode.Open));
		debugText = "Loading from: " + Application.persistentDataPath + "/" + poseFilename;
	}

	
	public void LoadLogFile(string filename, ref TangoXYZij depthFrame, ref float[] points) {
		depthFileReader = new BinaryReader(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Open));
		debugText = "Loading from: " + filename + " " + depthFileReader.ToString();
		LoadDepthFromLogFile(depthFileReader, ref depthFrame, ref points);
	}

	public void UpdatePoints(float[] points, int size) {
		for( int i = 0; i < size; i++) {
//			Vector3 pos = mainCamera.transform.TransformPoint(new Vector3(points[3*i],-points[3*i+1],points[3*i+2]));
//			Vector3 ray = (mainCamera.transform.position-pos).normalized;
//			dynamicMesh.Insert(pos, ray);
		}
	
	}

	void OnApplicationQuit() {
		if(depthFileWriter != null)
			depthFileWriter.Close();

		if(depthFileReader != null)
			depthFileReader.Close();
	}

	// Update is called once per frame
	void Update () {

		applicationPath = Application.persistentDataPath;
		if(playbackData) {
			StepLoad();
		}

		if(Input.GetKeyDown(KeyCode.Space))
			playbackData = !playbackData;


	}

	void StepLoad() {

		if(depthFileReader != null) {
			LoadDepthFromLogFile(depthFileReader,ref depthFrame, ref depthPoints);
		}

		if(poseFileReader != null) {
			//load up to 2 seconds of pose events until we are close are past the depth timestamp
			for(int i = 0 ; i < 60; i++) {
				currPose = new TangoPoseData();
				LoadPoseFromLogFile(poseFileReader, ref currPose);
				if(currPose.timestamp > depthFrame.timestamp)
					break;				
			}

//			tempPose = cameraController.InterpolateTangoPose(prevPose, currPose, depthFrame.timestamp,0,4);
//			cameraController.UpdateUsingTangoPose(tempPose);
//			cameraController.transform.RotateAround(cameraController.transform.position, cameraController.transform.right,-13);

			Vector3 p = mainCamera.transform.position;
			p.y = 0.01f;
			linePositions.Add(p);
			if(linePositions.Count >= maximumLinePositions)
				linePositions.RemoveAt(0);

		}

		UpdatePoints(depthPoints, renderCount);

	}
	
	void OnGUI()
	{
		GUI.Label(new Rect(10,50,1000,30), "FilePath: " + applicationPath);
		GUI.Label(new Rect(10,70,1000,30), "PoseTimestamp: " + tempPose.timestamp);
		GUI.Label(new Rect(10,90,1000,30), "DepthTimestamp: " + depthFrame.timestamp);
		GUI.Label(new Rect(10,110,1000,30), "Debug: " + debugText);
		GUI.Label(new Rect(10,130,1000,30), "Debug: " + debugText2);
		GUI.Label(new Rect(10,150,1000,30), "Debug: " + debugText3);
		GUI.Label(new Rect(10,170,1000,30), "Debug: " + debugText4);

		if(!playbackData && !recordData) {
			if(GUI.Button(new Rect(50, Screen.height-200, 200,100),"Load Last Session")) {
				playbackData = true;
				LoadLogFiles();
			}
			if(GUI.Button(new Rect(Screen.width - 250, Screen.height-200, 200,100),"Start Record")) {
				recordData = true;
				CreateLogFiles();
			}
		}
	}
}
