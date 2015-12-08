//-----------------------------------------------------------------------
// <copyright file="CreateHeadsetGeometery.cs" company="Google">
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
/// Creates geometry to model a headset.
/// </summary>
public class CreateHeadsetGeometery : MonoBehaviour
{
    public GameObject leftCamera;
    public GameObject rightCamera;

    public Vector2 screenSizeInMM = new Vector2(150, 95);
    public Vector3 screenOffsetInMM = new Vector3(0, -60, 0);
    
    public Vector3 headOffsetInMM = new Vector3(0, -60, -110);

    private GameObject tangoPoseCenter;
    private GameObject leftEyeBall;
    private GameObject rightEyeBall;
    private GameObject headCenter;

////    private GameObject leftCamNearClip;
////    private GameObject rightCamNearClip;

    private GameObject deviceScreen;

////    private Camera leftCameraComponent;
////    private Camera rightCameraComponent;
    private float eyeBallSizeInMM = 24;

    private float worldScale = 1.0f;
    private Color color = Color.gray;
    private float headBallSizeInMM = 140;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
////        leftCameraComponent = leftCamera.GetComponent<Camera> ();
////        rightCameraComponent = rightCamera.GetComponent<Camera> ();

        tangoPoseCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tangoPoseCenter.transform.parent = transform;
        tangoPoseCenter.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f) * worldScale;
        tangoPoseCenter.transform.localPosition = new Vector3();
        tangoPoseCenter.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
        tangoPoseCenter.GetComponent<Renderer>().material.SetColor("_Color", color);

        leftEyeBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEyeBall.transform.parent = leftCamera.transform;
        leftEyeBall.transform.localPosition = new Vector3();
        leftEyeBall.transform.localScale = new Vector3(eyeBallSizeInMM, eyeBallSizeInMM, eyeBallSizeInMM) * worldScale / 1000.0f;
        leftEyeBall.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
        leftEyeBall.GetComponent<Renderer>().material.SetColor("_Color", color);

        headCenter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headCenter.transform.parent = transform;
        headCenter.transform.localPosition = headOffsetInMM * worldScale / 1000.0f;
        headCenter.transform.localScale = new Vector3(headBallSizeInMM, headBallSizeInMM, headBallSizeInMM) * worldScale / 1000.0f;
        headCenter.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
        headCenter.GetComponent<Renderer>().material.SetColor("_Color", color);

////        leftCamNearClip =  GameObject.CreatePrimitive(PrimitiveType.Cube);
////        leftCamNearClip.transform.parent = leftCamera.transform;
////        leftCamNearClip.transform.localPosition = new Vector3 (0,0,leftCameraComponent.nearClipPlane);
////        leftCamNearClip.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
////        leftCamNearClip.GetComponent<Renderer>().material.SetColor("_Color", color);

        deviceScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deviceScreen.transform.parent = transform;
        deviceScreen.transform.localScale = new Vector3(screenSizeInMM.x * worldScale / 1000.0f, screenSizeInMM.y * worldScale / 1000.0f, worldScale / 1000.0f);
        deviceScreen.transform.localPosition = screenOffsetInMM * worldScale / 1000.0f;
        deviceScreen.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
        deviceScreen.GetComponent<Renderer>().material.SetColor("_Color", color);

        rightEyeBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEyeBall.transform.parent = rightCamera.transform;
        rightEyeBall.transform.localPosition = new Vector3();
        rightEyeBall.transform.localScale = new Vector3(eyeBallSizeInMM, eyeBallSizeInMM, eyeBallSizeInMM) * worldScale / 1000.0f;
        rightEyeBall.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
        rightEyeBall.GetComponent<Renderer>().material.SetColor("_Color", color);

////        rightCamNearClip =  GameObject.CreatePrimitive(PrimitiveType.Cube);
////        rightCamNearClip.transform.parent = rightCamera.transform;
////        rightCamNearClip.transform.localPosition = new Vector3 (0,0,rightCameraComponent.nearClipPlane);
////        rightCamNearClip.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
////        rightCamNearClip.GetComponent<Renderer>().material.SetColor("_Color", color);
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
////        float horzSize = Mathf.Tan (leftCameraComponent.fieldOfView * Mathf.Deg2Rad/2) * leftCameraComponent.nearClipPlane;
////        leftCamNearClip.transform.localScale = new Vector3 (horzSize, horzSize, 0.01f);
////        rightCamNearClip.transform.localScale = new Vector3 (horzSize, horzSize, 0.01f);
    }
}
