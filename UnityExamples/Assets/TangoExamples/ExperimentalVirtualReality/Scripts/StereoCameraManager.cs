// <copyright file="StereoCameraManager.cs" company="Google">
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
/// Updates two cameras to provide a stereo view.
/// </summary>
public class StereoCameraManager : MonoBehaviour
{
    public GameObject leftCamera;
    public GameObject rightCamera;

    public bool enableStereo = false;

    public float FOV = 80.0f;
    public float IPDInMM = 65;
    public float screenWidthInMM = 152.4f;

    public float nearClippingPlaneInMM = 150;
    public float farClippingPlaneInMM = 10000;

    public Vector3 eyeOffsetInMM = new Vector3(0, -50, -50);

    public bool isPoseIndependentCamera = false;
    public bool isShowingDebugButton = false;
    
    private float worldScale = 1.0f;
    private Camera leftCameraComponent;
    private Camera rightCameraComponent;
    private int frameCount = 0;

    private GameObject blackPanel;
    private Vector3 rightVector = new Vector3(1, 0, 0);

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        Application.targetFrameRate = 60;

        leftCameraComponent = leftCamera.GetComponent<Camera>();
        rightCameraComponent = rightCamera.GetComponent<Camera>();

        SetupCameras(enableStereo);
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        frameCount += 1;
        
        if (frameCount < 10)
        {
            // needed to clear the frame buffers
            SetupCameras(false);
        } 
        if (frameCount == 11)
        {
            SetupCameras(enableStereo);
        }
        
        if (enableStereo)
        {
            Vector3 lateralCameraOffset = 0.5f * rightVector * (IPDInMM / 1000.0f) * worldScale;
            Vector3 offset = eyeOffsetInMM * worldScale / 1000.0f;
            leftCamera.transform.localPosition = -lateralCameraOffset + offset;
            leftCamera.transform.rotation = transform.rotation;
            rightCamera.transform.localPosition = lateralCameraOffset + offset;
            rightCamera.transform.rotation = transform.rotation;
        }
        else
        {
            leftCamera.transform.position = transform.position;
            leftCamera.transform.rotation = transform.rotation;
        }
    }

    /// <summary>
    /// Unity GUI callback.
    /// </summary>
    public void OnGUI()
    {
        // TODO(jason): temporarily checking off this part, to do is to move this button to someother debug functionality class.
        if (isShowingDebugButton)
        {
            if (GUI.Button(new Rect(Screen.width - 200, 150, 150, 80), "Toggle Stereo"))
            {
                frameCount = 0;
                enableStereo = !enableStereo;
            }
        }
    }
    
    /// <summary>
    /// Setup the cameras for the left and right eye to display in stereo.
    /// </summary>
    /// <param name="enable">If set to <c>true</c> enable.</param>
    private void SetupCameras(bool enable)
    {
        rightCameraComponent.enabled = enable;
        if (enable)
        {
            if (IPDInMM / 2 > screenWidthInMM / 4)
            {
                float viewPortWidth = 2 * (screenWidthInMM - IPDInMM) / 2 / screenWidthInMM;
                
                // screen is too small, put a gap in the middle
                leftCameraComponent.rect = new Rect(0, 0, viewPortWidth, 1);
                rightCameraComponent.rect = new Rect(1.0f - viewPortWidth, 0, viewPortWidth, 1);
            }
            else
            {
                // screen is too large, put a gap on the sides
                float viewPortWidth = IPDInMM / screenWidthInMM;
                leftCameraComponent.rect = new Rect(0.5f - viewPortWidth, 0, viewPortWidth, 1);
                rightCameraComponent.rect = new Rect(0.5f, 0, viewPortWidth, 1);
            }
            
            leftCameraComponent.nearClipPlane = nearClippingPlaneInMM * worldScale / 1000.0f;
            leftCameraComponent.farClipPlane = farClippingPlaneInMM * worldScale / 1000.0f;
            leftCameraComponent.fieldOfView = FOV;
            rightCameraComponent.fieldOfView = leftCameraComponent.fieldOfView;
            rightCameraComponent.backgroundColor = leftCameraComponent.backgroundColor;
            rightCameraComponent.nearClipPlane = leftCameraComponent.nearClipPlane;
            rightCameraComponent.farClipPlane = leftCameraComponent.farClipPlane;
        }
        else
        {
            leftCameraComponent.rect = new Rect(0, 0, 1, 1);
        }
    }
}
