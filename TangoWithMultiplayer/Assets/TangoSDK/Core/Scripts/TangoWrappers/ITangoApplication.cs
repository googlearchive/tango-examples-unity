//-----------------------------------------------------------------------
// <copyright file="ITangoApplication.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
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
namespace Tango
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Defines an interface of TangoApplication.
    /// </summary>
    internal interface ITangoApplication : ITangoApplicationSettings, ITangoUX
    {
        /// <summary>
        /// Registers an object to receive event callbacks from TangoApplication.
        /// </summary>
        /// <param name="tangoObject">An object that implements ITangoDepth, ITangoEvent, ITangoPose, ITangoVideoOverlay,
        /// or ITangoExperimentalTangoVideoOverlay.</param>
        void Register(object tangoObject);

        /// <summary>
        /// Unregister from Tango callbacks.  See TangoApplication.Register for more details.
        /// </summary>
        /// <param name="tangoObject">An object that implements ITangoDepth, ITangoEvent, ITangoPose, ITangoVideoOverlay,
        /// or ITangoExperimentalTangoVideoOverlay.</param>
        void Unregister(System.Object tangoObject);

        /// <summary>
        /// Clear the 3D Reconstruction data.  The reconstruction will start fresh.
        /// </summary>
        void Tango3DRClear();

        /// <summary>
        /// Extract a single grid cell's mesh.
        /// </summary>
        /// <returns>Status of the extraction.</returns>
        /// <param name="gridIndex">Grid index to extract.</param>
        /// <param name="vertices">Filled out with extracted vertices.</param>
        /// <param name="normals">Filled out with extracted normals.</param>
        /// <param name="colors">Filled out with extracted colors.</param>
        /// <param name="triangles">Filled out with extracted triangle indices.</param>
        /// <param name="numVertices">Filled out with the number of valid vertices.</param>
        /// <param name="numTriangles">Filled out with the number of valid triangles.</param>
        Tango3DReconstruction.Status Tango3DRExtractMeshSegment(
            Tango3DReconstruction.GridIndex gridIndex, Vector3[] vertices, Vector3[] normals, Color32[] colors,
            int[] triangles, out int numVertices, out int numTriangles);

        /// <summary>
        /// Extracts a mesh of the entire 3D reconstruction into a suitable format for a Unity Mesh.
        /// </summary>
        /// <returns>
        /// Returns <c>Status.SUCCESS</c> if the mesh is fully extracted and stored in the lists. Otherwise, Status.ERROR or
        /// Status.INVALID is returned if some error occurs.</returns>
        /// <param name="vertices">A list to which mesh vertices will be appended; can be null.</param>
        /// <param name="normals">A list to which mesh normals will be appended; can be null.</param>
        /// <param name="colors">A list to which mesh colors will be appended; can be null.</param>
        /// <param name="triangles">A list to which vertex indices will be appended, can be null.</param>
        Tango3DReconstruction.Status Tango3DRExtractWholeMesh(
            List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles);

        /// <summary>
        /// Extract an array of <c>SignedDistanceVoxel</c> objects.
        /// </summary>
        /// <returns>
        /// Returns Status.SUCCESS if the voxels are fully extracted and stored in the array.  In this case, <c>numVoxels</c>
        /// will say how many voxels are used; the rest of the array is untouched.
        ///
        /// Returns Status.INVALID if the array length does not exactly equal the number of voxels in a single grid
        /// index.  By default, the number of voxels in a grid index is 16*16*16.
        ///
        /// Returns Status.INVALID if some other error occurs.
        /// </returns>
        /// <param name="gridIndex">Grid index to extract.</param>
        /// <param name="voxels">
        /// On successful extraction this is filled out with the signed distance voxels.
        /// </param>
        /// <param name="numVoxels">Number of voxels filled out.</param>
        Tango3DReconstruction.Status Tango3DRExtractSignedDistanceVoxel(
            Tango3DReconstruction.GridIndex gridIndex, Tango3DReconstruction.SignedDistanceVoxel[] voxels,
            out int numVoxels);
    }
}