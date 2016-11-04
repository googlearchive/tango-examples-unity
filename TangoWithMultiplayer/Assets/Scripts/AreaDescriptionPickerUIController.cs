//-----------------------------------------------------------------------
// <copyright file="AreaDescriptionPickerUIController.cs" company="Google">
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tango;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UIController for the AreaDescriptionPicker scene.
/// </summary>
public class AreaDescriptionPickerUIController : Photon.PunBehaviour, ITangoLifecycle
{
    /// <summary>
    /// The prefab of a standard button in the scrolling list.
    /// </summary>
    public GameObject m_listElement;

    /// <summary>
    /// The container for Area Description list elements to get added to.
    /// </summary>
    public RectTransform m_listContentParent;

    /// <summary>
    /// Toggle group for the Area Description list.
    /// 
    /// You can only select one Area Description at a time. To enforce this, every list element gets added to this 
    /// toggle group.
    /// </summary>
    public ToggleGroup m_toggleGroup;

    /// <summary>
    /// Unity Start function.
    /// </summary>
    public void Start()
    {
        TangoApplication tangoApplication = FindObjectOfType<TangoApplication>();
        
        if (tangoApplication != null)
        {
            tangoApplication.Register(this);
            if (AndroidHelper.IsTangoCorePresent())
            {
                tangoApplication.RequestPermissions();
            }
        }
        else
        {
            Debug.LogError("No Tango Manager found in scene." + Environment.StackTrace);
            return;
        }
#if UNITY_EDITOR
        PhotonNetwork.ConnectUsingSettings("0.1");
#endif
    }
    
    /// <summary>
    /// Unity Update function.
    /// 
    /// Quit the application when the back button is clicked.
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
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            _PopulateList();
            if (!PhotonNetwork.connected)
            {
                PhotonNetwork.ConnectUsingSettings("0.1");
            }
            
            if (PhotonNetwork.inRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Tango permission needed");
            Application.Quit();
        }
    }
    
    /// <summary>
    /// This is called when succesfully connected to the Tango service.
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
    /// Photon callback when the client is connect to the master server.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    /// <summary>
    /// Join or host a game.
    /// </summary>
    /// <param name="isJoin">If the client is hosting or joining game.</param>
    public void JoinOrCreateGame(bool isJoin)
    {
        if (!PhotonNetwork.insideLobby)
        {
            AndroidHelper.ShowAndroidToastMessage("Please wait to join or create the room until you are in lobby.");
            Debug.Log("Please wait to join or create the room until you are in lobby." + Environment.StackTrace);
            return;
        }

        if (isJoin)
        {
            Globals.m_curAreaDescription = null;
            if (PhotonNetwork.GetRoomList().Length == 0)
            {
                AndroidHelper.ShowAndroidToastMessage("There's no room in the lobby.");
                Debug.Log("There's no room in the lobby." + Environment.StackTrace);
                return;
            }
        }
        else
        {
#if UNITY_EDITOR
            Globals.m_curAreaDescription = AreaDescription.ForUUID("abc");
#else
            if (Globals.m_curAreaDescription == null)
            {
                AndroidHelper.ShowAndroidToastMessage("No Area Description selected.");
                Debug.Log("No Area Description selected." + Environment.StackTrace);
                return;
            }
#endif
        }

        Application.LoadLevel("MultiplayerCubeStacker");
    }

    /// <summary>
    /// Refresh the Area Description list's UI content.
    /// 
    /// This function updates the list's UI based on local Area Descriptions. It also hooks up a callback to each
    /// element to get notified when the selected Area Description changes.
    /// </summary>
    private void _PopulateList()
    {
        foreach (Transform t in m_listContentParent.transform)
        {
            Destroy(t.gameObject);
        }

        AreaDescription[] areaDescriptionList = AreaDescription.GetList();

        if (areaDescriptionList == null)
        {
            return;
        }

        foreach (AreaDescription areaDescription in areaDescriptionList)
        {
            GameObject newElement = Instantiate<GameObject>(m_listElement);
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
            Globals.m_curAreaDescription = item;
        }
    }
}
