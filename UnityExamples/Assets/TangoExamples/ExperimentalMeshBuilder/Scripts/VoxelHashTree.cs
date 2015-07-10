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


/**
 * Voxel Hash Tree
 * A binary tree datastructure that uses a hashkey based on the 3D coordinates.
 * Space with within the cube is divided into a regular grid of voxels.  The hashkey is a 
 * reversible index into that volume.  allows quick indexing, and ray marching 
 * through the cube.
 */

public class VoxelHashTree {

    /**
     * hashkey for this tree node
     */    
    private int m_hashKey = int.MinValue;

    /**
     * voxel data at this node
     */    
    private Voxel m_voxel = null;

    /**
     * Left subtree.
     */    
    private VoxelHashTree m_leftHashTree = null;

    /**
     * Right subtree.
     */    
    private VoxelHashTree m_rightHashTree = null;

    /**
     * Parent tree.
     */    
    private VoxelHashTree m_parentHashTree = null;

    /**
     * Initalize the tree
     */    
    public VoxelHashTree() {}

    /**
     * Clear the tree and subtrees
     */    
    public void Clear() {
        m_hashKey = int.MinValue;

        m_voxel = null;
        m_parentHashTree = null;
        if(m_leftHashTree != null) {
            m_leftHashTree.Clear();
            m_leftHashTree = null;
        }
        if(m_rightHashTree != null) {
            m_rightHashTree.Clear();
            m_rightHashTree = null;
        }
    }

    /**
     * Gets the voxel at this node
     */    
    public Voxel Voxel {
        get {
            return m_voxel;
        }
    }
    
    /**
     * Gets the hashkey of this node
     */    
    public int Key {
        get {
            return m_hashKey;
        }
    }
    
    /**
     * Allows iterating through all nodes in the tree
     */    
    public IEnumerable<VoxelHashTree> GetEnumerable() {
        if (m_leftHashTree != null)
            foreach(VoxelHashTree n in m_leftHashTree.GetEnumerable())
                yield return n;
        yield return this;
        if (m_rightHashTree != null)
            foreach (VoxelHashTree n in m_rightHashTree.GetEnumerable())
                yield return n;
    }

    /**
     * gets the minimum key value in the tree
     */    
    VoxelHashTree GetMinKey() {
        if (m_leftHashTree == null) {
            return this;
        } else {
            return m_leftHashTree.GetMinKey ();
        }
    }
    
    /**
     * Delete this node from tree.
     */    
    public bool Delete() {
        return Delete (m_hashKey);
    }

    /**
     * Delete node with specific haskey from tree.
     */    
    public bool Delete(int hashkey) {
        
        if (hashkey == m_hashKey) {
            if((m_leftHashTree != null)&&(m_rightHashTree != null)) {
                //we have left and right trees,
                //copy the right's minimum tree into this tree
                //delete from right tree
                VoxelHashTree target = m_rightHashTree.GetMinKey();
                m_voxel = target.m_voxel;
                m_hashKey = target.m_hashKey;
                m_rightHashTree.Delete(m_hashKey);
            } else if(m_parentHashTree.m_leftHashTree == this) {
                //point parent directly at child
                //depends on GC to remove this?
                m_parentHashTree.m_leftHashTree = (m_leftHashTree != null) ? m_leftHashTree : m_rightHashTree;
            } else if(m_parentHashTree.m_rightHashTree == this) {
                //point parent directly at child
                //depends on GC to remove this?
                m_parentHashTree.m_rightHashTree = (m_leftHashTree != null) ? m_leftHashTree : m_rightHashTree;
            }
            return true;
        }
        
        if (hashkey < m_hashKey) {
            if (m_leftHashTree == null)
                return false;
            return m_leftHashTree.Delete(hashkey);
        } else {
            if (m_rightHashTree == null)
                return false;
            return m_rightHashTree.Delete(hashkey);
        }
    }

    /**
     * Insert new voxel with hashkey into the hashtree
     * @param voxel voxel to be inserted
     * @param hashkey hashkey of the voxel
     */    
    public void Insert(Voxel voxel, int hashkey) {
        if (m_hashKey == int.MinValue) {
            m_hashKey = hashkey;
            this.m_voxel = voxel;
            return;
        }
        if (m_hashKey == hashkey) {
            this.m_voxel = voxel;
            return;
        }
        if(hashkey < m_hashKey) {
            if(m_leftHashTree == null) {
                m_leftHashTree = new VoxelHashTree();
                m_leftHashTree.m_parentHashTree = this;
            }
            m_leftHashTree.Insert(voxel, hashkey);
        } else {
            if(m_rightHashTree == null){
                m_rightHashTree = new VoxelHashTree();
                m_rightHashTree.m_parentHashTree = this;
            }
            m_rightHashTree.Insert(voxel, hashkey);
        }
    }

    /**
     * Query for spefici voxel using hashkey
     * @param hashkey haskey of the target voxel
     * @return the voxel if it exsits, otherwise null
     */    
    public Voxel Query(int hashkey) {
        if(hashkey == m_hashKey)
            return m_voxel;
        if(hashkey < m_hashKey) {
            if(m_leftHashTree == null)
                return null;
            else
                return m_leftHashTree.Query(hashkey);
        } else {
            if(m_rightHashTree == null)
                return null;
            else
                return m_rightHashTree.Query(hashkey);
        }
    }    
}
