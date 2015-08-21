//-----------------------------------------------------------------------
// <copyright file="SaveADFController.cs" company="Google">
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
/// Save ADF controller.
/// </summary>
public class SaveADFController : MonoBehaviour
{
    private TangoApplication m_tangoApplication;
    private TouchScreenKeyboard m_keyboard;
    private KeyboardState m_keyboardState;
    private UUIDUnityHolder m_adfUnityHolder;
    private string m_keyboardString;

    /// <summary>
    /// Describes the state, TouchScreenKeyBoard is in.
    /// </summary>
    public enum KeyboardState
    {
        NONE,
        OPEN,
        ACTIVE,
        DONE
    }
    
     /// <summary>
     /// Start is called after Awake.Make any initilizations here.
     /// </summary>
    private void Start() 
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_adfUnityHolder = new UUIDUnityHolder();
        m_keyboardState = KeyboardState.NONE;
    }

    /// <summary>
    /// Data logging GUI.
    /// </summary>
    private void OnGUI()
    {
        if (m_tangoApplication.m_enableAreaLearning)
        {
            if (GUI.Button(new Rect(Common.UI_BUTTON_GAP_X, 
                                    Screen.height - (Common.UI_BUTTON_SIZE_Y + Common.UI_LABEL_GAP_Y),
                                    Common.UI_BUTTON_SIZE_X, 
                                    Common.UI_BUTTON_SIZE_Y), "<size=20>Save ADF</size>"))
            {
                m_keyboardState = KeyboardState.OPEN;
            }
            KeyBoardBehaviour();
        }
    }

    /// <summary>
    /// Enables keyboard when the save button is pressed and saves the ADF with required metadata when Keyboard is
    /// done.
    /// </summary>
    private void KeyBoardBehaviour()
    {
        if (m_keyboardState == KeyboardState.OPEN)
        {
            m_keyboard = TouchScreenKeyboard.Open(m_keyboardString, TouchScreenKeyboardType.Default, false);
            m_keyboardState = KeyboardState.ACTIVE;
        }
        if (m_keyboard != null)
        {   
            if (m_keyboard.done && m_keyboardState != KeyboardState.DONE)
            {
                m_keyboardState = KeyboardState.DONE;
                m_keyboardString = m_keyboard.text;
                PoseProvider.SaveAreaDescription(m_adfUnityHolder);
                PoseProvider.GetAreaDescriptionMetaData(m_adfUnityHolder);
                PoseProvider.AreaDescriptionMetaData_set(Common.MetaDataKeyType.KEY_NAME, m_keyboardString, m_adfUnityHolder);
                PoseProvider.SaveAreaDescriptionMetaData(m_adfUnityHolder);
            }
            else
            {
                m_keyboardString = m_keyboard.text;
            }
        }
    }
}
