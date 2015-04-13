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

//uses a spatial hash key and a binary tree to store sparse volumetric data
public class VolumetricHashStorage {

	private VolumetricHashStorage leftHashTree = null;
	private VolumetricHashStorage rightHashTree = null;
	private VolumetricHashStorage root = null;

	private GameObject meshingCube = null;
	private DynamicMeshVolume grid = null;
	private int[] cellIndex = new int[3];

	private int key = 0;
	private int dimension = 1000;//supports +/- 500m from origin along each axis

	public VolumetricHashStorage(VolumetricHashStorage parent, int hashkey) {
		if (parent == null)
			root = this;
		else
			root = parent;

		key = hashkey;
	}
	
	public DynamicMeshVolume Grid {
		get {
			return grid;
		}
	}
	
	private void Instantiate(int hashkey, GameObject prefab, Transform parent, int gridCellDivisions) {
		int x, y, z;
		GetReverseHashKey(hashkey, out x, out y, out z);
		meshingCube = (GameObject)GameObject.Instantiate (prefab);
		meshingCube.transform.position = new Vector3 (x, y, z);
		meshingCube.transform.parent = parent;
		grid = meshingCube.GetComponent<DynamicMeshVolume> ();
		grid.SetProperties (gridCellDivisions);
		grid.Key = hashkey;
	}

	//IEnumerator and IEnumerable require these methods.
	public IEnumerable<VolumetricHashStorage> GetEnumerable()
	{
		if (leftHashTree != null)
			foreach(VolumetricHashStorage n in leftHashTree.GetEnumerable())
				yield return n;
		yield return this;
		if (rightHashTree != null)
			foreach (VolumetricHashStorage n in rightHashTree.GetEnumerable())
				yield return n;
	}

	//hash function is simply a floor function of the x,y,z coordinates
	public int GetHashKey(Vector3 p) {
		return (int)Mathf.Floor (p.x) + (int)(dimension * Mathf.Floor (p.y)) + (int)(dimension*dimension * Mathf.Floor (p.z));
	}

	private void GetReverseHashKey(int index, out int x, out int y, out int z) {
		int flipLimit = dimension / 2;
		int temp = index;
	
		x = temp % dimension;
		if (x > flipLimit) //x is negative, but opposite sign of y
			x = x - dimension;
		if (x < -flipLimit) //x is positive, but opposite sign of y
			x = dimension + x;
		temp -= x;
		temp /= dimension;

		y = temp % dimension;
		if (y > flipLimit)
			y = y - dimension;//y is negative, but opposite sign of z
		if (y < -flipLimit)
			y = dimension + y;//y is positive, but opposite sign of z

		temp -= y;
		z = temp/dimension;
	}

	public float InsertPoint(Vector3 p, Vector3 n, float weight, GameObject prefab, Transform parent, int cellDivisions) {
		int hashKey = GetHashKey(p);
		return InsertPoint (hashKey, p, n, weight, prefab, parent, cellDivisions);
	}

	private float InsertPoint(int hashkey, Vector3 p, Vector3 n, float weight, GameObject prefab, Transform parent, int cellDivisions) {

		if (key == hashkey) {

			if(meshingCube == null) {
				Instantiate(hashkey, prefab, parent, cellDivisions);
			}

			if(grid == null)
				grid = meshingCube.GetComponent<DynamicMeshVolume> ();

			//adjust weight of mutiple voxels along observation ray
			float result = grid.InsertPoint(p, n, weight, ref cellIndex);
			Vector3 closerPoint = p-n*grid.GridCellSize.x;
			Vector3 furtherPoint = p+n*grid.GridCellSize.x;
			//voxel was inside the surface, back out one, and insert in the next closest voxel
			if(result > 0)
				grid.InsertPoint(closerPoint, p, n, weight);
			else
				grid.InsertPoint(furtherPoint, p, n, weight);


			if(cellIndex[0] == 0) {
				int neighborHashKey = hashkey - 1;
				result = root.InsertPoint(neighborHashKey, p,n,weight,prefab,parent,cellDivisions);
			}
			if(cellIndex[1] == 0) {
				int neighborHashKey = hashkey - dimension;
				result = root.InsertPoint(neighborHashKey, p,n,weight,prefab,parent,cellDivisions);
			}
			if(cellIndex[2] == 0) {
				int neighborHashKey = hashkey - dimension*dimension;
				result = root.InsertPoint(neighborHashKey, p,n,weight,prefab,parent,cellDivisions);
			}

			return result;
		}

		if(hashkey < key) {
			if(leftHashTree == null)
				leftHashTree = new VolumetricHashStorage(root, hashkey);
			return leftHashTree.InsertPoint(hashkey, p, n, weight, prefab, parent, cellDivisions);
		} else {
			if(rightHashTree == null)
				rightHashTree = new VolumetricHashStorage(root, hashkey);
			return rightHashTree.InsertPoint(hashkey, p, n, weight, prefab, parent, cellDivisions);
		}		
	}

	public VolumetricHashStorage Query(int hashKey) {
		if (hashKey == key) {
			return this;
		}
		if(hashKey < key) {
			if(leftHashTree != null)
				return leftHashTree.Query(hashKey);
		} else {
			if(rightHashTree != null)
				return rightHashTree.Query(hashKey);
		}
		return null;
	}
	
	public void Clear() {
		if (leftHashTree != null) {
			leftHashTree.Clear ();
			leftHashTree = null;
		}
		if (rightHashTree != null) {
			rightHashTree.Clear ();
			rightHashTree = null;
		}

		if (grid != null) {
			grid.Clear ();
		}

		if (meshingCube != null) {
			GameObject.DestroyImmediate (meshingCube);
			meshingCube = null;
		}
	}
	
	public void ComputeStats(ref int vertCount, ref int triangleCount, ref int nodeCount) {
		if (leftHashTree != null)
			leftHashTree.ComputeStats(ref vertCount, ref triangleCount, ref nodeCount);
		if (grid != null) {
			vertCount += grid.Vertices.Count;
			triangleCount += grid.Triangles.Count;
		}
		nodeCount++;

		if (rightHashTree != null)
			rightHashTree.ComputeStats(ref vertCount, ref triangleCount, ref nodeCount);
		}

	public void UpdateMeshes() {
		if (leftHashTree != null)
			leftHashTree.UpdateMeshes();
		if (grid != null)
			if(grid.IsDirty)
				grid.RegenerateMesh ();
		if (rightHashTree != null)
			rightHashTree.UpdateMeshes();
		return;
	}

	public void Draw() {
		if (leftHashTree != null)
			leftHashTree.Draw();
		if (grid != null) {
			grid.Draw();
		}
		if (rightHashTree != null)
			rightHashTree.Draw();
	}
}

public class DynamicMeshManager : MonoBehaviour {

	public GameObject meshingCubePrefab;
	public int gridCellDivisions = 10;
	public float meshingTimeBudgetMS = 10;

	private int totalVerts = 0;
	private int totalTriangles = 0;
	private int insertCount = 0;
	private int nodeCount = 0;
	private bool clearing = false;

	private Queue regenerationQueue = new Queue ();

	private VolumetricHashStorage dynamicMeshStorage = new VolumetricHashStorage(null, 0);
	private float remeshingTime = 0;
	private int remeshingCount = 5;
	private float insertionTime = 0;
	private float smoothing = 0.97f;
	private float lastDequeTime = 0;
	private float lastUpdateTime = 0;

	private DynamicMeshVolume selectedVolume = null;
	
	public Camera mainCamera;
	public GameObject hitCursor;

	[HideInInspector]
	public int depthPointCount = 0;
	
	// Use this for initialization
	void Start () {
	}
	
	public void InsertPoint(Vector3 p, Vector3 n, float weight) {
		if (clearing)
			return;
		dynamicMeshStorage.InsertPoint(p,n,weight, meshingCubePrefab, transform, gridCellDivisions);
		insertCount++;
	}

	public float Smoothing {
		get {
			return smoothing;
		}
		set {
			smoothing = value;
		}
	}

	public float InsertionTime {
		get {
			return insertionTime;
		}
		set {
			insertionTime = value;
		}
	}

	public void QueueDirtyMeshesForRegeneration() {
		if (clearing)
			return;
		//enque dirty meshes
		int count = 0;
		foreach (VolumetricHashStorage o in dynamicMeshStorage.GetEnumerable()) {
			count += 1;
			if(o.Grid == null)
				continue;

			if(o.Grid.IsDirty) {
				if(!regenerationQueue.Contains(o.Grid))
					regenerationQueue.Enqueue(o.Grid);
			}
		}
	}

	public void UpdateStats() {
		totalVerts = 0;
		totalTriangles = 0;
		nodeCount = 0;
		dynamicMeshStorage.ComputeStats (ref totalVerts, ref totalTriangles, ref nodeCount);
	}

	public void Clear() {
		//i think this causes a thread contention because we are updating the mesh in the Update call
		clearing = true;
		insertCount = 0;
		regenerationQueue.Clear ();
		dynamicMeshStorage.Clear ();	
		clearing = false;
	}

	void OnGUI()
	{
		GUI.Label(new Rect(10,20,1000,30), "Persistent Path: " + Application.persistentDataPath);
		GUI.Label(new Rect(10,40,1000,30), "Total Verts/Triangles: " + totalVerts + "/" + totalTriangles + " Nodes: " + nodeCount + " UpdateQueue:" + regenerationQueue.Count);
		GUI.Label(new Rect(10,60,1000,30), "Insert Count: " + insertCount);
		GUI.Label(new Rect(10,80,1000,30), "RemeshingTime: " + remeshingTime.ToString("F6") + " Remeshing Count: " + remeshingCount);
		GUI.Label(new Rect(10,100,1000,30), "InsertionTime: " + insertionTime.ToString("F6"));
		GUI.Label(new Rect(10,120,1000,30), "Last Deque Time: " + lastDequeTime.ToString("F6"));
		GUI.Label(new Rect(10,140,1000,30), "Last Update Time2: " + lastUpdateTime.ToString("F6"));
		GUI.Label(new Rect(10,160,1000,30), "Depth Points: " + depthPointCount);

		if (GUI.Button (new Rect (Screen.width - 160, 20, 140, 80), "Clear")) {
			Clear();
		}
	}
	
	// Update is called once per frame
	void Update () {
		lastUpdateTime = Time.realtimeSinceStartup;

		//update 1 cell in the top of queue
		if (remeshingCount < 1)
			remeshingCount = 1;
		
		for(int i = 0; i < remeshingCount; i++) {
			if(regenerationQueue.Count == 0)
				break;
			lastDequeTime = Time.realtimeSinceStartup;
			float start = Time.realtimeSinceStartup;
			((DynamicMeshVolume)regenerationQueue.Dequeue ()).RegenerateMesh();
			float stop = Time.realtimeSinceStartup;
			remeshingTime = smoothing*remeshingTime + (1.0f-smoothing)*(stop - start);
		}
		if(remeshingTime > float.Epsilon)
			remeshingCount = (int)(meshingTimeBudgetMS*0.001f / remeshingTime);
		
		UpdateStats ();

//		if (Input.GetMouseButtonDown (0)) {
//			RaycastHit hitInfo = new RaycastHit ();
//			Ray ray = mainCamera.ScreenPointToRay (new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
//			if (Physics.Raycast (ray, out hitInfo, 4,1)) {
//				hitCursor.transform.position = hitInfo.point;
//				selectedVolume = hitInfo.collider.gameObject.GetComponent<DynamicMeshVolume>();
//				int index = selectedVolume.Triangles[hitInfo.triangleIndex*3];
//
//				Debug.Log(selectedVolume + " " + selectedVolume.Uvs[index] + " " + selectedVolume.Uvs[index+1] + " " + selectedVolume.Uvs[index+2] + " query:" + hitInfo.textureCoord);				 
//			}
//		}

		if (selectedVolume != null) {
//			selectedGrid.Draw ();
//			selectedGrid.SimplifyMesh();
//			selectedGrid.SetMesh();
		}

		if (Input.GetKeyDown (KeyCode.C))
			Clear ();


	}
}
