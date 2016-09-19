//-----------------------------------------------------------------------
// <copyright file="MeshBuilderWithColorGUIController.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// Extra GUI controls.
/// </summary>
public class MeshBuilderWithColorGUIController : MonoBehaviour
{
    /// <summary>
    /// If set, grid indices will stop meshing when they have been sufficiently observed.
    /// </summary>
    public bool m_enableSelectiveMeshing;

    /// <summary>
    /// Debug info: If the mesh is being updated.
    /// </summary>
    private bool m_isEnabled = true;

    private TangoApplication m_tangoApplication;
    private TangoDynamicMesh m_dynamicMesh;

    /// <summary>
    /// Start is used to initialize.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_dynamicMesh = FindObjectOfType<TangoDynamicMesh>();
        m_dynamicMesh.m_enableSelectiveMeshing = m_enableSelectiveMeshing;
    }

    /// <summary>
    /// Updates UI and handles player input.
    /// </summary>
    public void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Draws the Unity GUI.
    /// </summary>
    public void OnGUI()
    {
        GUI.color = Color.white;
        if (GUI.Button(new Rect(Screen.width - 160, 20, 140, 80), "<size=30>Clear</size>"))
        {
            m_dynamicMesh.Clear();
            m_tangoApplication.Tango3DRClear();
        }
        
        string text = m_isEnabled ? "Pause" : "Resume";
        if (GUI.Button(new Rect(Screen.width - 160, 120, 140, 80), "<size=30>" + text + "</size>"))
        {
            m_isEnabled = !m_isEnabled;
            m_tangoApplication.Set3DReconstructionEnabled(m_isEnabled);
        }

        if (GUI.Button(new Rect(Screen.width - 160, 220, 140, 80), "<size=30>Export</size>"))
        {
            string filepath = "/sdcard/DemoMesh.obj";
            m_dynamicMesh.ExportMeshToObj(filepath);
            Debug.Log(filepath);
        }
    }
}
