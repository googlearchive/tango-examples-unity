/*
 * Copyright 2015 Google Inc. All Rights Reserved.
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

public class CancelBuildingButton : TouchableObject {
	public GameObject content;
	public float normaledScaleFactor = 0.95f;
	private Vector3 touchScaleSize;
	private Vector3 untouchedScaleSize;
	
	void Start() {
		m_raycastCamera = GameObject.FindGameObjectWithTag("UICamera").camera;
		touchScaleSize = normaledScaleFactor * content.transform.localScale;
		untouchedScaleSize = content.transform.localScale;
	}

	protected override void OnTouch() {
		content.gameObject.transform.localScale = touchScaleSize;
	}

	protected override void OutTouch() {
		content.gameObject.transform.localScale = untouchedScaleSize;
	}

	protected override void TouchUp() {
		content.gameObject.transform.localScale = untouchedScaleSize;
		BuildingManager.instance.CancelBuildingPlacement ();
	}

	protected override void Update() {
		base.Update();
	}
}
