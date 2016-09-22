//-----------------------------------------------------------------------
// <copyright file="AreaDescriptionListElement.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
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
using UnityEngine.UI;

/// <summary>
/// Item object of the Area Description File list.
/// </summary>
public class AreaDescriptionListElement : MonoBehaviour
{
    /// <summary>
    /// The toggle game object.
    /// </summary>
    public Toggle m_toggle;

    /// <summary>
    /// The name text view for displaying the Area Description's human readable name.
    /// </summary>
    public Text m_areaDescriptionName;

    /// <summary>
    /// The UUID text view for displaying the Area Description's UUID.
    /// </summary>
    public Text m_areaDescriptionUUID;
}
