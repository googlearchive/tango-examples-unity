//-----------------------------------------------------------------------
// <copyright file="TangoInitializer.cs" company="Google">
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
using System.Collections.Generic;
using System.IO;
using Tango;
using UnityEngine;

/// <summary>
/// Script to connect to the Tango service.
/// </summary>
public class TangoInitializer : MonoBehaviour
{
    public TangoApplication tangoApplication; 

    private bool isInitialized = false;
    private bool shouldInitTango = false;

    /// <summary>
    /// Use this to initialize.
    /// </summary>
    private void Start()
    {
        tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication != null)
        {
            if (AndroidHelper.IsTangoCorePresent())
            {
                // Request Tango permissions
                tangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
                tangoApplication.RequestNecessaryPermissionsAndConnect();
            }
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update()
    {
        if (shouldInitTango)
        {
            tangoApplication.InitApplication();
            isInitialized = true;
            shouldInitTango = false;
        }
    }

    /// <summary>
    /// Tango permissions event callback.
    /// </summary>
    /// <param name="permissionsGranted">If set to <c>true</c> permissions granted.</param>
    private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
    {
        if (permissionsGranted && !isInitialized)
        {
            isInitialized = true;
            shouldInitTango = true;
            tangoApplication.InitApplication();
            EventManager.Instance.SendTangoServiceInitialized();
        }
        else if (!permissionsGranted)
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
        }
    }
}
