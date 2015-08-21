//-----------------------------------------------------------------------
// <copyright file="ADFPicker.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// An ADF picker button.
/// </summary>
public class ADFPicker : MonoBehaviour
{
    public Vector3 buttonOffsets;
    public GameObject adfPickerButtonPrototype;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Awake()
    {
        EventManager.TangoServiceInitialized += HandleEventTangoInitialized;
    }

    /// <summary>
    /// Callback when the Tango service is initialized.
    /// </summary>
    private void HandleEventTangoInitialized()
    {
        PoseProvider.RefreshADFList();
        RefreshADFPickerList(PoseProvider.GetCachedADFList());
    }

    /// <summary>
    /// Refreshs the ADF picker list UI.
    /// </summary>
    /// <param name="list">List to use.</param>
    private void RefreshADFPickerList(UUID_list list)
    {
        foreach (Transform child in transform)
        {
            Destroy(transform.gameObject);
        }
        int numberOfADFs = list.Count;
        Vector3 startPosition = transform.position;
        for (int i = 0; i < numberOfADFs; i++)
        {
            UUIDUnityHolder adf = list.GetADFAtIndex(i);
            Dictionary<string, string> adfMeta = adf.uuidMetaData.GetMetaDataKeyValues();
            string uuid = adfMeta["id"];
            string name = adfMeta["name"];
            GameObject button = (GameObject)Instantiate(adfPickerButtonPrototype, startPosition + (i * buttonOffsets), Quaternion.identity);
            button.GetComponent<ADFPickerButton>().SetTitles(name, uuid);
        }
    }

    /// <summary>
    /// Called when this game object is destroyed.
    /// </summary>
    private void OnDestory()
    {
        EventManager.TangoServiceInitialized -= HandleEventTangoInitialized;
    }
}
