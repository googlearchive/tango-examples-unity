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
using System.Threading;
using UnityEngine;
using Tango;

/// <summary>
/// Save ADF controller.
/// </summary>
public class SaveADFController : MonoBehaviour, ITangoEvent
{
    /// <summary>
    /// Width of the Saving ADF... text box.
    /// </summary>
    private const float SAVE_ADF_TEXT_WIDTH = 800;

    private TangoApplication m_tangoApplication;
    private TouchScreenKeyboard m_keyboard;
    private KeyboardState m_keyboardState;
    private string m_keyboardString;

    /// <summary>
    /// While saving an ADF, this holds the thread doing the saving.
    /// </summary>
    private Thread m_adfSaveThread;

    /// <summary>
    /// Time the ADF saving thread was started.
    /// </summary>
    private float m_adfSaveStartTime;

    /// <summary>
    /// Value from 0 - 1, where 0 is saving has just started and 1 is the saving completed.
    /// </summary>
    private float m_adfSavePercentComplete;

    /// <summary>
    /// If true, the ADF save operation failed.
    /// </summary>
    private bool m_adfSaveFailed;

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
    /// This is called each time a Tango event happens.
    /// </summary>
    /// <param name="tangoEvent">Tango event.</param>
    public void OnTangoEventAvailableEventHandler(Tango.TangoEvent tangoEvent)
    {
        if (tangoEvent.type == TangoEnums.TangoEventType.TANGO_EVENT_AREA_LEARNING
            && tangoEvent.event_key == "AreaDescriptionSaveProgress")
        {
            m_adfSavePercentComplete = float.Parse(tangoEvent.event_value);
        }
    }
    
     /// <summary>
     /// Start is called after Awake.Make any initilizations here.
     /// </summary>
    private void Start() 
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);

        m_keyboardState = KeyboardState.NONE;
    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    private void Update()
    {
        if (m_adfSaveThread != null && m_adfSaveThread.ThreadState != ThreadState.Running)
        {
            // After saving an ADF, the Tango Service will no longer provide pose updates until you reconnect.
            // Do this by reloading the current scene.
            Application.LoadLevel(Application.loadedLevel);
        }
    }

    /// <summary>
    /// Data logging GUI.
    /// </summary>
    private void OnGUI()
    {
        if (m_tangoApplication.m_enableAreaLearning)
        {
            if (m_adfSaveThread == null)
            {
                // Save thread is not active
                if (GUI.Button(new Rect(AreaLearningGUIController.UI_BUTTON_GAP_X, 
                                        Screen.height - (AreaLearningGUIController.UI_BUTTON_SIZE_Y + AreaLearningGUIController.UI_LABEL_GAP_Y),
                                        AreaLearningGUIController.UI_BUTTON_SIZE_X, 
                                        AreaLearningGUIController.UI_BUTTON_SIZE_Y), "<size=20>Save ADF</size>"))
                {
                    m_keyboardState = KeyboardState.OPEN;
                }
                KeyBoardBehaviour();
            }
            else if (!m_adfSaveFailed)
            {
                // Save thread is active.
                Color oldColor = GUI.color;
                GUI.color = Color.black;

                int numDots = ((int)(Time.time - m_adfSaveStartTime) % 3) + 1;
                string progressString = "<size=100>Saving ADF" + new string('.', numDots) + "</size>";
                GUI.Label(new Rect((Screen.width - SAVE_ADF_TEXT_WIDTH) / 2, Screen.height / 2, SAVE_ADF_TEXT_WIDTH, Screen.height / 2),
                          progressString);

                int percent = (int)(m_adfSavePercentComplete * 100);
                GUI.Label(new Rect((Screen.width + SAVE_ADF_TEXT_WIDTH) / 2, Screen.height / 2, SAVE_ADF_TEXT_WIDTH, Screen.height / 2),
                          string.Format("<size=100>({0}%)</size>", percent));

                GUI.color = oldColor;
            }
            else
            {
                // Save thread failed
                Color oldColor = GUI.color;
                GUI.color = Color.black;

                GUI.Label(new Rect((Screen.width - SAVE_ADF_TEXT_WIDTH) / 2, Screen.height / 2, SAVE_ADF_TEXT_WIDTH, Screen.height / 2),
                          "<size=100>Save failed</size>");

                GUI.color = oldColor;
            }
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

                // Do the save in a background thread, because it can take some time.
                m_adfSaveStartTime = Time.time;
                m_adfSavePercentComplete = 0;
                m_adfSaveFailed = false;

                // The above values can get written in the background thread, ensure the thread starts with those
                // values available.
                Thread.MemoryBarrier();

                m_adfSaveThread = new Thread(_SaveADFInBackground);
                m_adfSaveThread.Start(m_keyboardString);

                // While saving is going on, also disable anything that would display UI
                GameObject.FindObjectOfType<SceneSwitcher>().enabled = false;
                GameObject.FindObjectOfType<AreaLearningGUIController>().enabled = false;
                GameObject.FindObjectOfType<AreaLearningFPSCounter>().enabled = false;
                GameObject.FindObjectOfType<TangoGestureCamera>().enabled = false;
            }
            else
            {
                m_keyboardString = m_keyboard.text;
            }
        }
    }

    /// <summary>
    /// Thread method to save an ADF.  Make this the ThreadFunc.
    /// </summary>
    /// <param name="rawName">Name of the ADF.  Must be a string.</param>
    private void _SaveADFInBackground(object rawName)
    {
        UUIDUnityHolder adfUnityHolder = new UUIDUnityHolder();
        string name = (string)rawName;

        if (PoseProvider.SaveAreaDescription(adfUnityHolder) == Common.ErrorType.TANGO_SUCCESS)
        {
            PoseProvider.GetAreaDescriptionMetaData(adfUnityHolder);
            PoseProvider.AreaDescriptionMetaData_set(Common.MetaDataKeyType.KEY_NAME, name, adfUnityHolder);
            PoseProvider.SaveAreaDescriptionMetaData(adfUnityHolder);
        }
        else
        {
            // Setting a bool is atomic.
            m_adfSaveFailed = true;
        }
    }
}
