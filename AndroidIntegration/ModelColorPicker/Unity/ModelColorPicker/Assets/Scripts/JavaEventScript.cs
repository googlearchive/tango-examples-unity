// <copyright file="JavaEventScript.cs" company="Google">
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
using System.Collections;
using UnityEngine;

/// <summary>
/// Receives and routes model color selection events from the Androd Java layer.
/// </summary>
public class JavaEventScript : MonoBehaviour
{
    /// <summary>
    /// The currently selected object.
    /// </summary>
    public GameObject selectedObject;

    /// <summary>
    /// Sets the currently selected object to the given color.
    /// </summary>
    /// <param name="colorString">The color string in RGB format with a starting hash,
    /// e.g.: #AA0022.</param>
    public void ChangeModelColor(string colorString)
    {
        if (selectedObject != null)
        {
            Material material = selectedObject.GetComponent<Renderer>().material;
            colorString = colorString.Replace("#", string.Empty);
            byte red = byte.Parse(colorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte green = byte.Parse(colorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte blue = byte.Parse(colorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            material.color = new Color32(red, green, blue, 255);
        }
    }
}
