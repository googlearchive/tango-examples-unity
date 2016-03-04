//-----------------------------------------------------------------------
// <copyright file="NetworkingInfo.cs" company="Google">
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
/// Networking debug information.
/// </summary>
public class NetworkingInfo : MonoBehaviour
{
    /// <summary>
    /// Debug Text UI.
    /// </summary>
    public Text text;

    private string m_networkingInfoString = "n/a";

    /// <summary>
    /// Unity update function.
    /// </summary>
    public void Update()
    {
        text.text = PhotonNetwork.connectionStateDetailed.ToString();
        
        if (m_networkingInfoString != PhotonNetwork.connectionStateDetailed.ToString())
        {
            Debug.Log("PhotonNetwork State:" + PhotonNetwork.connectionStateDetailed.ToString());
        }

        m_networkingInfoString = PhotonNetwork.connectionStateDetailed.ToString();
    }
}