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

public class DynamicMeshVolume : MonoBehaviour {

	public int divisions = 10;
	private int cellGridDimension = 1;
	private int marginCellCount = 2;

	private Vector3 cellSize = new Vector3 (1, 1, 1);
	private int key = 0;
	
	//temporary storage used by marching cubes
	private Voxel[] voxelBuffer = new Voxel[8];

	//used by marching cubes mesh
	MeshFilter mf = null;
//	MeshRenderer mr = null;
	List<Vector3> vertices = new List<Vector3>();
	List<Vector3> normals = new List<Vector3>();
	List<int> triangles = new List<int>(); 
	List<Vector2> uvs = new List<Vector2>();

	float isolevel = 0.0f;
	float initialVoxelValue = -1.0f;
	float maximumWeight = 100;
	float minimumCellPercentageChangeForDirty = 0.33f;
	float minimumChangeForDirty = 0.01f;//will get updated when cell size is computed

	VoxelTree voxelStorage = new VoxelTree();


	Vector2[] uvOptions = new Vector2[4];
	bool isDirty = true;

	public List<Vector3> Vertices {
		get {
			return vertices;
		}
	}

	public List<int> Triangles {
		get {
			return triangles;
		}
	}

	public List<Vector2> Uvs {
		get {
			return uvs;
		}
	}

	public int Key {
		get {
			return key;
		}
		set {
			key = value;
		}
	}
	
	void Awake() {
		for(int i = 0; i < 8; i++)
			voxelBuffer[i] = new Voxel();

		uvOptions [0] = new Vector3 (-1, -1);
		uvOptions [1] = new Vector3 (0, 1);
		uvOptions [2] = new Vector3 (1, 1);
		uvOptions [3] = new Vector3 (0, 0);

		mf = gameObject.GetComponent<MeshFilter> ();
//		mr = gameObject.GetComponent<MeshRenderer> ();
		
		mf.mesh = new Mesh ();
		SetProperties (divisions);
	}

	public Vector3 GridCellSize {
		get {
			return cellSize;
		}
		set {
			cellSize = value;
		}
	}

	// Use this for initialization
	void Start () {	
	}

	public void SetProperties(int divisions) {
		this.divisions = divisions;
		cellGridDimension = divisions + 2*marginCellCount;
		cellSize = transform.localScale / divisions;
		minimumChangeForDirty = cellSize.x * minimumCellPercentageChangeForDirty;
		Clear ();
	}

	public void InsertTestData(int count, float weight) {
		int[] index = new int[3];
		for (int i = 0; i < count; i++) {
			Vector3 p = new Vector3(UnityEngine.Random.Range(transform.position.x,transform.position.x+transform.localScale.x+cellSize.x),
			                        UnityEngine.Random.Range(transform.position.y,transform.position.y+transform.localScale.y+cellSize.y),
			                        UnityEngine.Random.Range(transform.position.z,transform.position.z+transform.localScale.z+cellSize.z));
			Vector3 n = new Vector3(0, 1, 0);
			
			InsertPoint(p,n,weight, ref index);
		}
	}

	public bool IsDirty {
		get {
			return isDirty;
		}
	}
	
	public int GetHashKey(int x, int y, int z) {
		return x+marginCellCount + (y+marginCellCount)*cellGridDimension + (z+marginCellCount)*cellGridDimension*cellGridDimension;
	}

	public Voxel QueryVoxel(int x, int y, int z) {
		return voxelStorage.Query(GetHashKey(x,y,z));
	}

	Voxel QueryCreateVoxel(int x, int y, int z) {
		int hashKey = GetHashKey(x,y,z);
		Voxel v = voxelStorage.Query(hashKey);
		if (v == null) {
			v = InitializeVoxel (x, y, z, cellSize, initialVoxelValue, 0);
			voxelStorage.Insert(v,hashKey);
		}
		return v;
	}
	
	Voxel InitializeVoxel(int xID, int yID, int zID, Vector3 voxelSize, float initialValue, float initialWeight) {
		Voxel v = new Voxel();
		v.size = voxelSize;
		v.value = initialValue;
		v.weight = initialWeight;
		v.normal = new Vector3 (0, 1, 0);
		v.xID = xID;
		v.yID = yID;
		v.zID = zID;

		v.anchor = new Vector3(xID*v.size.x,yID*v.size.y,zID*v.size.z) + v.size/2;
		return v;
	}
	
	float AdjustWeight(Voxel v, Vector3 p, Vector3 n, float weight) {

		if (v.weight > maximumWeight) {
			v.weight += weight; //keep accumulating, so we know when to subdivide
			return v.value;
		}

		float penetration = Vector3.Dot (n, v.anchor - p);
		v.value = (v.value * v.weight + penetration * weight) / (v.weight + weight);
		v.weight += weight;

		if (v.value < -1) {
			v.value = -1;
			v.weight = 0;
		}
		
		//		if (v.value > 1)
		//			v.value = 1;
		//		if (v.weight > maximumWeight)
		//			v.weight = maximumWeight;
		
		if(Mathf.Abs(v.lastMeshedValue - v.value) > minimumChangeForDirty)
				isDirty = true;
		return v.value;
	}
	
	public float InsertPoint(Vector3 p, Vector3 n, float weight, ref int[] index) {
		index[0] = (int)Math.Floor((p.x-transform.position.x)/cellSize.x);
		index[1] = (int)Math.Floor((p.y-transform.position.y)/cellSize.y);
		index[2] = (int)Math.Floor((p.z-transform.position.z)/cellSize.z);
		return AdjustWeight (QueryCreateVoxel(index[0], index[1], index[2]), p-transform.position, n, weight);
	}

	public float InsertPoint(Vector3 queryPoint, Vector3 p, Vector3 n, float weight) {
		int x = (int)Math.Floor ((queryPoint.x - transform.position.x) / cellSize.x);
		int y = (int)Math.Floor ((queryPoint.y - transform.position.y) / cellSize.y);
		int z = (int)Math.Floor ((queryPoint.z - transform.position.z) / cellSize.z);

		return AdjustWeight (QueryCreateVoxel (x,y,z), p - transform.position, n, weight);
	}


	public void Clear() {
		if(vertices != null)
			vertices.Clear ();
		if(triangles != null)
			triangles.Clear ();
		if (normals != null)
			normals.Clear ();
		if(uvs != null)
			uvs.Clear ();
		if(voxelStorage != null)
			voxelStorage.Clear ();
		if (mf != null) {
			if (mf.mesh != null) {
					mf.mesh.Clear ();
//					mf.mesh = null;
			}
//			mf = null;
		}
				
		isDirty = true;
	}

	void PrepareVoxels() {
		//for each voxel above ISO, making sure neighboring lower voxels exist
		foreach (VoxelTree t in voxelStorage.GetEnumerable()) {
			Voxel v = t.Voxel;
			if (v == null) {
				Debug.Log("something is wrong, VoxelTree has null voxel");
				continue;
			}

			//clear the stored triangle data used to compute voxel normal
			v.trianglesIndicies.Clear();

			//create lower padding voxels as needed, so marching cubes will work correctly
			if(v.value > isolevel) {

				//neighboring 3
				QueryCreateVoxel(v.xID-1, v.yID, v.zID);
				QueryCreateVoxel(v.xID-1, v.yID-1, v.zID);
				QueryCreateVoxel(v.xID, v.yID-1, v.zID);

				//lower four
				QueryCreateVoxel(v.xID, v.yID, v.zID-1);
				QueryCreateVoxel(v.xID-1, v.yID, v.zID-1);
				QueryCreateVoxel(v.xID-1, v.yID-1, v.zID-1);
				QueryCreateVoxel(v.xID, v.yID-1, v.zID-1);
			}
		}
	}

	void CalculateUVs(int ia, int ib, int ic) {
		//likely bug, if it winds down to the last face
		//it forces it to be valid, which will alter 
		//an existing face without checking that face
		//may not be resolvable without doing a full
		//3-color graph solver

		if(uvs[ia] == uvOptions[0]){
			if(uvs[ib] == uvOptions[0]){
				if(uvs[ic] == uvOptions[0]){
					uvs[ia] = uvOptions[1];
					uvs[ib] = uvOptions[2];
					uvs[ic] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[1]){
					uvs[ia] = uvOptions[2];
					uvs[ib] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[2]){
					uvs[ia] = uvOptions[1];
					uvs[ib] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[3]){
					uvs[ia] = uvOptions[1];
					uvs[ib] = uvOptions[2];
				}
			}
			else if(uvs[ib] == uvOptions[1]){
				if(uvs[ic] == uvOptions[0]){
					uvs[ia] = uvOptions[2];
					uvs[ic] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[2]){
					uvs[ia] = uvOptions[1];
				}
				else if(uvs[ic] == uvOptions[3]){
					uvs[ia] = uvOptions[2];
				}
			}
			else if(uvs[ib] == uvOptions[2]){
				if(uvs[ic] == uvOptions[0]){
					uvs[ia] = uvOptions[1];
					uvs[ic] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[1]){
					uvs[ia] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[3]){
					uvs[ia] = uvOptions[1];
				}
			}
			else if(uvs[ib] == uvOptions[3]){
				if(uvs[ic] == uvOptions[0]){
					uvs[ia] = uvOptions[1];
					uvs[ic] = uvOptions[2];
				}
				else if(uvs[ic] == uvOptions[1]){
					uvs[ia] = uvOptions[2];
				}
				else if(uvs[ic] == uvOptions[2]){
					uvs[ia] = uvOptions[1];
				}
			}
		}
		else if(uvs[ia] == uvOptions[1]){
			if((uvs[ib] == uvOptions[0])||(uvs[ib] == uvOptions[1])){
				if(uvs[ic] == uvOptions[0]){
					uvs[ib] = uvOptions[2];
					uvs[ic] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[2]){
					uvs[ib] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[3]){
					uvs[ib] = uvOptions[2];
				}
			}
			else if(uvs[ib] == uvOptions[2]){
				uvs[ic] = uvOptions[3];
			}
			else if(uvs[ib] == uvOptions[3]){
				uvs[ic] = uvOptions[2];
			}
		}
		else if(uvs[ia] == uvOptions[2]){
			if((uvs[ib] == uvOptions[0])||(uvs[ib] == uvOptions[2])){
				if(uvs[ic] == uvOptions[0]){
					uvs[ib] = uvOptions[1];
					uvs[ic] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[1]){
					uvs[ib] = uvOptions[3];
				}
				else if(uvs[ic] == uvOptions[3]){
					uvs[ib] = uvOptions[1];
				}
			}
			else if(uvs[ib] == uvOptions[1]){
				uvs[ic] = uvOptions[3];
			}
			else if(uvs[ib] == uvOptions[3]){
				uvs[ic] = uvOptions[1];
			}
		}
		if(uvs[ia] == uvOptions[3]){
			if((uvs[ib] == uvOptions[0])||(uvs[ib] == uvOptions[3])){
				if(uvs[ic] == uvOptions[0]){
					uvs[ib] = uvOptions[1];
					uvs[ic] = uvOptions[2];
				}
				else if(uvs[ic] == uvOptions[1]){
					uvs[ib] = uvOptions[2];
				}
				else if(uvs[ic] == uvOptions[2]){
					uvs[ib] = uvOptions[1];
				}
			}
			else if(uvs[ib] == uvOptions[1]){
				uvs[ic] = uvOptions[2];
			}
			else if(uvs[ib] == uvOptions[2]){
				uvs[ic] = uvOptions[1];
			}
		}
	}

	void ComputeVoxelNormals() {
		foreach (VoxelTree t in voxelStorage.GetEnumerable()) {
			Voxel v = t.Voxel;
			if(v.trianglesIndicies.Count == 0)
				continue;

			v.normal.Set(0,0,0);

			foreach(int index in v.trianglesIndicies)
				v.normal += normals[triangles[index]];

			v.normal /= v.trianglesIndicies.Count;
		}
	}

	int CreateMarchingCube(Voxel v) {
		if (v == null)
			return 0;

		//allows us to track if the mesh is dirty and needs updating
		//needs to be above the early exit checks, otherwise cells 
		//keep getting marked as dirty
		v.lastMeshedValue = v.value; 

		//keep the meshing within the volume bounds
		if (v.xID >= divisions)
			return 0;
		if (v.yID >= divisions)
			return 0;
		if (v.zID >= divisions)
			return 0;
		if (v.xID < 0)
			return 0;
		if (v.yID < 0)
			return 0;
		if (v.zID < 0)
			return 0;

		Voxel v0 = v;

		//this conditions te input to the polygonize, and make sure all the neighbors have valid entries
		//if all input voxels are gauranteed to not be edge voxels, this may not be necessary.
		Voxel v1 = QueryVoxel(v.xID+1, v.yID, v.zID);
		if (v1 == null) {
			voxelBuffer[1].value = initialVoxelValue;
			voxelBuffer[1].anchor = v.anchor + new Vector3(v.size.x, 0, 0);
			v1 = voxelBuffer[1];
		}

		Voxel v2 = QueryVoxel(v.xID+1, v.yID+1, v.zID);
		if (v2 == null) {
			voxelBuffer[2].value = initialVoxelValue;
			voxelBuffer[2].anchor = v.anchor + new Vector3(v.size.x, v.size.y,0);
			v2 = voxelBuffer[2];
		}
		Voxel v3 = QueryVoxel(v.xID, v.yID+1, v.zID);
		if (v3 == null) {
			voxelBuffer[3].value = initialVoxelValue;
			voxelBuffer[3].anchor = v.anchor + new Vector3(0, v.size.y,0);
			v3 = voxelBuffer[3];
		}
		Voxel v4 = QueryVoxel(v.xID, v.yID, v.zID+1);
		if (v4 == null) {
			voxelBuffer[4].value = initialVoxelValue;
			voxelBuffer[4].anchor = v.anchor + new Vector3(0, 0,v.size.z);
			v4 = voxelBuffer[4];
		}
		Voxel v5 = QueryVoxel(v.xID+1, v.yID, v.zID+1);
		if (v5 == null) {
			voxelBuffer[5].value = initialVoxelValue;
			voxelBuffer[5].anchor = v.anchor + new Vector3(v.size.x, 0,v.size.z);
			v5 = voxelBuffer[5];
		}
		Voxel v6 = QueryVoxel(v.xID+1, v.yID+1, v.zID+1);
		if (v6 == null) {
			voxelBuffer[6].value = initialVoxelValue;
			voxelBuffer[6].anchor = v.anchor + new Vector3(v.size.x, v.size.y,v.size.z);
			v6 = voxelBuffer[6];
		}
		Voxel v7 = QueryVoxel(v.xID, v.yID+1, v.zID+1);
		if (v7 == null) {
			voxelBuffer[7].value = initialVoxelValue;
			voxelBuffer[7].anchor = v.anchor + new Vector3(0, v.size.y,v.size.z);
			v7 = voxelBuffer[7];
		}

		int triangleStart = triangles.Count;
		int createdTriangles = Polygonizer.Process(isolevel,
		                    v0.value,
		                    v1.value,
		                    v2.value,
		                    v3.value,
		                    v4.value,
		                    v5.value,
		                    v6.value,
		                    v7.value,
		                    v0.anchor,
		                    v1.anchor,
		                    v2.anchor,
		                    v3.anchor,
		                    v4.anchor,
		                    v5.anchor,
		                    v6.anchor,
		                    v7.anchor,
		                    ref vertices,ref triangles);

		//compute per vertex normal
		int addedVertCount = 0;
		while (normals.Count < vertices.Count) {
			normals.Add(new Vector3(0,1,0));
			uvs.Add (new Vector2 (-1, -1));
			addedVertCount++;
		}

		for (int i = triangleStart; i < triangles.Count; i += 3) {
			v.trianglesIndicies.Add(i);//add it to each voxel this impacted?

			int ia = triangles[i];
			int ib = triangles[i+1];
			int ic = triangles[i+2];
			Vector3 a = vertices[ia];
			Vector3 b = vertices[ib];
			Vector3 c = vertices[ic];
			
			Vector3 n = Vector3.Cross(a-b,b-c).normalized;
			normals[ia] = n;
			normals[ib] = n;
			normals[ic] = n;

			CalculateUVs(ia,ib,ic);
		}

		//adjust per voxel normal

		return createdTriangles;
	}
	
	void DebugDrawX(Vector3 p, float size, Color c) {
		Debug.DrawLine (p + new Vector3 (-size, -size, -size), p + new Vector3 (size, size, size), c);
		Debug.DrawLine (p + new Vector3 (size, -size, -size), p + new Vector3 (-size, size, size), c);
		Debug.DrawLine (p + new Vector3 (size, size, -size), p + new Vector3 (-size, -size, size), c);
		Debug.DrawLine (p + new Vector3 (-size, size, -size), p + new Vector3 (size, -size, size), c);
	}

	public void SimplifyPlanarGroup(Voxel v0, Voxel v1, Voxel v2, Voxel v3) {

		Voxel[] voxels = new Voxel[4];
		voxels [0] = v0;
		voxels [1] = v1;
		voxels [2] = v2;
		voxels [3] = v3;

		int totalTriangles = 0;
		Vector3 averageNormal = new Vector3 (0, 0, 0);
		List<int> triIndicies = new List<int>();

		foreach (Voxel v in voxels) {
			if(v == null)
				return;
			if(v.trianglesIndicies.Count != 2)//only do planar groups right now
				return;
			totalTriangles += v.trianglesIndicies.Count;
			foreach(int triIndex in v.trianglesIndicies)
				averageNormal += normals[triangles[triIndex]];
		}

		averageNormal /= totalTriangles;

		//too contentious
		if (averageNormal.magnitude < 0.75f)
			return;

		//we have 4 voxel neighors with triangles
		//see if the normals are within tolernace

		float variance = 0;
		foreach (Voxel v in voxels) {
			foreach(int triIndex in v.trianglesIndicies) {
				variance += Vector3.SqrMagnitude(normals[triangles[triIndex]]-averageNormal);
				triIndicies.Add(triIndex);
			}
		}
		variance /= totalTriangles;

		if (variance > 0.05) 
			return;

		//we have triangles in a plane

		Vector3 avg = new Vector3 ();
		foreach (int index in triIndicies) {
			avg += vertices[triangles[index]];
			avg += vertices[triangles[index+1]];
			avg += vertices[triangles[index+2]];
		}
		avg /= totalTriangles * 3;

		foreach (int index in triIndicies) {
			bool remove = false;
			if(Vector3.SqrMagnitude(vertices[triangles[index]]-avg) < 0.05f) {
				remove = true;
			}
			if(Vector3.SqrMagnitude(vertices[triangles[index+1]]-avg) < 0.05f) {
				remove = true;
			}
			if(Vector3.SqrMagnitude(vertices[triangles[index+2]]-avg) < 0.05f) {
				remove = true;
			}

			if(remove) {
				triangles.RemoveAt(index);
				triangles.RemoveAt(index+1);
				triangles.RemoveAt(index+2);
			}
		}
		
		DebugDrawX (avg + transform.position, 0.01f, Color.magenta);

		//lets create the neighbor graph

	}

	public void SimplifyByVoxel(Voxel v) {
		//this conditions te input to the polygonize, and make sure all the neighbors have valid entries
		//if all input voxels are gauranteed to not be edge voxels, this may not be necessary.
		Voxel v0 = v;
		Voxel v1 = QueryVoxel(v.xID+1, v.yID, v.zID);
		Voxel v2 = QueryVoxel(v.xID+1, v.yID+1, v.zID);
		Voxel v3 = QueryVoxel(v.xID, v.yID+1, v.zID);
		Voxel v4 = QueryVoxel(v.xID, v.yID, v.zID+1);
		Voxel v5 = QueryVoxel(v.xID+1, v.yID, v.zID+1);
		Voxel v6 = QueryVoxel(v.xID, v.yID+1, v.zID+1);

		//three planar directions.. diagonals aren't necessary?
		SimplifyPlanarGroup (v0, v1, v2, v3);
		SimplifyPlanarGroup (v0, v1, v4, v5);
		SimplifyPlanarGroup (v0, v2, v3, v6);
	}


	public void SimplifyMesh() {


//		for (int i = 0; i < 2; i++) {
//			int index = UnityEngine.Random.Range(0,triangles.Count/3);
//			index *= 3;
//			triangles.RemoveAt(index);
//			triangles.RemoveAt(index+1);
//			triangles.RemoveAt(index+2);
//		}
//		return;


		foreach (VoxelTree t in voxelStorage.GetEnumerable()) {
			SimplifyByVoxel(t.Voxel);
		}
	}


	public int RegenerateMesh() {
		if (!isDirty)
			return 0;
		if (mf == null)
			Debug.Log ("mesh is null");
		mf.mesh.Clear ();
		vertices.Clear ();
		triangles.Clear ();
		normals.Clear ();
		uvs.Clear ();

		PrepareVoxels ();

		foreach (VoxelTree t in voxelStorage.GetEnumerable()) {
			if (t.Voxel == null) {
				Debug.Log ("something is wrong, VoxelTree has null voxel");
				continue;
			}

			if(CreateMarchingCube(t.Voxel)==0) {
				//no triangles were created, consider deleting this voxel?
				//just delecting, causes a lot of re-creation.  need o be smarter
			}
		}

		//ComputeVoxelNormals ();

//		SimplifyMesh ();

		SetMesh ();
		isDirty = false;
		return 1;
	}

	public void SetMesh() {
		mf.mesh.Clear ();
		mf.mesh.vertices = vertices.ToArray();
		mf.mesh.normals = normals.ToArray();
		mf.mesh.uv = uvs.ToArray ();
		mf.mesh.triangles = triangles.ToArray();
		mf.mesh.RecalculateBounds ();
		mf.mesh.Optimize ();
		
		GetComponent<MeshCollider>().sharedMesh = null;
		GetComponent<MeshCollider>().sharedMesh = mf.mesh;
	}

	public void Draw() {
		for (int i =0; i < vertices.Count; i++) {
			Vector3 p = transform.position + vertices[i];
			Debug.DrawLine (p,p + normals[i]*cellSize.x,Color.red);
		}
	}

	// Update is called once per frame
	void Update () {
	}
}
