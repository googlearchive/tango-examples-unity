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

public class PointCloudGenerator : MonoBehaviour {


	public Camera mainCamera = null;
	public int insertionCount = 2000;
	private DynamicMeshManager occupancyManager;
	private int frameCount = 0;
	
	RaycastHit hitInfo = new RaycastHit ();
	int virutalRoomLayer = 1 << 8;

	// Use this for initialization
	void Start () {	
		occupancyManager = GetComponent<DynamicMeshManager> ();
	}
	
	// Update is called once per frame
	void LateUpdate () {
		frameCount++;

#if UNITY_EDITOR
	
		if (Input.GetKeyDown (KeyCode.C)) {
			occupancyManager.Clear();
		}

		if ((frameCount % 6) == 0) {
			float start = Time.realtimeSinceStartup;

			for (int i = 0; i < insertionCount; i++) {
					int x = UnityEngine.Random.Range (0, Screen.width);
					int y = UnityEngine.Random.Range (0, Screen.height);
					Ray ray = mainCamera.ScreenPointToRay (new Vector3 (x, y, 0));
					if (Physics.Raycast (ray, out hitInfo, 4, virutalRoomLayer)) {
							occupancyManager.InsertPoint (hitInfo.point, ray.direction, 1.0f/(hitInfo.distance+1));
					}
			}
			occupancyManager.QueueDirtyMeshesForRegeneration ();

			float stop = Time.realtimeSinceStartup;
			occupancyManager.InsertionTime = occupancyManager.InsertionTime*occupancyManager.Smoothing + (1.0f-occupancyManager.Smoothing)*(stop - start);

		}
#endif
	}
}
