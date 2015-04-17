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
using System.Collections.Generic;

public class Voxel
{
	public int xID;
	public int yID;
	public int zID;
	public float value;
	public float weight;
	public float lastMeshedValue;
	public Vector3 anchor;
	public Vector3 size;
	public Vector3 normal;
	public List<int> trianglesIndicies = new List<int>(); 

	public Voxel() {

	}
}

public class VoxelTree {
	
	private int key = -1;
	private Voxel voxel = null;
	private VoxelTree leftTree = null;
	private VoxelTree rightTree = null;
	private VoxelTree parent = null;
	
	public VoxelTree() {}
	
	public void Clear() {
		key = -1;
//		voxel.trianglesIndicies.Clear ();//causes a hang? perhaps GC hangs?
		//voxel.trianglesIndicies = null;
		voxel = null;
		parent = null;
		if(leftTree != null) {
			leftTree.Clear();
			leftTree = null;
		}
		if(rightTree != null) {
			rightTree.Clear();
			rightTree = null;
		}
	}
	
	public Voxel Voxel {
		get {
			return voxel;
		}
	}
	
	//IEnumerator and IEnumerable require these methods.
	public IEnumerable<VoxelTree> GetEnumerable()
	{
		if (leftTree != null)
			foreach(VoxelTree n in leftTree.GetEnumerable())
				yield return n;
		yield return this;
		if (rightTree != null)
			foreach (VoxelTree n in rightTree.GetEnumerable())
				yield return n;
	}

	VoxelTree GetMinKey() {
		if (leftTree == null) {
			return this;
		} else {
			return leftTree.GetMinKey ();
		}
	}

	public bool Delete() {
		return Delete (key);
	}

	public bool Delete(int hashkey) {

		if (hashkey == key) {
			if((leftTree != null)&&(rightTree != null)) {
				//we have left and right trees,
				//copy the right's minimum tree into this tree
				//delete from right tree
				VoxelTree target = rightTree.GetMinKey();
				voxel = target.voxel;
				key = target.key;
				rightTree.Delete(key);
			} else if(parent.leftTree == this) {
				//point parent directly at child
				//depends on GC to remove this?
				parent.leftTree = (leftTree != null) ? leftTree : rightTree;
			} else if(parent.rightTree == this) {
				//point parent directly at child
				//depends on GC to remove this?
				parent.rightTree = (leftTree != null) ? leftTree : rightTree;
			}
			return true;
		}

		if (hashkey < key) {
			if (leftTree == null)
				return false;
			return leftTree.Delete(hashkey);
		} else {
			if (rightTree == null)
				return false;
			return rightTree.Delete(hashkey);
		}
	}

	public void Insert(Voxel voxel, int hashkey) {
		if (key == -1) {
			key = hashkey;
			this.voxel = voxel;
			return;
		}
		if (hashkey == key) {
			this.voxel = voxel;
			return;
		}
		if(hashkey < key) {
			if(leftTree == null) {
				leftTree = new VoxelTree();
				leftTree.key = hashkey;
				leftTree.voxel = voxel;
				leftTree.parent = this;
			}
			else
				leftTree.Insert(voxel, hashkey);
		} else {
			if(rightTree == null){
				rightTree = new VoxelTree();
				rightTree.key = hashkey;
				rightTree.voxel = voxel;
				rightTree.parent = this;
			}
			else
				rightTree.Insert(voxel, hashkey);
		}
	}
	
	public Voxel Query(int hashkey) {
		if(hashkey == key)
			return voxel;
		if(hashkey < key) {
			if(leftTree == null)
				return null;
			else
				return leftTree.Query(hashkey);
		} else {
			if(rightTree == null)
				return null;
			else
				return rightTree.Query(hashkey);
		}
	}
	
}
