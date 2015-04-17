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

public class BallThrower : MonoBehaviour {


	public GameObject ballPrefab;
	public Camera mainCamera;
	float forwardVelocity = 5.0f;


	GameObject[] ballArray = new GameObject[10];
	int currentBallID = 0;
	
	// Use this for initialization
	void Start () {
		for(int i = 0; i < ballArray.Length; i++){ 
			ballArray[i] = (GameObject)Instantiate (ballPrefab);
			ballArray[i].SetActive (false);
			ballArray[i].transform.parent = transform;
		}
		currentBallID = 0;
	}
	
	// Update is called once per frame
	void Update () {
	
		if (Input.GetKeyDown (KeyCode.Space)) {
			ballArray[currentBallID].transform.position = mainCamera.transform.position - mainCamera.transform.up*ballPrefab.transform.localScale.y;
			ballArray[currentBallID].GetComponent<Rigidbody>().velocity = mainCamera.transform.forward * forwardVelocity + mainCamera.transform.up*forwardVelocity/2;
			ballArray[currentBallID].SetActive(true);
			currentBallID = (currentBallID + 1)%ballArray.Length;
		}

		for (var i = 0; i < Input.touchCount; ++i) {
			if (Input.GetTouch(i).phase == TouchPhase.Began) {

				ballArray[currentBallID].transform.position = mainCamera.transform.position - mainCamera.transform.up*ballPrefab.transform.localScale.y;
				ballArray[currentBallID].GetComponent<Rigidbody>().velocity = mainCamera.transform.forward * forwardVelocity + mainCamera.transform.up*forwardVelocity/2;
				ballArray[currentBallID].SetActive(true);
				currentBallID = (currentBallID + 1)%ballArray.Length;
			}
		}
	}
}
