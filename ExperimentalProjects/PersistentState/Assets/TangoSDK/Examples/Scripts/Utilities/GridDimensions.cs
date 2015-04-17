/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections;
using UnityEngine;

/// <summary>
/// Displays the status messages like sparse map saved, loaded succesfully
/// Disables the starter screen when record/ load is pressed.
/// </summary>
public class GridDimensions : MonoBehaviour
{
    // status message string for updates
    private string m_status;

    /// <summary>
    /// Update the status.
    /// </summary>
    private void Update()
    {
        _UpdateStatus(" grid Length = 0.5m \n" + " grid Width = 0.5m");
    }

    /// <summary>
    /// Used to display things on GUI.
    /// </summary>
    private void OnGUI()
	{
        GUI.Label(new Rect(Screen.width - 225, Screen.height - 100, 1300, 100),
            "<size=20>Scale: \n" + m_status + "</size>");
	}

    /// <summary>
    /// Update debug status messages here.
    /// </summary>
    /// <param name="newStatus">The new debug message you want to see on screen.</param>
    private void _UpdateStatus(string newStatus)
	{
        m_status = newStatus;
	}
}
