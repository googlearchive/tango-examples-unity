// <copyright file="SelectMe.cs" company="Google">
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
/// Keeps track of the selected model object.
/// </summary>
public class SelectMe : MonoBehaviour
{
    /// <summary>
    /// The color controller object.
    /// </summary>
    public GameObject m_colorController;

    /// <summary>
    /// The selected model object.
    /// </summary>
    public GameObject m_selectorCube;

    /// <summary>
    /// Unity click handler function.
    ///
    /// When clicked, set this object as the selected one and move the model selector
    /// to be on top of this selected model.
    /// </summary>
    public void OnMouseDown()
    {
        JavaEventScript script = m_colorController.GetComponent<JavaEventScript>();
        script.selectedObject = gameObject;
        Vector3 selectorPosition = m_selectorCube.transform.position;
        selectorPosition.x = gameObject.transform.position.x;
        m_selectorCube.transform.position = selectorPosition;
    }
}
