//-----------------------------------------------------------------------
// <copyright file="SaveGameButton.cs" company="Google">
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
/// Save game button.
/// </summary>
public class SaveGameButton : TouchableObject
{
    public GameObject content;
    public float normaledScaleFactor = 0.95f;
    private Vector3 touchScaleSize;
    private Vector3 untouchedScaleSize;

    /// <summary>
    /// Initialize this component.
    /// </summary>
    public void Start()
    {
        m_raycastCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        touchScaleSize = normaledScaleFactor * content.transform.localScale;
        untouchedScaleSize = content.transform.localScale;
    }

    /// <summary>
    /// Touch event similar to key hold.
    /// </summary>
    protected override void OnTouch()
    {
        content.gameObject.transform.localScale = touchScaleSize;
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    protected override void OutTouch()
    {
        content.gameObject.transform.localScale = untouchedScaleSize;
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    protected override void TouchUp()
    {
        content.gameObject.transform.localScale = untouchedScaleSize;
        DataSavingController.SaveData();
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    protected override void Update()
    {
        base.Update();
    }
}
