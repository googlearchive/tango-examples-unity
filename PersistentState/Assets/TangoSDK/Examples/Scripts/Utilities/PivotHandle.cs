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

/// <summary>
/// Pivot handle to visualize local transform.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PivotHandle : MonoBehaviour
{
    private LineRenderer m_lines;

	// Use this for initialization
	void Start () 
    {
        m_lines = gameObject.GetComponent<LineRenderer>();

        m_lines.material = new Material(Shader.Find("Particles/Additive"));
        m_lines.SetWidth(0.05f, 0.05f);
        m_lines.SetVertexCount(5);
        m_lines.SetColors(Color.red, Color.blue);
	}
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 origin = transform.position;

	    // draw red line (x-axis)
        Vector3 vecToRed = origin + transform.right;

        m_lines.SetPosition(0,vecToRed);
        m_lines.SetPosition(1, origin);

        // draw green line (y-axis)
        Vector3 vecToGreen = origin + transform.up;
        
        m_lines.SetPosition(2,vecToGreen);
        m_lines.SetPosition(3, origin);

        // draw blue line (z-axis)
        Vector3 vecToBlue = origin + transform.forward;
        
        m_lines.SetPosition(4,vecToBlue);
        //m_lines.SetPosition(6, origin);
	}
}
