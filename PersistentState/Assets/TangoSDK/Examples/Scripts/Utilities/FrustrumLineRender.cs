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
using System.Collections;
using UnityEngine;

/// <summary>
/// Frustrum render.
/// </summary>
public class FrustrumLineRender : IBasePostRenderer 
{
	// ar camera
	public Camera cam;

	// far end plane distance.
	public float distance;

	public Material lineMat;

	// screen corner unprojected
	private Vector3 lb, lt, rb, rt;

	/// <summary>
	/// Use this for initialization.
	/// </summary>
	private void Start()
	{
		lb = new Vector3(0.0f, 0.0f, distance);
		lt = new Vector3(0.0f, cam.pixelHeight, distance);
		rb = new Vector3(cam.pixelWidth, 0.0f, distance);
		rt = new Vector3(cam.pixelWidth, cam.pixelHeight, distance);
		lineMat.color = Color.white;
	}

	/// <summary>
	/// Unity post render call back.
	/// </summary>
	public override void OnPostRender() 
	{	
		Debug.Log("Rendering");
		Vector3 pos0 = cam.transform.position;
		Vector3 pos1 = cam.ScreenToWorldPoint(lb);
		Vector3 pos2 = cam.ScreenToWorldPoint(lt);
		Vector3 pos3 = cam.ScreenToWorldPoint(rt);
		Vector3 pos4 = cam.ScreenToWorldPoint(rb);
	
		GL.PushMatrix();
		lineMat.SetPass(0);
		GL.Begin(GL.LINES);

		GL.Color(Color.red);
		GL.Vertex(pos0);
		GL.Vertex(pos1);
		GL.Vertex(pos0);
		GL.Vertex(pos2);
		GL.Vertex(pos0);
		GL.Vertex(pos3);
		GL.Vertex(pos0);
		GL.Vertex(pos4);

		GL.Vertex(pos1);
		GL.Vertex(pos2);
		GL.Vertex(pos2);
		GL.Vertex(pos3);
		GL.Vertex(pos3);
		GL.Vertex(pos4);
		GL.Vertex(pos4);
		GL.Vertex(pos1);
		GL.End();
		GL.PopMatrix();
	}
}
