//-----------------------------------------------------------------------
// <copyright file="ManagerSingleton.cs" company="Google">
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
/// Manager singleton.
/// </summary>
public class ManagerSingleton : MonoBehaviour
{
    private static ManagerSingleton m_instance;

    /// <summary>
    /// The singleton instance.
    /// </summary>
    /// <value>The instance.</value>
    public static ManagerSingleton Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType<ManagerSingleton>();
                DontDestroyOnLoad(m_instance.gameObject);
            }
            
            return m_instance;
        }
    }

    /// <summary>
    /// Do initialization here.
    /// </summary>
    public void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != m_instance)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
