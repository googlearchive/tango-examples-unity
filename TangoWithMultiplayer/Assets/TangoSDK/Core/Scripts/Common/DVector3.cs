//-----------------------------------------------------------------------
// <copyright file="DVector3.cs" company="Google">
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
    using System;

    /// <summary>
    /// Double precision vector in 3D.
    /// </summary>
    public struct DVector3
    {
        public double x;
        public double y;
        public double z;

        /// <summary>
        /// Creates a new double-precision vector with given x, y, z components.
        /// </summary>
        /// <param name="x">The x component.</param>
        /// <param name="y">The y component.</param>
        /// <param name="z">The z component.</param>
        public DVector3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// Gets the length of this vector (Read Only).
        /// </summary>
        /// <value>The length.</value>
        public double Magnitude
        {
            get { return Math.Sqrt(SqrMagnitude); }
        }

        /// <summary>
        /// Gets the squared length of this vector (Read Only).
        /// </summary>
        /// <value>The squared length.</value>
        public double SqrMagnitude 
        {
            get { return (x * x) + (y * y) + (z * z); }
        }

        /// <summary>
        /// Returns the distance between a and b.
        /// </summary>
        /// <returns>Euclidean distance as a double.</returns>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        public static double Distance(DVector3 a, DVector3 b)
        {
            return (a - b).Magnitude;
        }

        /// <summary>
        /// Dot Product of two vectors.
        /// </summary>
        /// <returns>Dot product as a double.</returns>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        public static double Dot(DVector3 a, DVector3 b)
        {
            return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
        }

        /// <summary>
        /// Subtracts one vector from another.
        /// 
        /// Subtracts each component of b from a.
        /// </summary>
        /// <returns>Subtraction result.</returns>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        public static DVector3 operator -(DVector3 a, DVector3 b)
        {
            return new DVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        /// <summary>
        /// Adds two vectors.
        /// 
        /// Adds corresponding components together.        
        /// </summary>
        /// <returns>Addition result.</returns>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        public static DVector3 operator +(DVector3 a, DVector3 b)
        {
            return new DVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
    }
}