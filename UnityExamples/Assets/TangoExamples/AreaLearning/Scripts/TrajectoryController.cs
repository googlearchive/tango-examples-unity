//-----------------------------------------------------------------------
// <copyright file="TrajectoryController.cs" company="Google">
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
/// Switches between using the Blue and Green trails based on if localized or not.
/// </summary>
public class TrajectoryController : MonoBehaviour
{
    public AreaLearningPoseController m_sampleController;
    private GameObject m_blueTrajectory;
    private GameObject m_greenTrajectory;
    
    /// <summary>
    /// Used to initialize objects.
    /// </summary>
    private void Awake() 
    {
        m_blueTrajectory = GameObject.Find("BlueTrajectory");
        m_greenTrajectory = GameObject.Find("GreenTrajectory");
    }
    
    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update() 
    {
        if (m_sampleController.IsLocalized())
        {
            m_greenTrajectory.transform.position = m_sampleController.transform.position;
        }
        else
        {
            m_blueTrajectory.transform.position = m_sampleController.transform.position;
        }
    }
}
