//-----------------------------------------------------------------------
// <copyright file="BuildingPickerButton.cs" company="Google">
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
/// Building picker button.
/// </summary>
public class BuildingPickerButton : TouchableObject
{
    public int buildingId;

    /// <summary>
    /// Touch event similar to key hold.
    /// </summary>
    protected override void OnTouch()
    {
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    protected override void OutTouch()
    {
        if (!Statics.isPlacingObject)
        {
            BuildingManager.Instance.CreateBulding(buildingId);
        }
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    protected override void TouchUp()
    {
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    protected override void Update()
    {
        base.Update();
    }
}
