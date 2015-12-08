//-----------------------------------------------------------------------
// <copyright file="VoxelHashTree.cs" company="Google">
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A binary tree datastructure that uses a hashkey based on the 3D coordinates.
/// Space with within the cube is divided into a regular grid of voxels.  The hashkey is a 
/// reversible index into that volume.  allows quick indexing, and ray marching 
/// through the cube.
/// </summary>
public class VoxelHashTree
{
    /// <summary>
    /// Hashkey for this tree node.
    /// </summary>
    private int m_hashKey = int.MinValue;

    /// <summary>
    /// Voxel data at this node.
    /// </summary>
    private Voxel m_voxel = null;

    /// <summary>
    /// Left subtree.
    /// </summary>
    private VoxelHashTree m_leftHashTree = null;

    /// <summary>
    /// Right subtree.
    /// </summary>
    private VoxelHashTree m_rightHashTree = null;

    /// <summary>
    /// Parent tree.
    /// </summary>
    private VoxelHashTree m_parentHashTree = null;

    /// <summary>
    /// Initalize the tree.
    /// </summary>
    public VoxelHashTree()
    {
    }

    /// <summary>
    /// Clear the tree and subtrees.
    /// </summary>
    public void Clear()
    {
        m_hashKey = int.MinValue;

        m_voxel = null;
        m_parentHashTree = null;
        if (m_leftHashTree != null)
        {
            m_leftHashTree.Clear();
            m_leftHashTree = null;
        }
        if (m_rightHashTree != null)
        {
            m_rightHashTree.Clear();
            m_rightHashTree = null;
        }
    }

    /// <summary>
    /// Gets the voxel at this node.
    /// </summary>
    /// <value>The voxel.</value>
    public Voxel Voxel
    {
        get { return m_voxel; }
    }
    
    /// <summary>
    /// Gets the hashkey of this node.
    /// </summary>
    /// <value>The key.</value>
    public int Key
    {
        get { return m_hashKey; }
    }
    
    /// <summary>
    /// Allows iterating through all nodes in the tree.
    /// </summary>
    /// <returns>The enumerable.</returns>
    public IEnumerable<VoxelHashTree> GetEnumerable()
    {
        if (m_leftHashTree != null)
        {
            foreach (VoxelHashTree n in m_leftHashTree.GetEnumerable())
            {
                yield return n;
            }
        }
        yield return this;
        if (m_rightHashTree != null)
        {
            foreach (VoxelHashTree n in m_rightHashTree.GetEnumerable())
            {
                yield return n;
            }
        }
    }

    /// <summary>
    /// Gets the minimum key value in the tree.
    /// </summary>
    /// <returns>The minimum key.</returns>
    public VoxelHashTree GetMinKey()
    {
        if (m_leftHashTree == null)
        {
            return this;
        }
        else
        {
            return m_leftHashTree.GetMinKey();
        }
    }
    
    /// <summary>
    /// Delete this node from tree.
    /// </summary>
    /// <returns>True.</returns>
    public bool Delete()
    {
        return Delete(m_hashKey);
    }

    /// <summary>
    /// Delete node with specific haskey from tree.
    /// </summary>
    /// <param name="hashkey">Hashkey.</param>
    /// <returns>If it found a node to delete.</returns>
    public bool Delete(int hashkey)
    {
        if (hashkey == m_hashKey)
        {
            if ((m_leftHashTree != null) && (m_rightHashTree != null))
            {
                // we have left and right trees,
                // copy the right's minimum tree into this tree
                // delete from right tree
                VoxelHashTree target = m_rightHashTree.GetMinKey();
                m_voxel = target.m_voxel;
                m_hashKey = target.m_hashKey;
                m_rightHashTree.Delete(m_hashKey);
            }
            else if (m_parentHashTree.m_leftHashTree == this)
            {
                // point parent directly at child
                // depends on GC to remove this?
                m_parentHashTree.m_leftHashTree = (m_leftHashTree != null) ? m_leftHashTree : m_rightHashTree;
            }
            else if (m_parentHashTree.m_rightHashTree == this)
            {
                // point parent directly at child
                // depends on GC to remove this?
                m_parentHashTree.m_rightHashTree = (m_leftHashTree != null) ? m_leftHashTree : m_rightHashTree;
            }
            return true;
        }
        
        if (hashkey < m_hashKey)
        {
            if (m_leftHashTree == null)
            {
                return false;
            }
            return m_leftHashTree.Delete(hashkey);
        }
        else
        {
            if (m_rightHashTree == null)
            {
                return false;
            }
            return m_rightHashTree.Delete(hashkey);
        }
    }

    /// <summary>
    /// Insert new voxel with hashkey into the hashtree.
    /// </summary>
    /// <param name="voxel">Voxel to be inserted.</param>
    /// <param name="hashkey">Hashkey of the voxel.</param>
    public void Insert(Voxel voxel, int hashkey)
    {
        if (m_hashKey == int.MinValue)
        {
            m_hashKey = hashkey;
            this.m_voxel = voxel;
            return;
        }
        if (m_hashKey == hashkey)
        {
            this.m_voxel = voxel;
            return;
        }
        if (hashkey < m_hashKey)
        {
            if (m_leftHashTree == null)
            {
                m_leftHashTree = new VoxelHashTree();
                m_leftHashTree.m_parentHashTree = this;
            }
            m_leftHashTree.Insert(voxel, hashkey);
        }
        else
        {
            if (m_rightHashTree == null)
            {
                m_rightHashTree = new VoxelHashTree();
                m_rightHashTree.m_parentHashTree = this;
            }
            m_rightHashTree.Insert(voxel, hashkey);
        }
    }

    /// <summary>
    /// Query for spefici voxel using hashkey.
    /// </summary>
    /// <param name="hashkey">Haskey of the target voxel.</param>
    /// <returns>The voxel if it exsits, otherwise null.</returns>
    public Voxel Query(int hashkey)
    {
        if (hashkey == m_hashKey)
        {
            return m_voxel;
        }
        if (hashkey < m_hashKey)
        {
            if (m_leftHashTree == null)
            {
                return null;
            }
            else
            {
                return m_leftHashTree.Query(hashkey);
            }
        } 
        else
        {
            if (m_rightHashTree == null)
            {
                return null;
            }
            else
            {
                return m_rightHashTree.Query(hashkey);
            }
        }
    }    
}
