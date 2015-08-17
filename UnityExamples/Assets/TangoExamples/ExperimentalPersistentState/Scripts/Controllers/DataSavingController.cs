//-----------------------------------------------------------------------
// <copyright file="DataSavingController.cs" company="Google">
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
using Tango;

/// <summary>
/// Controls saving and loading ADFs.
/// </summary>
public class DataSavingController : MonoBehaviour
{
    private static TouchScreenKeyboard keyboard;
    private static string keyboardString;
    private static UUIDUnityHolder savedAdfHolder;
    private static bool startedSaving = false;
    
    /// <summary>
    /// Start the saving process.
    /// </summary>
    public static void SaveData()
    {
        keyboard = TouchScreenKeyboard.Open(keyboardString, TouchScreenKeyboardType.Default, false);
        startedSaving = true;
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        if (startedSaving)
        {
            if (keyboard.done)
            {
                savedAdfHolder = new UUIDUnityHolder();
                keyboardString = keyboard.text;
                PoseProvider.SaveAreaDescription(savedAdfHolder);
                PoseProvider.GetAreaDescriptionMetaData(savedAdfHolder);
                PoseProvider.AreaDescriptionMetaData_set(Common.MetaDataKeyType.KEY_NAME, keyboardString, savedAdfHolder);
                PoseProvider.SaveAreaDescriptionMetaData(savedAdfHolder);
                
                // Null terminator will cause invalid argument in the file system.
                string uuid = savedAdfHolder.GetStringDataUUID().Replace("\0", string.Empty);
                string path = Application.persistentDataPath + "/" + uuid;
                FileParser.SaveBuildingDataToPath(BuildingManager.Instance.buildingList, path);
                EventManager.Instance.SendGameDataSaved(true);
                startedSaving = false;
            }
        }
    }
}
