//-----------------------------------------------------------------------
// <copyright file="ADMGUIController.cs" company="Google">
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
using System.Collections.Generic;
using System.Threading;
using Tango;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class for all the UI interaction in the AreaDescriptionManagement sample.
/// </summary>
public class ADMGUIController : MonoBehaviour, ITangoLifecycle, ITangoEvent
{
    /// <summary>
    /// Parent of the Area Description management screen.
    /// </summary>
    public GameObject m_managementRoot;

    /// <summary>
    /// Parent of the Area Description quality screen.
    /// </summary>
    public GameObject m_qualityRoot;

    /// <summary>
    /// UI parent of the list of Area Descriptions on the device.
    /// </summary>
    public RectTransform m_listParent;

    /// <summary>
    /// UI prefab for each element in the list of Area Descriptions on the device.
    /// </summary>
    public AreaDescriptionListElement m_listElement;

    /// <summary>
    /// UI to enable when m_ListParent has no children.
    /// </summary>
    public RectTransform m_listEmptyText;

    /// <summary>
    /// UI parent of the selected Area Description's details.
    /// </summary>
    public RectTransform m_detailsParent;

    /// <summary>
    /// Read-only UI for the selected Area Description's date.
    /// </summary>
    public Text m_detailsDate;

    /// <summary>
    /// Editable UI for the selected Area Description's human readable name.
    /// </summary>
    public InputField m_detailsEditableName;

    /// <summary>
    /// Editable UI for the selected Area Description's X position.
    /// </summary>
    public InputField m_detailsEditablePosX;

    /// <summary>
    /// Editable UI for the selected Area Description's Y position.
    /// </summary>
    public InputField m_detailsEditablePosY;

    /// <summary>
    /// Editable UI for the selected Area Description's Z position.
    /// </summary>
    public InputField m_detailsEditablePosZ;

    /// <summary>
    /// Editable UI for the selected Area Description's qX rotation.
    /// </summary>
    public InputField m_detailsEditableRotQX;

    /// <summary>
    /// Editable UI for the selected Area Description's qY rotation.
    /// </summary>
    public InputField m_detailsEditableRotQY;

    /// <summary>
    /// Editable UI for the selected Area Description's qZ rotation.
    /// </summary>
    public InputField m_detailsEditableRotQZ;

    /// <summary>
    /// Editable UI for the selected Area Description's qW rotation.
    /// </summary>
    public InputField m_detailsEditableRotQW;

    /// <summary>
    /// The reference of the TangoDeltaPoseController object.
    ///
    /// TangoDeltaPoseController listens to pose updates and applies the correct pose to itself and its built-in camera.
    /// </summary>
    public TangoDeltaPoseController m_deltaPoseController;

    /// <summary>
    /// Saving progress UI parent.
    /// </summary>
    public RectTransform m_savingTextParent;

    /// <summary>
    /// Saving progress UI text.
    /// </summary>
    public Text m_savingText;

    /// <summary>
    /// TangoApplication for this scene.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Currently selected Area Description.
    /// </summary>
    private AreaDescription m_selectedAreaDescription;

    /// <summary>
    /// Currently selected Area Description's metadata.
    /// </summary>
    private AreaDescription.Metadata m_selectedMetadata;

    /// <summary>
    /// The background thread saving occurs on.
    /// </summary>
    private Thread m_saveThread;

#if UNITY_EDITOR
    /// <summary>
    /// Handles GUI text input in Editor where there is no device keyboard.
    /// If true, text input for naming new saved Area Description is displayed.
    /// </summary>
    private bool m_displayGuiTextInput;

    /// <summary>
    /// Handles GUI text input in Editor where there is no device keyboard.
    /// Contains text data for naming new saved Area Descriptions.
    /// </summary>
    private string m_guiTextInputContents;

    /// <summary>
    /// Handles GUI text input in Editor where there is no device keyboard.
    /// Indicates whether last text input was ended with confirmation or cancellation.
    /// </summary>
    private bool m_guiTextInputResult;
#endif

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        if (m_saveThread != null && m_saveThread.ThreadState != ThreadState.Running)
        {
            // After saving an Area Description, we reload the scene to restart the game.
            #pragma warning disable 618
            Application.LoadLevel(Application.loadedLevel);
            #pragma warning restore 618
        }

        // Pressing the back button should popup the management window if you are not in the management screen,
        // otherwise it can quit.
        if (Input.GetKey(KeyCode.Escape))
        {
            if (m_managementRoot.activeSelf)
            {
                // This is a fix for a lifecycle issue where calling
                // Application.Quit() here, and restarting the application
                // immediately results in a deadlocked app.
                AndroidHelper.AndroidQuit();
            }
            else
            {
                #pragma warning disable 618
                Application.LoadLevel(Application.loadedLevel);
                #pragma warning restore 618
            }
        }
    }

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();

        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);

            if (AndroidHelper.IsTangoCorePresent())
            {
                m_tangoApplication.RequestPermissions();
            }
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }
    }

    /// <summary>
    /// Application onPause / onResume callback.
    /// </summary>
    /// <param name="pauseStatus"><c>true</c> if the application about to pause, otherwise <c>false</c>.</param>
    public void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && !m_managementRoot.activeSelf)
        {
            // When application is backgrounded, we reload the level because the Tango Service is disconected. All
            // learned area and placed marker should be discarded as they are not saved.
            #pragma warning disable 618
            Application.LoadLevel(Application.loadedLevel);
            #pragma warning restore 618
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Unity OnGUI.
    ///
    /// Handles text input when there is no device keyboard in the editor.
    /// </summary>
    public void OnGUI()
    {
        if (m_displayGuiTextInput)
        {
            Rect textBoxRect = new Rect(100,
                                        Screen.height - 200,
                                        Screen.width - 200,
                                        100);

            Rect okButtonRect = textBoxRect;
            okButtonRect.y += 100;
            okButtonRect.width /= 2;

            Rect cancelButtonRect = okButtonRect;
            cancelButtonRect.x = textBoxRect.center.x;

            GUI.SetNextControlName("TextField");
            GUIStyle customTextFieldStyle = new GUIStyle(GUI.skin.textField);
            customTextFieldStyle.alignment = TextAnchor.MiddleCenter;
            m_guiTextInputContents =
                GUI.TextField(textBoxRect, m_guiTextInputContents, customTextFieldStyle);
            GUI.FocusControl("TextField");

            if (GUI.Button(okButtonRect, "OK")
                || (Event.current.type == EventType.keyDown && Event.current.character == '\n'))
            {
                m_displayGuiTextInput = false;
                m_guiTextInputResult = true;
            }
            else if (GUI.Button(cancelButtonRect, "Cancel"))
            {
                m_displayGuiTextInput = false;
                m_guiTextInputResult = false;
            }
        }
    }
#endif

    /// <summary>
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            RefreshAreaDescriptionList();
        }
    }

    /// <summary>
    /// This is called when successfully connected to the Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
    }

    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// <summary>
    /// This is called each time a Tango event happens.
    /// </summary>
    /// <param name="tangoEvent">Tango event.</param>
    public void OnTangoEventAvailableEventHandler(Tango.TangoEvent tangoEvent)
    {
        // We will not have the saving progress when the learning mode is off.
        if (!m_tangoApplication.m_areaDescriptionLearningMode)
        {
            return;
        }

        if (tangoEvent.type == TangoEnums.TangoEventType.TANGO_EVENT_AREA_LEARNING
            && tangoEvent.event_key == "AreaDescriptionSaveProgress")
        {
            m_savingText.text = "Saving... " + Mathf.RoundToInt(float.Parse(tangoEvent.event_value) * 100) + "%";
        }
    }

    /// <summary>
    /// Refresh the UI list of Area Descriptions.
    /// </summary>
    public void RefreshAreaDescriptionList()
    {
        AreaDescription[] areaDescriptions = AreaDescription.GetList();
        _SelectAreaDescription(null);

        // Always remove all old children.
        foreach (Transform child in m_listParent)
        {
            Destroy(child.gameObject);
        }

        if (areaDescriptions != null)
        {
            // Add new children
            ToggleGroup toggleGroup = GetComponent<ToggleGroup>();
            foreach (AreaDescription areaDescription in areaDescriptions)
            {
                AreaDescriptionListElement button = GameObject.Instantiate(m_listElement) as AreaDescriptionListElement;
                button.m_areaDescriptionName.text = areaDescription.GetMetadata().m_name;
                button.m_areaDescriptionUUID.text = areaDescription.m_uuid;
                button.m_toggle.group = toggleGroup;

                // Ensure the lambda gets a copy of the reference to areaDescription in its current state.
                // (See https://resnikb.wordpress.com/2009/06/17/c-lambda-and-foreach-variable/)
                AreaDescription lambdaParam = areaDescription;
                button.m_toggle.onValueChanged.AddListener((value) => _OnAreaDescriptionToggleChanged(lambdaParam, value));
                button.transform.SetParent(m_listParent, false);
            }

            m_listEmptyText.gameObject.SetActive(false);
        }
        else
        {
            m_listEmptyText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Start quality mode, creating a brand new Area Description.
    /// </summary>
    public void NewAreaDescription()
    {
        m_tangoApplication.Startup(null);

        // Disable the management UI, we are now in the world.
        m_managementRoot.SetActive(false);
        m_qualityRoot.SetActive(true);
    }

    /// <summary>
    /// Start quality mode, extending an existing Area Description.
    /// </summary>
    public void ExtendSelectedAreaDescription()
    {
        if (m_selectedAreaDescription == null)
        {
            AndroidHelper.ShowAndroidToastMessage("You must have a selected Area Description to extend");
            return;
        }

        m_tangoApplication.Startup(m_selectedAreaDescription);

        // Disable the management UI, we are now in the world.
        m_managementRoot.SetActive(false);
        m_qualityRoot.SetActive(true);
    }

    /// <summary>
    /// Import an Area Description.
    /// </summary>
    public void ImportAreaDescription()
    {
        StartCoroutine(_DoImportAreaDescription());
    }

    /// <summary>
    /// Export an Area Description.
    /// </summary>
    public void ExportSelectedAreaDescription()
    {
        if (m_selectedAreaDescription != null)
        {
            StartCoroutine(_DoExportAreaDescription(m_selectedAreaDescription));
        }
    }

    /// <summary>
    /// Delete the selected Area Description.
    /// </summary>
    public void DeleteSelectedAreaDescription()
    {
        if (m_selectedAreaDescription != null)
        {
            m_selectedAreaDescription.Delete();
            RefreshAreaDescriptionList();
        }
    }

    /// <summary>
    /// Save changes made to the selected Area Description's metadata.
    /// </summary>
    public void SaveSelectedAreaDescriptionMetadata()
    {
        if (m_selectedAreaDescription != null && m_selectedMetadata != null)
        {
            m_selectedMetadata.m_name = m_detailsEditableName.text;
            double.TryParse(m_detailsEditablePosX.text, out m_selectedMetadata.m_transformationPosition[0]);
            double.TryParse(m_detailsEditablePosY.text, out m_selectedMetadata.m_transformationPosition[1]);
            double.TryParse(m_detailsEditablePosZ.text, out m_selectedMetadata.m_transformationPosition[2]);
            double.TryParse(m_detailsEditableRotQX.text, out m_selectedMetadata.m_transformationRotation[0]);
            double.TryParse(m_detailsEditableRotQY.text, out m_selectedMetadata.m_transformationRotation[1]);
            double.TryParse(m_detailsEditableRotQZ.text, out m_selectedMetadata.m_transformationRotation[2]);
            double.TryParse(m_detailsEditableRotQW.text, out m_selectedMetadata.m_transformationRotation[3]);

            m_selectedAreaDescription.SaveMetadata(m_selectedMetadata);
            RefreshAreaDescriptionList();
        }
    }

    /// <summary>
    /// When in quality mode, save the current Area Description and switch back to management mode.
    /// </summary>
    public void SaveCurrentAreaDescription()
    {
        StartCoroutine(_DoSaveCurrentAreaDescription());
    }

    /// <summary>
    /// Actually do the Area Description import.
    ///
    /// This runs over multiple frames, so a Unity coroutine is used.
    /// </summary>
    /// <returns>Coroutine IEnumerator.</returns>
    private IEnumerator _DoImportAreaDescription()
    {
        if (TouchScreenKeyboard.visible)
        {
            yield break;
        }

        TouchScreenKeyboard kb = TouchScreenKeyboard.Open("/sdcard/", TouchScreenKeyboardType.Default, false);
        while (!kb.done && !kb.wasCanceled)
        {
            yield return null;
        }

        if (kb.done)
        {
            AreaDescription.ImportFromFile(kb.text);
        }
    }

    /// <summary>
    /// Actually do the Area description export.
    ///
    /// This runs over multiple frames, so a Unity coroutine is used.
    /// </summary>
    /// <returns>Coroutine IEnumerator.</returns>
    /// <param name="areaDescription">Area Description to export.</param>
    private IEnumerator _DoExportAreaDescription(AreaDescription areaDescription)
    {
        if (TouchScreenKeyboard.visible)
        {
            yield break;
        }

        TouchScreenKeyboard kb = TouchScreenKeyboard.Open("/sdcard/", TouchScreenKeyboardType.Default, false);
        while (!kb.done && !kb.wasCanceled)
        {
            yield return null;
        }

        if (kb.done)
        {
            areaDescription.ExportToFile(kb.text);
        }
    }

    /// <summary>
    /// Called every time an Area Description toggle changes state.
    /// </summary>
    /// <param name="areaDescription">Area Description the toggle is for.</param>
    /// <param name="value">The new state of the toggle.</param>
    private void _OnAreaDescriptionToggleChanged(AreaDescription areaDescription, bool value)
    {
        if (value)
        {
            _SelectAreaDescription(areaDescription);
        }
        else
        {
            _SelectAreaDescription(null);
        }
    }

    /// <summary>
    /// Select a specific Area Description to show details.
    /// </summary>
    /// <param name="areaDescription">Area Description to show details for, or <c>null</c> if details should be hidden.</param>
    private void _SelectAreaDescription(AreaDescription areaDescription)
    {
        m_selectedAreaDescription = areaDescription;

        if (areaDescription != null)
        {
            m_selectedMetadata = areaDescription.GetMetadata();
            m_detailsParent.gameObject.SetActive(true);

            m_detailsDate.text = m_selectedMetadata.m_dateTime.ToLongDateString() + ", " + m_selectedMetadata.m_dateTime.ToLongTimeString();

            m_detailsEditableName.text = m_selectedMetadata.m_name;
            m_detailsEditablePosX.text = m_selectedMetadata.m_transformationPosition[0].ToString();
            m_detailsEditablePosY.text = m_selectedMetadata.m_transformationPosition[1].ToString();
            m_detailsEditablePosZ.text = m_selectedMetadata.m_transformationPosition[2].ToString();
            m_detailsEditableRotQX.text = m_selectedMetadata.m_transformationRotation[0].ToString();
            m_detailsEditableRotQY.text = m_selectedMetadata.m_transformationRotation[1].ToString();
            m_detailsEditableRotQZ.text = m_selectedMetadata.m_transformationRotation[2].ToString();
            m_detailsEditableRotQW.text = m_selectedMetadata.m_transformationRotation[3].ToString();
        }
        else
        {
            m_selectedMetadata = null;
            m_detailsParent.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Actually do the Area Description save.
    /// </summary>
    /// <returns>Coroutine IEnumerator.</returns>
    private IEnumerator _DoSaveCurrentAreaDescription()
    {
#if UNITY_EDITOR
        // Work around lack of on-screen keyboard in editor:
        if (m_displayGuiTextInput || m_saveThread != null)
        {
            yield break;
        }

        m_displayGuiTextInput = true;
        m_guiTextInputContents = "Unnamed";
        while (m_displayGuiTextInput)
        {
            yield return null;
        }
#else
        if (TouchScreenKeyboard.visible || m_saveThread != null)
        {
            yield break;
        }

        TouchScreenKeyboard kb = TouchScreenKeyboard.Open("Unnamed");
        while (!kb.done && !kb.wasCanceled)
        {
            yield return null;
        }

        // Store name so it is available when we use it from thread delegate.
        var fileNameFromKeyboard = kb.text;
#endif

        // Save the text in a background thread.
        m_savingTextParent.gameObject.SetActive(true);
        m_saveThread = new Thread(delegate()
        {
            // Save the name put in with the Area Description.
            AreaDescription areaDescription = AreaDescription.SaveCurrent();
            AreaDescription.Metadata metadata = areaDescription.GetMetadata();
#if UNITY_EDITOR
            metadata.m_name = m_guiTextInputContents;
#else
            metadata.m_name = fileNameFromKeyboard;
#endif
            areaDescription.SaveMetadata(metadata);
        });

        m_saveThread.Start();
    }
}
