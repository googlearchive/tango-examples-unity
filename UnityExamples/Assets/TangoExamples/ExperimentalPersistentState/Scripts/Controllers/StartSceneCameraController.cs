//-----------------------------------------------------------------------
// <copyright file="StartSceneCameraController.cs" company="Google">
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
/// Updates a simple chromatic effect.
/// </summary>
public class StartSceneCameraController : MonoBehaviour
{
    public float spinningSpeed;
    public Vignetting vignetting;
    
    private float counter = 0.0f;
    private float totalTime = 1.0f;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        StartCoroutine(ColorChromEffect());
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        gameObject.transform.Rotate(new Vector3(0.0f, spinningSpeed, 0.0f));
    }

    /// <summary>
    /// Coroutine for updating the chromatic effect.
    /// </summary>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator ColorChromEffect()
    {
        while (true)
        {
            counter += Time.deltaTime;
            if (counter <= totalTime)
            {
                vignetting.chromaticAberration = Mathf.Lerp(-6.0f, 6.0f, counter / totalTime);
            }
            if (counter > totalTime && counter <= 2 * totalTime)
            {
                vignetting.chromaticAberration = Mathf.Lerp(6.0f, -6.0f, (counter - totalTime) / totalTime);
            }
            if (counter > 2 * totalTime)
            {
                counter = 0.0f;
            }
            yield return null;
        }
    }
}
