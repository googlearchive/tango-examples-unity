//-----------------------------------------------------------------------
// <copyright file="AreaDescriptionPicker.cs" company="Google">
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
using System.IO;
using Tango;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// List controller of the scrolling list.
/// 
/// This list controller present a toggle group of Tango space Area Descriptions. The list class also has interface
/// to start the game and connect to Tango Service.
/// </summary>
public class AreaDescriptionPicker : MonoBehaviour, ITangoLifecycle
{
    /// <summary>
    /// The prefab of a standard button in the scrolling list.
    /// </summary>
    public GameObject m_listElement;

    /// <summary>
    /// The container panel of the Tango space Area Description scrolling list.
    /// </summary>
    public RectTransform m_listContentParent;

    /// <summary>
    /// Toggle group for the Area Description list.
    /// 
    /// You can only toggle one Area Description at a time. After we get the list of Area Description from Tango,
    /// they are all added to this toggle group.
    /// </summary>
    public ToggleGroup m_toggleGroup;

    /// <summary>
    /// Enable learning mode toggle.
    /// 
    /// Learning Mode allows the loaded Area Description to be extended with more knowledge about the area..
    /// </summary>
    public Toggle m_enableLearningToggle;

    /// <summary>
    /// The reference of the TangoPoseController object.
    /// 
    /// TangoPoseController listens to pose updates and applies the correct pose to itself and its built-in camera.
    /// </summary>
    public TangoPoseController m_poseController;

    /// <summary>
    /// Control panel game object.
    /// 
    /// The panel will be enabled when the game starts.
    /// </summary>
    public GameObject m_gameControlPanel;

    /// <summary>
    /// The GUI controller.
    /// 
    /// GUI controller will be enabled when the game starts.
    /// </summary>
    public AreaLearningInGameController m_guiController;

    /// <summary>
    /// A reference to TangoApplication instance.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// The UUID of the selected Area Description.
    /// </summary>
    private string m_curAreaDescriptionUUID;

    /// <summary>
    /// Start the game.
    /// 
    /// This will start the service connection, and start pose estimation from Tango Service.
    /// </summary>
    /// <param name="isNewAreaDescription">If set to <c>true</c> game with start to learn a new Area 
    /// Description.</param>
    public void StartGame(bool isNewAreaDescription)
    {
        // The game has to be started with an Area Description.
        if (!isNewAreaDescription)
        {
            if (string.IsNullOrEmpty(m_curAreaDescriptionUUID))
            {
                AndroidHelper.ShowAndroidToastMessage("Please choose an Area Description.");
                return;
            }
        }
        else
        {
            m_curAreaDescriptionUUID = null;
        }

        // Dismiss Area Description list, footer and header UI panel.
        gameObject.SetActive(false);

        if (isNewAreaDescription)
        {
            // Completely new area description.
            m_guiController.m_curAreaDescription = null;
            m_tangoApplication.m_areaDescriptionLearningMode = true;
        }
        else
        {
            // Load up an existing Area Description.
            AreaDescription areaDescription = AreaDescription.ForUUID(m_curAreaDescriptionUUID);
            m_guiController.m_curAreaDescription = areaDescription;
            m_tangoApplication.m_areaDescriptionLearningMode = m_enableLearningToggle.isOn;
        }

        m_tangoApplication.Startup(m_guiController.m_curAreaDescription);

        // Enable GUI controller to allow user tap and interactive with the environment.
        m_poseController.gameObject.SetActive(true);
        m_guiController.enabled = true;
        m_gameControlPanel.SetActive(true);
    }

    /// <summary>
    /// Internal callback when a permissions event happens.
    /// </summary>
    /// <param name="permissionsGranted">If set to <c>true</c> permissions granted.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            _PopulateList();
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking and Area Learning Permissions Needed");
            
            // This is a fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application
            // immediately results in a deadlocked app.
            AndroidHelper.AndroidQuit();
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
    /// Unity Start function.
    /// 
    /// This function is responsible for connecting callbacks, set up TangoApplication and initialize the data list.
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
    /// Unity Update function.
    /// 
    /// Application will be closed when click the back button.
    /// </summary>
    public void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            // This is a fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application
            // immediately results in a deadlocked app.
            AndroidHelper.AndroidQuit();
        }
    }

    /// <summary>
    /// Refresh the scrolling list's content for both list.
    /// 
    /// This function will query from the Tango API for the Tango space Area Description. Also, when it populates 
    /// the scrolling list content, it will connect the delegate for each button in the list. The delegate is
    /// responsible for the actual import/export  through the Tango API.
    /// </summary>
    private void _PopulateList()
    {
        foreach (Transform t in m_listContentParent.transform)
        {
            Destroy(t.gameObject);
        }

        // Update Tango space Area Description list.
        AreaDescription[] areaDescriptionList = AreaDescription.GetList();

        if (areaDescriptionList == null)
        {
            return;
        }

        foreach (AreaDescription areaDescription in areaDescriptionList)
        {
            GameObject newElement = Instantiate(m_listElement) as GameObject;
            AreaDescriptionListElement listElement = newElement.GetComponent<AreaDescriptionListElement>();
            listElement.m_toggle.group = m_toggleGroup;
            listElement.m_areaDescriptionName.text = areaDescription.GetMetadata().m_name;
            listElement.m_areaDescriptionUUID.text = areaDescription.m_uuid;

            // Ensure the lambda makes a copy of areaDescription.
            AreaDescription lambdaParam = areaDescription;
            listElement.m_toggle.onValueChanged.AddListener((value) => _OnToggleChanged(lambdaParam, value));
            newElement.transform.SetParent(m_listContentParent.transform, false);
        }
    }

    /// <summary>
    /// Callback function when toggle button is selected.
    /// </summary>
    /// <param name="item">Caller item object.</param>
    /// <param name="value">Selected value of the toggle button.</param>
    private void _OnToggleChanged(AreaDescription item, bool value)
    {
        if (value)
        {
            m_curAreaDescriptionUUID = item.m_uuid;
        }
    }
}
