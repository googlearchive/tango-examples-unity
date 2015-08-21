//-----------------------------------------------------------------------
// <copyright file="UIInfoPanelController.cs" company="Google">
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
using System.Collections;
using UnityEngine;

/// <summary>
/// User interface info panel controller.
/// </summary>
public class UIInfoPanelController : MonoBehaviour
{
    public GameObject background;
    public TextMesh textMesh;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        EventManager.GameDataSaved += GameDataSaved;
        EventManager.TangoPoseStateChanged += TangoPoseStateChanged;
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
    }

    /// <summary>
    /// Callback for when GameData is saved.
    /// </summary>
    /// <param name="successed">If set to <c>true</c> successed.</param>
    private void GameDataSaved(bool successed)
    {
        if (successed)
        {
            StartCoroutine(ShowText("Game Saved", 1.5f));
        }
    }

    /// <summary>
    /// Callback for when the Tango pose state changes.
    /// </summary>
    /// <param name="curState">Current state.</param>
    private void TangoPoseStateChanged(TangoPoseStates curState)
    {
        if (curState == TangoPoseStates.Connecting)
        {
            SetPanelShown(true);
            textMesh.text = Statics.uiPanelConnectingService;
        }
        else if (curState == TangoPoseStates.Relocalizing)
        {
            SetPanelShown(true);
            textMesh.text = Statics.uiPanelRelocalizing;
        }
        else if (curState == TangoPoseStates.Running)
        {
            SetPanelShown(false);
        }
    }

    /// <summary>
    /// Set the panel visibility.
    /// </summary>
    /// <param name="isShowing">If set to <c>true</c> is showing.</param>
    private void SetPanelShown(bool isShowing)
    {
        background.GetComponent<Renderer>().enabled = isShowing;
        textMesh.gameObject.SetActive(isShowing);
    }

    private float counter = 0.0f;

    /// <summary>
    /// Coroutine for showing text.
    /// </summary>
    /// <returns>Coroutine enumerator.</returns>
    /// <param name="text">Text to show.</param>
    /// <param name="timeLength">How long to show the text.</param>
    private IEnumerator ShowText(string text, float timeLength)
    {
        textMesh.text = text;
        while (counter <= timeLength)
        {
            SetPanelShown(true);
            counter += Time.deltaTime;
            yield return null;
        }
        SetPanelShown(false);
        textMesh.text = string.Empty;
        counter = 0.0f;
        yield return null;
    }
}
