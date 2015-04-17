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
using UnityEngine;
using System.Collections;

/// <summary>
/// Helper to manually reset motion tracking.
/// </summary>
public class ResetMotionTracking : MonoBehaviour
{
    private const int RESET_BUTTON_WIDTH = 200;
    private const int RESET_BUTTON_HEIGHT = 75;

    private bool m_shouldReset;
    private Rect m_resetButtonRect;

    public void ShowResetButton()
    {
        m_shouldReset = true;
    }

	void Start ()
    {
        m_shouldReset = false;
        m_resetButtonRect = new Rect(Screen.width * 0.5f - RESET_BUTTON_WIDTH,
                                     Screen.height * 0.5f - RESET_BUTTON_HEIGHT,
                                     RESET_BUTTON_WIDTH,
                                     RESET_BUTTON_HEIGHT);

	}
	
    private void OnGUI()
    {
        if (m_shouldReset)
        {
            if(GUI.Button(m_resetButtonRect, "<size=30>RESET</size>"))
            {
                Tango.PoseProvider.ResetMotionTracking();
                m_shouldReset = false;
            }
        }
    }
}
