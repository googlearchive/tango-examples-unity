//-----------------------------------------------------------------------
// <copyright file="MultiplayerCubeStackerUIController.cs" company="Google">
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
using System.Xml;
using System.Xml.Serialization;
using Tango;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIController for the MultiplayerCubeStacker scene.
///
/// This controller does the following three things:
/// 1. When the scene starts, the controller either creates a NetworkGameRoom if hosting or joins an existing room if 
/// not hosting.
/// 2. When another player joins the room and the local player is hosting, the controller sends over an Area
/// Description to localize to.
/// 3. While connected and localized, the gameplay of adding and removing cubes to the world using RPCs to synchronize
/// over the network.
/// </summary>
public class MultiplayerCubeStackerUIController : Photon.PunBehaviour, ITangoAreaDescriptionEvent, ITangoLifecycle
{
    /// <summary>
    /// The list of cube prefabs.
    /// 
    /// These are the cubes that are going to be added to the scene.
    /// </summary>
    public GameObject[] m_cubePrefab;

    /// <summary>
    /// Reference to a DeltaPoseController with a PhotonView so the poses get synced over the network.
    ///
    /// The sole purpose for this field is to get the name of the prefab, so that Photon can instantiate it over
    /// network.
    /// </summary>
    public GameObject m_networkDeltaPoseControllerPrefab;

    /// <summary>
    /// UI background before local player is initialized.
    /// </summary>
    public GameObject m_uiBackgroundOverlay;

    /// <summary>
    /// Progress text UI for downloading an Area Description.
    /// </summary>
    public GameObject m_progressPanel;

    /// <summary>
    /// Maximum size of the world.
    /// </summary>
    private const int WORLD_SIZE = 100;
    
    /// <summary>
    /// Maximum raycast distance when adding or removing cubes.
    /// </summary>
    private const float RAYCAST_MAX_DISTANCE = 10.0f;

    /// <summary>
    /// The temp file location, used for storing downloaded Area Description and exported Area Description.
    /// </summary>
    private const string TEMP_FILE_PATH = "/sdcard/tango_multiplayer_example/temp/";

    /// <summary>
    /// The local player's Tango Delta Controller.
    ///
    /// Used for raycasting. This object is instantiated by PhotonNetwork.
    /// </summary>
    private GameObject m_localPlayer;

    /// <summary>
    /// Dictionary of currently placed cubes.
    ///
    /// The key is the position of the cube, the value is the cube GameObject.
    /// </summary>
    private Dictionary<Vector3, GameObject> m_cubeList = new Dictionary<Vector3, GameObject>();

    /// <summary>
    /// Size of the cube objects.
    /// </summary>
    private float m_cubeSize;

    /// <summary>
    /// Currently selected cube type to place.
    /// </summary>
    private int m_currentCubeIndex = 0;

    /// <summary>
    /// A reference to TangoApplication object.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// The Area Description localized to. When hosting, this is the Area Description picked in the UI. When joining,
    /// this is the Area Description sent over the network.
    /// </summary>
    private AreaDescription m_loadedAreaDescription;

    /// <summary>
    /// RPCFileSender object for sending and receiving Area Descriptions over the network.
    /// </summary>
    private RPCFileSender m_fileSender;

    /// <summary>
    /// Unity Start function.
    ///
    /// This function is responsible for initialization, including setting up m_fileSender and joining or creating a
    /// Photon room.
    /// </summary>
    public void Start()
    {
        m_cubeSize = m_cubePrefab[0].transform.lossyScale.x;

        m_progressPanel.SetActive(false);
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        if (m_tangoApplication == null)
        {
            _QuitGame();
        }

        m_tangoApplication.Register(this);
        m_tangoApplication.RequestPermissions();

        m_fileSender = GetComponent<RPCFileSender>();
        m_fileSender.OnPackageReceived += _OnAreaDescriptionTransferReceived;
        m_fileSender.OnPackageTransferFinished += _OnAreaDescriptionTransferFinished;
        m_fileSender.OnPackageTransferStarted += _OnAreaDescriptionTransferStarted;
        m_fileSender.OnPackageTransferError += _OnAreaDescriptionTransferError;

        if (!PhotonNetwork.insideLobby)
        {
            AndroidHelper.ShowAndroidToastMessage("Please wait to join the room until you are in lobby.");
            return;
        }
        
        if (Globals.m_curAreaDescription == null)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.CreateRoom("Random Room");
        }
    }

    /// <summary>
    /// Unity Update function.
    /// 
    /// Used for getting mouse input for desktop debugging.
    /// </summary>
    public void Update()
    {
#if UNITY_EDITOR
        if(Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddCube();
        }

        if(Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            RemoveCube();
        }
#endif
        if (Input.GetKey(KeyCode.Escape))
        {
            _QuitGame();
        }
    }

    /// <summary>
    /// This is called when the Area Description export operation completes.
    /// </summary>
    /// <param name="isSuccessful">If the export operation completed successfully.</param>
    public void OnAreaDescriptionExported(bool isSuccessful)
    {
        if (!isSuccessful)
        {
            AndroidHelper.ShowAndroidToastMessage("Area Description export failed," +
                    "other players will be unable to join game.");
            _QuitGame();
            return;
        }

        m_tangoApplication.Startup(Globals.m_curAreaDescription);
        _StartPlayer();
    }

    /// <summary>
    /// This is called when the Area Description import operation completes.
    /// 
    /// Please note that the Tango Service can only load Area Description file from internal storage.
    /// </summary>
    /// <param name="isSuccessful">If the import operation completed successfully.</param>
    /// <param name="areaDescription">The imported Area Description.</param>
    public void OnAreaDescriptionImported(bool isSuccessful, AreaDescription areaDescription)
    {
        if (!isSuccessful)
        {
            AndroidHelper.ShowAndroidToastMessage("Area Description import failed, unable to join game.");
            _QuitGame();
            return;
        }
        
        // Only non-master client will run this part of code.
        m_loadedAreaDescription = areaDescription;
        m_tangoApplication.Startup(m_loadedAreaDescription);
        _StartPlayer();
    }

    /// <summary>
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
        if (!permissionsGranted)
        {
            AndroidHelper.ShowAndroidToastMessage("Tango permission needed");
            _QuitGame();
            return;
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
    /// 
    /// We do clean up the Area Description File that was imported from download.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
#if !UNITY_EDITOR
        if (Globals.m_curAreaDescription == null && m_loadedAreaDescription != null)
        {
            // This indicates that we joined a room and imported a valid temporary Area Description.
            m_loadedAreaDescription.Delete();
        }

        try 
        {
            Directory.Delete(TEMP_FILE_PATH, true);
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.Log("Temp file directory does not exsit.");
        }
#endif
    }

    /// <summary>
    /// Called when entering a room (by creating or joining it). Called on all clients (including the Master Client).
    /// </summary>
    /// <remarks>This method is commonly used to instantiate player characters.
    /// If a match has to be started "actively", you can call an [PunRPC](@ref PhotonView.RPC) triggered by a user's
    /// button-press or a timer.
    /// 
    /// When this is called, you can usually already access the existing players in the room via 
    /// PhotonNetwork.playerList. Also, all custom properties should be already available as Room.customProperties.
    /// Check Room.playerCount to find out if enough players are in the room to start playing.</remarks>
    public override void OnJoinedRoom()
    {
#if UNITY_EDITOR
        m_tangoApplication.Startup(null);
        _StartPlayer();
#else
        if (Globals.m_curAreaDescription != null)
        {
            Directory.CreateDirectory(TEMP_FILE_PATH);
            Globals.m_curAreaDescription.ExportToFile(TEMP_FILE_PATH);
        }
#endif
        m_loadedAreaDescription = Globals.m_curAreaDescription;
    }

    /// <summary>
    /// Called when a CreateRoom() call failed. The parameter provides ErrorCode and message (as array).
    /// </summary>
    /// <remarks>
    /// Most likely because the room name is already in use (some other client was faster than you).
    /// PUN logs some info if the PhotonNetwork.logLevel is >= PhotonLogLevel.Informational.
    /// </remarks>
    /// <param name="codeAndMsg">CodeAndMsg[0] is a short ErrorCode and codeAndMsg[1] is a string debug msg.</param>
    public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        AndroidHelper.ShowAndroidToastMessage("Create room failed");
        Debug.Log("Create room failed" + Environment.StackTrace);
        _QuitGame();
    }

    /// <summary>
    /// Called when a JoinRoom() call failed. The parameter provides ErrorCode and message (as array).
    /// </summary>
    /// <remarks>
    /// Most likely error is that the room does not exist or the room is full (some other client was faster than you).
    /// PUN logs some info if the PhotonNetwork.logLevel is >= PhotonLogLevel.Informational.
    /// </remarks>
    /// <param name="codeAndMsg">CodeAndMsg[0] is short ErrorCode. codeAndMsg[1] is string debug msg.</param>
    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        AndroidHelper.ShowAndroidToastMessage("Join room failed");
        Debug.Log("Join room failed" + Environment.StackTrace);
        _QuitGame();
    }

    /// <summary>
    /// Called after switching to a new MasterClient when the current one leaves.
    /// </summary>
    /// <remarks>
    /// This is not called when this client enters a room.
    /// The former MasterClient is still in the player list when this method get called.
    /// </remarks>
    /// <param name="newMasterClient">New master client.</param>
    public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        // In the case of master client leave the room, we lost host that sharing Area Description. So, all client
        // should quit the room too.
        _QuitGame();
    }

    /// <summary>
    /// Photon networking callback. Called when a new player is joined the room.
    /// 
    /// We use this callback to notify master client to send over the Area Description File.
    /// </summary>
    /// <param name="newPlayer">New player that joined the room.</param>
    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogError("newPlayer is null\n" + Environment.StackTrace);
            return;
        }

        byte[] dataArr;
#if UNITY_EDITOR
        // 1mb data for testing.
        dataArr = new byte[1000000];
        dataArr[0] = 0;
        dataArr[1] = 1;
        dataArr[2] = 2;
        dataArr[3] = 3;
#else
        string path = TEMP_FILE_PATH + Globals.m_curAreaDescription.m_uuid;
        dataArr = File.ReadAllBytes(path);
#endif
        // Send out the Area Description File.
        m_fileSender.SendPackage(newPlayer, dataArr);

        foreach (KeyValuePair<Vector3, GameObject> entry in m_cubeList)
        {
            Vector3 key = entry.Key;
            GameObject value = entry.Value;
            GetComponent<PhotonView>().RPC("_AddCubeAt",
                                           PhotonTargets.AllViaServer,
                                           value.transform.position, 
                                           value.GetComponent<CubeType>().m_cubeType,
                                           key);
        }
    }

    /// <summary>
    /// Called after disconnecting from the Photon server.
    /// </summary>
    /// <remarks>In some cases, other callbacks are called before OnDisconnectedFromPhoton is called.
    /// Examples: OnConnectionFail() and OnFailedToConnectToPhoton().</remarks>
    public override void OnDisconnectedFromPhoton()
    {
        _QuitGame();
    }

    /// <summary>
    /// Called if a connect call to the Photon server failed before the connection was established, followed by a call
    /// to OnDisconnectedFromPhoton().
    /// </summary>
    /// <remarks>This is called when no connection could be established at all.
    /// It differs from OnConnectionFail, which is called when an existing connection fails.</remarks>
    /// <param name="cause">Cause of disconnect.</param>
    public override void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        _QuitGame();
    }

    /// <summary>
    /// Change current cube type.
    /// </summary>
    /// <param name="index">The cube's index in the prefab list.</param>
    public void SetCubeIndex(int index)
    {
        m_currentCubeIndex = index;
    }
    
    /// <summary>
    /// Add a cube to the scene.
    /// </summary>
    public void AddCube()
    {
        if (m_localPlayer == null)
        {
            Debug.LogError("m_localPlayer is not available." + Environment.StackTrace);
            return;
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(m_localPlayer.transform.position,
                            m_localPlayer.transform.forward,
                            out hitInfo, RAYCAST_MAX_DISTANCE))
        {
            Vector3 center = (hitInfo.point / m_cubeSize) + (hitInfo.normal * m_cubeSize);
            float x = Mathf.Floor(center.x + m_cubeSize);
            float y = Mathf.Floor(center.y + m_cubeSize);
            float z = Mathf.Floor(center.z + m_cubeSize);
            center.x = x;
            center.y = y;
            center.z = z;
            
            int xIndex = (int)x + (WORLD_SIZE / 2);
            int yIndex = (int)y + (WORLD_SIZE / 2);
            int zIndex = (int)z + (WORLD_SIZE / 2);
            if (xIndex >= WORLD_SIZE || yIndex >= WORLD_SIZE || zIndex >= WORLD_SIZE ||
                xIndex < 0 || yIndex < 0 || zIndex < 0)
            {
                Debug.Log("Index out of bound\n" + Environment.StackTrace);
            }

            Vector3 p = (center * m_cubeSize) - new Vector3(0.0f, m_cubeSize / 2.0f, 0.0f);
            
            GetComponent<PhotonView>().RPC("_AddCubeAt",
                                           PhotonTargets.AllViaServer,
                                           p, m_currentCubeIndex, new Vector3(xIndex, yIndex, zIndex));
        }
    }
    
    /// <summary>
    /// Remove a cube from the scene.
    /// </summary>
    public void RemoveCube()
    {
        if (m_localPlayer == null)
        {
            Debug.LogError("m_localPlayer is not available." + Environment.StackTrace);
            return;
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(m_localPlayer.transform.position,
                            m_localPlayer.transform.forward,
                            out hitInfo, RAYCAST_MAX_DISTANCE))
        {
            Vector3 p = (hitInfo.collider.gameObject.transform.position + new Vector3(0.0f, m_cubeSize / 2.0f, 0.0f)) /
                m_cubeSize;
            int xIndex = (int)p.x + (WORLD_SIZE / 2);
            int yIndex = (int)p.y + (WORLD_SIZE / 2);
            int zIndex = (int)p.z + (WORLD_SIZE / 2);
            
            if (xIndex >= WORLD_SIZE || yIndex >= WORLD_SIZE || zIndex >= WORLD_SIZE ||
                xIndex < 0 || yIndex < 0 || zIndex < 0)
            {
                Debug.Log("Index out of bound\n" + Environment.StackTrace);
            }

            GetComponent<PhotonView>().RPC("_RemoveCubeAt",
                                           PhotonTargets.AllViaServer, new Vector3(xIndex, yIndex, zIndex));
        }
    }

    /// <summary>
    /// Callback from FileSender. Called when the m_fileSender started transfer Area Description File.
    /// </summary>
    /// <param name="size">Total size of the buffer that is going to be transferred.</param>
    private void _OnAreaDescriptionTransferStarted(int size)
    {
        m_progressPanel.SetActive(true);
    }

    /// <summary>
    /// Callback from FileSender. Called when the m_fileSender received a package.
    /// </summary>
    /// <param name="finishedPercentage">The percentage of packages have been transferred.</param>
    private void _OnAreaDescriptionTransferReceived(float finishedPercentage)
    {
        m_progressPanel.GetComponentInChildren<Text>().text =
            string.Format("Receiving Area Description:\n{0}% completed", (int)(finishedPercentage * 100));
    }

    /// <summary>
    /// Callback from FileSender. Called when the m_fileSender finished transfering the full package.
    /// </summary>
    /// <param name="fullAreaDescription">The full buffer that has been transferred.</param>
    private void _OnAreaDescriptionTransferFinished(byte[] fullAreaDescription)
    {
        m_progressPanel.SetActive(false);
#if !UNITY_EDITOR
        if (fullAreaDescription[0] == 0 && fullAreaDescription[1] == 1 &&
            fullAreaDescription[2] == 2 && fullAreaDescription[3] == 3)
        {
            // If first 4 values of full Area Description is 0, we consider the file sender is a debugging host.
            // In that case, we will start play in motion tracking mode.
            m_tangoApplication.GetComponent<RelocalizingOverlay>().m_relocalizationOverlay.SetActive(false);
            m_tangoApplication.m_enableAreaDescriptions = false;
            m_tangoApplication.Startup(null);
            _StartPlayer();
            m_localPlayer.GetComponent<TangoDeltaPoseController>().m_useAreaDescriptionPose = false;
        }
        else
        {
            Directory.CreateDirectory(TEMP_FILE_PATH);
            string path = TEMP_FILE_PATH + "received_area_description";
            File.WriteAllBytes(path, fullAreaDescription);
            AreaDescription.ImportFromFile(path);
        }
#endif
    }

    /// <summary>
    /// Callback from FileSender. Called when encountered error during transfer.
    /// </summary>
    private void _OnAreaDescriptionTransferError()
    {
        _QuitGame();
    }

    /// <summary>
    /// Photon RPC call notifying the local client to add a cube at a specific position.
    /// </summary>
    /// <param name="cubePosition">The position of cube.</param>
    /// <param name="type">The type of cube.</param>
    /// <param name="key">Key hash key of the cube object.</param>
    [PunRPC]
    private void _AddCubeAt(Vector3 cubePosition, int type, Vector3 key)
    {
        if (m_cubeList.ContainsKey(key))
        {
            Debug.Log("Cube index exsited");
            return;
        }

        GameObject obj = Instantiate(m_cubePrefab[type], cubePosition, Quaternion.identity) as GameObject;
        m_cubeList.Add(key, obj);
    }

    /// <summary>
    /// Photon RPC call notifying the local client to remove a cube at a specific position.
    /// </summary>
    /// <param name="key">Key hash key of the cube object.</param>
    [PunRPC]
    private void _RemoveCubeAt(Vector3 key)
    {
        if (!m_cubeList.ContainsKey(key))
        {
            Debug.LogError("Cube index doesn't exsited");
            return;
        }

        Destroy(m_cubeList[key]);
        m_cubeList.Remove(key);
    }

    /// <summary>
    /// Enable the local player's camera to track the local Tango pose.
    /// </summary>
    private void _StartPlayer()
    {
        m_localPlayer = PhotonNetwork.Instantiate(m_networkDeltaPoseControllerPrefab.name,
                                                 Vector3.zero, Quaternion.identity, 0);

        m_uiBackgroundOverlay.SetActive(false);
        m_localPlayer.GetComponent<TangoDeltaPoseController>().enabled = true;
        m_localPlayer.GetComponentInChildren<Camera>().enabled = true;
    }

    /// <summary>
    /// Quit the room properly.
    /// </summary>
    private void _QuitGame()
    {
        if (PhotonNetwork.inRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        m_tangoApplication.Shutdown();
        Application.LoadLevel("AreaDescriptionPicker");
    }
}
