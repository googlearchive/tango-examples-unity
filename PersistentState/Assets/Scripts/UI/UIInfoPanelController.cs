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

public class UIInfoPanelController : MonoBehaviour {
	public GameObject background;
	public TextMesh textMesh;
	// Use this for initialization
	void Start () {
		EventManager.gameDataSaved += GameDataSaved;
		EventManager.tangoPoseStatedChanged += TangoPoseStateChanged;
	}
	
	// Update is called once per frame
	void Update () {
	}

	void GameDataSaved(bool successed) {
		if (successed) {
			StartCoroutine(ShowText("Game Saved", 1.5f));
		}
	}

	void TangoPoseStateChanged(TangoPoseStates curState) {
		if (curState == TangoPoseStates.Connecting) {
			SetPanelShown(true);
			textMesh.text = Statics.uiPanelConnectingService;
		}
		else if (curState == TangoPoseStates.Relocalizing) {
			SetPanelShown(true);
			textMesh.text = Statics.uiPanelRelocalizing;
		}
		else if (curState == TangoPoseStates.Running) {
			SetPanelShown(false);
		}
	}

	void SetPanelShown(bool isShowing) {
		background.renderer.enabled = isShowing;
		textMesh.gameObject.SetActive(isShowing);
	}

	float counter = 0.0f;
	IEnumerator ShowText(string text, float timeLength) {
		textMesh.text = text;
		while (counter <= timeLength) {
			SetPanelShown(true);
			counter += Time.deltaTime;
			yield return null;
		}
		SetPanelShown(false);
		textMesh.text = "";
		counter = 0.0f;
		yield return null;
	}


}
