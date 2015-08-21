//-----------------------------------------------------------------------
// <copyright file="GameCameraEffectController.cs" company="Google">
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
/// Controls camera effects.
/// </summary>
public class GameCameraEffectController : MonoBehaviour
{
    public Vignetting vignetting;
    public DepthOfField34 depthOfField;

    private float counter = 0.0f;
    private float totalTime = 1.0f;
    private bool savedStartStateOfEffects = false;

    private float chromaticAberration;
    private float blury;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        if (Statics.currentTangoState == TangoPoseStates.Connecting || Statics.currentTangoState == TangoPoseStates.Relocalizing)
        {
            depthOfField.smoothness = 0.1f;
            counter += Time.deltaTime;
            if (counter <= totalTime)
            {
                vignetting.chromaticAberration = Mathf.Lerp(-36.0f, 36.0f, counter / totalTime);
            }
            if (counter > totalTime && counter <= 2 * totalTime)
            {
                vignetting.chromaticAberration = Mathf.Lerp(36.0f, -36.0f, (counter - totalTime) / totalTime);
            }
            if (counter > 2 * totalTime)
            {
                counter = 0.0f;
            }
            savedStartStateOfEffects = false;
        }
        else
        {
            if (!savedStartStateOfEffects)
            {
                counter = 0.0f;
                savedStartStateOfEffects = true;
                chromaticAberration = vignetting.chromaticAberration;
                blury = depthOfField.smoothness;
            }
            counter += Time.deltaTime;
            if (counter <= 2.0f)
            {
                vignetting.chromaticAberration = Mathf.Lerp(chromaticAberration, 0.2f, counter / 2.0f);
                depthOfField.smoothness = Mathf.Lerp(blury, 3.84f, counter / 2.0f);
            }
        }
    }
}
