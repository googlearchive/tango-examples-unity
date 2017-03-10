// <copyright file="PointCloudFPSCounter.cs" company="Google">
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
using Tango;
using UnityEngine;

/// <summary>
/// FPS counter.
/// </summary>
public class PointCloudFPSCounter : MonoBehaviour
{
    public float m_updateFrequency = 1.0f;

    public string m_FPSText;
    private int m_currentFPS;
    private int m_framesSinceUpdate;
    private float m_accumulation;
    private float m_currentTime;

    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        m_currentFPS = 0;
        m_framesSinceUpdate = 0;
        m_currentTime = 0.0f;
        m_FPSText = "FPS = Calculating";
        m_tangoApplication = FindObjectOfType<TangoApplication>();
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        m_currentTime += Time.deltaTime;
        ++m_framesSinceUpdate;
        m_accumulation += Time.timeScale / Time.deltaTime;
        if (m_currentTime >= m_updateFrequency)
        {
            m_currentFPS = (int)(m_accumulation / m_framesSinceUpdate);
            m_currentTime = 0.0f;
            m_framesSinceUpdate = 0;
            m_accumulation = 0.0f;
            m_FPSText = "FPS: " + m_currentFPS;
        }
    }

    /// <summary>
    /// Unity GUI callback.
    /// </summary>
    public void OnGUI()
    {
        if (m_tangoApplication.HasRequiredPermissions)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.black;

            GUI.Label(new Rect(PointCloudGUIController.UI_LABEL_START_X, PointCloudGUIController.UI_FPS_LABEL_START_Y, PointCloudGUIController.UI_LABEL_SIZE_X, PointCloudGUIController.UI_LABEL_SIZE_Y),
                      PointCloudGUIController.UI_FONT_SIZE + m_FPSText + "</size>");

            GUI.color = oldColor;
        }
    }
}
