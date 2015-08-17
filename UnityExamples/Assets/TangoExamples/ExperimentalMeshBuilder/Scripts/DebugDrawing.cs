//-----------------------------------------------------------------------
// <copyright file="DebugDrawing.cs" company="Google">
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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility functions for drawing debug lines for more complex shapes.
/// </summary>
public class DebugDrawing
{
    /// <summary>
    /// Draws a box.
    /// </summary>
    /// <param name="min">One edge of the box.</param>
    /// <param name="max">Other edge of the box.</param>
    /// <param name="c">Color to draw lines for.</param>
    public static void Box(Vector3 min, Vector3 max, Color c)
    {
        Debug.DrawLine(min, new Vector3(min.x, min.y, max.z), c);
        Debug.DrawLine(min, new Vector3(min.x, max.y, min.z), c);
        Debug.DrawLine(min, new Vector3(max.x, min.y, min.z), c);

        Debug.DrawLine(new Vector3(min.x, max.y, max.z), new Vector3(min.x, min.y, max.z), c);
        Debug.DrawLine(new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z), c);

        Debug.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(max.x, min.y, min.z), c);
        Debug.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z), c);

        Debug.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z), c);
        Debug.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(max.x, min.y, min.z), c);

        Debug.DrawLine(max, new Vector3(min.x, max.y, max.z), c);
        Debug.DrawLine(max, new Vector3(max.x, max.y, min.z), c);
        Debug.DrawLine(max, new Vector3(max.x, min.y, max.z), c);
    }

    /// <summary>
    /// Draws a crosshair.
    /// </summary>
    /// <param name="point">Crosshair center.</param>
    /// <param name="size">Size of the lines.</param>
    /// <param name="c">Color to draw lines for.</param>
    public static void CrossHair(Vector3 point, float size, Color c)
    {
        Debug.DrawLine(new Vector3(point.x + size, point.y, point.z), new Vector3(point.x - size, point.y, point.z), c);
        Debug.DrawLine(new Vector3(point.x, point.y + size, point.z), new Vector3(point.x, point.y - size, point.z), c);
        Debug.DrawLine(new Vector3(point.x, point.y, point.z + size), new Vector3(point.x, point.y, point.z - size), c);
        
        float inc = Mathf.PI / 16;
        for (float theta = 0; theta <= 2 * Mathf.PI; theta += inc) 
        {
            float cosPartBegin = size * Mathf.Cos(theta);
            float sinPartBegin = size * Mathf.Sin(theta);
            float cosPartEnd = size * Mathf.Cos(theta + inc);
            float sinPartEnd = size * Mathf.Sin(theta + inc);
            Debug.DrawLine(new Vector3(point.x + cosPartBegin, point.y + sinPartBegin, point.z),
                           new Vector3(point.x + cosPartEnd, point.y + sinPartEnd, point.z),
                           c);
            Debug.DrawLine(new Vector3(point.x + cosPartBegin, point.y, point.z + sinPartBegin),
                           new Vector3(point.x + cosPartEnd, point.y, point.z + sinPartEnd),
                           c);
            Debug.DrawLine(new Vector3(point.x, point.y + sinPartBegin, point.z + cosPartBegin),
                           new Vector3(point.x, point.y + sinPartEnd, point.z + cosPartEnd),
                           c);
        }
    }

    /// <summary>
    /// Draws cross X.
    /// </summary>
    /// <param name="p">Cross center.</param>
    /// <param name="size">Size of the lines.</param>
    /// <param name="c">Color to draw lines for.</param>
    public static void CrossX(Vector3 p, float size, Color c)
    {
        Debug.DrawLine(p + new Vector3(-size, -size, -size), p + new Vector3(size, size, size), c);
        Debug.DrawLine(p + new Vector3(size, -size, -size), p + new Vector3(-size, size, size), c);
        Debug.DrawLine(p + new Vector3(size, size, -size), p + new Vector3(-size, -size, size), c);
        Debug.DrawLine(p + new Vector3(-size, size, -size), p + new Vector3(size, -size, size), c);
    }
}
