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

public class AddBuildingPickerButton : TouchableObject
{
    public GameObject buildingPickerBar;
    public GameObject content;
    public float normaledScaleFactor = 0.95f;
    private Vector3 touchScaleSize;
    private Vector3 untouchedScaleSize;

    bool isPickerOpened = false;

    void Start() {
        m_raycastCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        touchScaleSize = normaledScaleFactor * content.transform.localScale;
        untouchedScaleSize = content.transform.localScale;
        // Reset the picker position.
        buildingPickerBar.transform.position = new Vector3(Statics.buildingPickerSlideOutPosX, 
                                                           buildingPickerBar.transform.position.y,
                                                           buildingPickerBar.transform.position.z);
    }

    protected override void OnTouch() {
        content.gameObject.transform.localScale = touchScaleSize;
    }

    protected override void OutTouch() {
        content.gameObject.transform.localScale = untouchedScaleSize;
    }

    protected override void TouchUp() {
        content.gameObject.transform.localScale = untouchedScaleSize;

        StartCoroutine(ButtonRotationAnimation());
    }

    protected override void Update() {
        base.Update();
    }

    float counter = 0.0f;
    float totalTime = 0.25f;
    IEnumerator ButtonRotationAnimation () {
        while(counter< totalTime) {
            counter += Time.deltaTime;
            float rotValue;
            float posValue;
            if (isPickerOpened) {
                rotValue = Mathf.Lerp(-45.0f, 0.0f, counter/totalTime);
                posValue = Mathf.Lerp(Statics.buildingPickerSlideInPosX,
                                      Statics.buildingPickerSlideOutPosX,
                                      counter/totalTime);

            } else {
                rotValue = Mathf.Lerp(0.0f, -45.0f, counter/totalTime);
                posValue = Mathf.Lerp(Statics.buildingPickerSlideOutPosX,
                                      Statics.buildingPickerSlideInPosX,
                                      counter/totalTime);
            }
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, rotValue));
            buildingPickerBar.transform.position = new Vector3(posValue, 
                                                               buildingPickerBar.transform.position.y,
                                                               buildingPickerBar.transform.position.z);
            yield return null;
        }
        isPickerOpened = !isPickerOpened;
        counter = 0.0f;
        yield return null;
    }
}
