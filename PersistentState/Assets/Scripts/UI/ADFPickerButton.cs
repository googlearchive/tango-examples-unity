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
using System.Collections;
using UnityEngine;

/// <summary>
/// Base class Touchable is inherited.
/// Texture based effects added in this class.
/// </summary>
public class ADFPickerButton : TouchableObject
{
    public GameObject content;
    public float normaledScaleFactor = 0.95f;
    public TextMesh titleTextObj;
    public TextMesh subtitleTextObj;
    public GameObject checkerObject;
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
        if (Statics.curADFId == subtitleTextObj.text) {
            Statics.curADFId = "";
            Statics.curADFName = "";
        } else {
            Statics.curADFId = subtitleTextObj.text;
            Statics.curADFName = titleTextObj.text;
        }
    }

    protected override void Update() {
        base.Update();
        if (Statics.curADFId != subtitleTextObj.text) {
            checkerObject.SetActive(false);
        }
        else {
            checkerObject.SetActive(true);
        }
    }

    public void SetTitles(string title, string subtitle) {
        titleTextObj.text = title;
        subtitleTextObj.text = subtitle;
    }
}
