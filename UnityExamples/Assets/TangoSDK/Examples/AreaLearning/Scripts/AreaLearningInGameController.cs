//-----------------------------------------------------------------------
// <copyright file="AreaLearningInGameController.cs" company="Google">
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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Tango;

/// <summary>
/// AreaLearningGUIController is responsible for the main game interaction.
/// 
/// This class also takes care of loading / save persistent data(marker), and loop closure handling.
/// </summary>
public class AreaLearningInGameController : MonoBehaviour, ITangoPose, ITangoEvent
{
    /// <summary>
    /// Data container for marker.
    /// 
    /// Used for serializing/deserializing marker to xml.
    /// </summary>
    [System.Serializable]
    public class MarkerData
    {
        /// <summary>
        /// Marker's type.
        /// 
        /// Red, green or blue markers. In a real game senario, this could be different game objects
        /// (e.g. banana, apple, watermelon, persimmons).
        /// </summary>
        [XmlElement("type")]
        public int m_type;
        
        /// <summary>
        /// Position of the this mark, with respect to the origin of the game world.
        /// </summary>
        [XmlElement("position")]
        public Vector3 m_position;
        
        /// <summary>
        /// Rotation of the this mark.
        /// </summary>
        [XmlElement("orientation")]
        public Quaternion m_orientation;
    }

    /// <summary>
    /// Prefabs of different colored markers.
    /// </summary>
    public GameObject[] m_markPrefabs;

    /// <summary>
    /// The Area Description currently loaded in the Tango Service.
    /// </summary>
    public AreaDescription m_curAreaDescription;

    /// <summary>
    /// Saving progress UI text.
    /// </summary>
    public UnityEngine.UI.Text m_savingText;

    /// <summary>
    /// A reference to DeltaPostController instance.
    /// 
    /// In this class, we need DeltaPostController reference to get the timestamp and pose when we place a marker.
    /// The timestamp and pose is used for later loop closure position correction. 
    /// </summary>
    private TangoDeltaPoseController m_deltaPoseController;

    /// <summary>
    /// List of markers placed in the scene.
    /// </summary>
    private List<GameObject> m_markerList = new List<GameObject>();

    /// <summary>
    /// Reference to the newly placed marker.
    /// </summary>
    private GameObject newMarkObject = null;

    /// <summary>
    /// Current marker type.
    /// </summary>
    private int m_currentMarkType = 0;

    /// <summary>
    /// If set, this is the selected marker.
    /// </summary>
    private ARMarker m_selectedMarker;

    /// <summary>
    /// If set, this is the rectangle bounding the selected marker.
    /// </summary>
    private Rect m_selectedRect;

    /// <summary>
    /// If the interaction is initialized.
    /// 
    /// Note that the initilization is triggered by the relocalization event. We don't want user to place object before
    /// the device is relocalized.
    /// </summary>
    private bool m_initialized = false;

    /// <summary>
    /// A reference to TangoApplication instance.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Matrix that transforms from the Unity Camera to the Unity World.
    /// 
    /// Needed to calculate offsets.
    /// </summary>
    private Matrix4x4 m_uwTss;

    /// <summary>
    /// Matrix that transforms from the Unity Camera to Device.
    /// </summary>
    private Matrix4x4 m_dTuc;

    private Thread m_saveThread;

    /// <summary>
    /// Unity Start function.
    /// 
    /// We find and assign pose controller and tango applicaiton, and register this class to callback events.
    /// </summary>
    public void Start()
    {
        // Constant matrix converting start of service frame to Unity world frame.
        m_uwTss = new Matrix4x4();
        m_uwTss.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        m_uwTss.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        // Constant matrix converting Unity world frame frame to device frame.
        m_dTuc = new Matrix4x4();
        m_dTuc.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_dTuc.SetColumn(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_dTuc.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
        m_dTuc.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        m_deltaPoseController = FindObjectOfType<TangoDeltaPoseController>();
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);
        }
    }

    /// <summary>
    /// Unity Update function.
    /// 
    /// Mainly handle the touch event and place mark in place.
    /// </summary>
    public void Update()
    {
        if (m_saveThread != null && m_saveThread.ThreadState != ThreadState.Running)
        {
            // After saving an Area Description or mark data, we reload the scene to restart the game.
            Application.LoadLevel(Application.loadedLevel);
        }

        if (!m_initialized)
        {
            return;
        }
        if (EventSystem.current.IsPointerOverGameObject(0) || GUIUtility.hotControl != 0)
        {
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            Vector2 guiPosition = new Vector2(t.position.x, Screen.height - t.position.y);
            Camera cam = Camera.main;
            RaycastHit hitInfo;

            if (m_selectedRect.Contains(guiPosition))
            {
                // do nothing, the button will handle it.
            }
            else if (Physics.Raycast(cam.ScreenPointToRay(t.position), out hitInfo))
            {
                if (t.phase == TouchPhase.Began)
                {
                    GameObject tapped = hitInfo.collider.gameObject;
                    if (tapped.tag != "Ground")
                    {
                        if (!tapped.GetComponent<Animation>().isPlaying)
                        {
                            m_selectedMarker = tapped.GetComponent<ARMarker>();
                        }
                    }
                    else
                    {
                        // Instantiate marker object.
                        newMarkObject = Instantiate(m_markPrefabs[m_currentMarkType],
                                                    hitInfo.point,
                                                    Quaternion.identity) as GameObject;
                        ARMarker markerScript = newMarkObject.GetComponent<ARMarker>();

                        markerScript.m_type = m_currentMarkType;
                        markerScript.m_timestamp = m_deltaPoseController.m_poseTimestamp;

                        Matrix4x4 uwTDevice = Matrix4x4.TRS(m_deltaPoseController.m_tangoPosition,
                                                            m_deltaPoseController.m_tangoRotation,
                                                            Vector3.one);
                        Matrix4x4 uwTMarker = Matrix4x4.TRS(hitInfo.point,
                                                            Quaternion.identity,
                                                            Vector3.one);
                        markerScript.m_deviceTMarker = Matrix4x4.Inverse(uwTDevice) * uwTMarker;

                        m_markerList.Add(newMarkObject);
                    }
                }
                else if (t.phase == TouchPhase.Moved)
                {
                    // Move instantiate object
                    if (newMarkObject != null)
                    {
                        newMarkObject.transform.position = hitInfo.point;
                    }
                }
                else if (t.phase == TouchPhase.Ended)
                {
                    newMarkObject = null;
                }
            }
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Unity OnGUI function.
    /// 
    /// Mainly for removing markers.
    /// </summary>
    public void OnGUI()
    {
        if (m_selectedMarker != null)
        {
            Renderer selectedRenderer = m_selectedMarker.GetComponent<Renderer>();
            
            // GUI's Y is flipped from the mouse's Y
            Rect screenRect = _WorldBoundsToScreen(Camera.main, selectedRenderer.bounds);
            float yMin = Screen.height - screenRect.yMin;
            float yMax = Screen.height - screenRect.yMax;
            screenRect.yMin = Mathf.Min(yMin, yMax);
            screenRect.yMax = Mathf.Max(yMin, yMax);
            
            if (GUI.Button(screenRect, "<size=30>Hide</size>"))
            {
                m_markerList.Remove(m_selectedMarker.gameObject);
                m_selectedMarker.SendMessage("Hide");
                m_selectedMarker = null;
                m_selectedRect = new Rect();
            }
            else
            {
                m_selectedRect = screenRect;
            }
        }
        else
        {
            m_selectedRect = new Rect();
        }
    }

    /// <summary>
    /// Set the marker type.
    /// </summary>
    /// <param name="type">Marker type.</param>
    public void SetCurrentMarkType(int type)
    {
        if (type != m_currentMarkType)
        {
            m_currentMarkType = type;
        }
    }

    /// <summary>
    /// Save the game.
    /// 
    /// Save will trigger 3 things:
    /// 1. Bundle adjustment for all marks position, please see _UpdateMarkersForLoopClosures() function header for 
    ///     more details.
    /// 2. Save all marker to xml, save the Area Description if the learning mode is on.
    /// 3. Reload the scene.
    /// </summary>
    public void Save()
    {
        // We do a bundle adjustment to all the markers first, to make sure it's located at the most precise position.
        if (m_tangoApplication.m_enableAreaLearning)
        {
            _UpdateMarkersForLoopClosures();
        }

        // Compose a XML data list
        List<MarkerData> xmlDataList = new List<MarkerData>();
        foreach (GameObject obj in m_markerList)
        {
            // Add marks data to the list, we intentionally didn't add the timestamp, because the timestamp will not be
            // useful when the next time Tango Service is connected. The timestamp is only used for loop closure pose
            // correction in current Tango connection.
            MarkerData temp = new MarkerData();
            temp.m_type = obj.GetComponent<ARMarker>().m_type;
            temp.m_position = obj.transform.position;
            temp.m_orientation = obj.transform.rotation;
            xmlDataList.Add(temp);
        }

        // Disable interaction before saving.
        m_initialized = false;
        m_savingText.gameObject.SetActive(true);

        // Start saving process in another thread.
        ThreadStart work = delegate { _AsyncDataSaving(xmlDataList); };
        m_saveThread = new Thread(work);
        m_saveThread.Start();
    }

    /// <summary>
    /// This is called each time a Tango event happens.
    /// </summary>
    /// <param name="tangoEvent">Tango event.</param>
    public void OnTangoEventAvailableEventHandler(Tango.TangoEvent tangoEvent)
    {
        // We will not have the saving progress when the learning mode is off.
        if (!m_tangoApplication.m_enableAreaLearning)
        {
            return;
        }

        if (tangoEvent.type == TangoEnums.TangoEventType.TANGO_EVENT_AREA_LEARNING
            && tangoEvent.event_key == "AreaDescriptionSaveProgress")
        {
            m_savingText.text = "Saving. " + (float.Parse(tangoEvent.event_value) * 100) + "%";
        }
    }

    /// <summary>
    /// OnTangoPoseAvailable event from Tango.
    /// 
    /// In this function, we only listen to the Start-Of-Service with respect to Area-Description frame pair. This pair
    /// indicates a relocalization or loop closure event happened, base on that, we either start the initialize the
    /// interaction or do a bundle adjustment for all marker position.
    /// </summary>
    /// <param name="poseData">Returned pose data from TangoService.</param>
    public void OnTangoPoseAvailable(Tango.TangoPoseData poseData)
    {
        // This frame pair's callback indicates that a loop closure or relocalization has happened. 
        //
        // When learning mode is on, this callback indicates the loop closure event. Loop closure will happen when the
        // system recognizes a pre-visited area, the loop closure operation will correct the previously saved pose 
        // to achieve more accurate result. (pose can be queried through GetPoseAtTime based on previously saved
        // timestamp).
        // Loop closure definition: https://en.wikipedia.org/wiki/Simultaneous_localization_and_mapping#Loop_closure
        //
        // When learning mode is off, and an Area Description is loaded, this callback indicates a
        // relocalization event. Relocalization is when the device finds out where it is with respect to the loaded
        // Area Description. In our case, when the device is relocalized, the markers will be loaded because we
        // know the relatvie device location to the markers.
        if (poseData.framePair.baseFrame == 
            TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
            poseData.framePair.targetFrame ==
            TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
            poseData.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
        {
            // When we get the first loop closure/ relocalization event, we initialized all the in-game interactions.
            if (!m_initialized)
            {
                m_initialized = true;
                if (m_curAreaDescription == null)
                {
                    Debug.Log("AndroidInGameController.OnTangoPoseAvailable(): m_curAreaDescription is null");
                    return;
                }
                
                // Attempt to load the exsiting markers from storage.
                List<MarkerData> xmlDataList = _ReadFromXml(m_curAreaDescription.m_uuid + ".xml");
                if (xmlDataList == null)
                {
                    Debug.Log("AndroidInGameController.OnTangoPoseAvailable(): xmlDataList is null");
                    return;
                }
                
                m_markerList.Clear();
                foreach (MarkerData mark in xmlDataList)
                {
                    // Instantiate all markers' gameobject.
                    GameObject temp = Instantiate(m_markPrefabs[mark.m_type],
                                                  mark.m_position,
                                                  mark.m_orientation) as GameObject;
                    m_markerList.Add(temp);
                }
            }
            else
            {
                Debug.Log("AndroidInGameController.OnTangoPoseAvailable(): relocalized");
                if (m_tangoApplication.m_enableAreaLearning)
                {
                    _UpdateMarkersForLoopClosures();
                }
            }
        }
    }

    /// <summary>
    /// Thread method to save an Area Description and marker data.  Make this the ThreadFunc.
    /// </summary>
    /// <param name="xmlDataList">The marker's data list.</param>
    private void _AsyncDataSaving(List<MarkerData> xmlDataList)
    {
        if (xmlDataList == null)
        {
            Debug.Log("AndroidInGameController._AsyncDataSaving(): xmlDataList is null");
        }

        if (m_tangoApplication.m_enableAreaLearning)
        {
            m_curAreaDescription = AreaDescription.SaveCurrent();
        }
        _WriteToXml(m_curAreaDescription.m_uuid + ".xml", xmlDataList);
    }

    /// <summary>
    /// Correct all saved marks when loop closure happens.
    /// 
    /// When Tango Service is in learning mode, the drift will accumulate overtime, but when the system sees a
    /// pre-exsiting area, it will do a operation to correct all previously saved poses
    /// (the pose you can query with GetPoseAtTime). This operation is called loop closure. When loop closure happens,
    /// we will need to re-query all previously saved marker position in order to achieve the best result.
    /// This function is doing the querying job based on timestamp.
    /// </summary>
    private void _UpdateMarkersForLoopClosures()
    {
        // Adjust mark's position each time we have a loop closure detected.
        foreach (GameObject obj in m_markerList)
        {
            ARMarker tempMarker = obj.GetComponent<ARMarker>();
            if (tempMarker.m_timestamp != -1.0f)
            {
                TangoCoordinateFramePair pair;
                TangoPoseData relocalizedPose = new TangoPoseData();

                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime(relocalizedPose, tempMarker.m_timestamp, pair);
                Vector3 pDevice = new Vector3((float)relocalizedPose.translation[0],
                                              (float)relocalizedPose.translation[1],
                                              (float)relocalizedPose.translation[2]);
                Quaternion qDevice = new Quaternion((float)relocalizedPose.orientation[0],
                                                    (float)relocalizedPose.orientation[1],
                                                    (float)relocalizedPose.orientation[2],
                                                    (float)relocalizedPose.orientation[3]);

                Matrix4x4 uwTDevice = m_uwTss * Matrix4x4.TRS(pDevice, qDevice, Vector3.one) * m_dTuc;
                Matrix4x4 uwTMarker = uwTDevice * tempMarker.m_deviceTMarker;

                obj.transform.position = uwTMarker.GetColumn(3);
                obj.transform.rotation = Quaternion.LookRotation(uwTMarker.GetColumn(2), uwTMarker.GetColumn(1));
            }
        }
    }

    /// <summary>
    /// Write marker list to an xml file stored in application storage.
    /// </summary>
    /// <param name="fileName">The xml's filename, corresponding to the Area Description's UUID.</param>
    /// <param name="obj">List of mark data.</param>
    private void _WriteToXml(string fileName, List<MarkerData> obj)
    {
        string path = Application.persistentDataPath + fileName;
        var serializer = new XmlSerializer(typeof(List<MarkerData>));
        using (var stream = new FileStream(path, FileMode.Create))
        {
            serializer.Serialize(stream, obj);
        }
    }

    /// <summary>
    /// Load marker list xml from application storage.
    /// </summary>
    /// <returns>List of marker data.</returns>
    /// <param name="fileName">The xml's filename, corresponding to the Area Description's UUID.</param>
    private List<MarkerData> _ReadFromXml(string fileName)
    {
        string path = Application.persistentDataPath + fileName;
        var serializer = new XmlSerializer(typeof(List<MarkerData>));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            return serializer.Deserialize(stream) as List<MarkerData>;
        }
    }

    /// <summary>
    /// Convert a 3D bounding box into a 2D Rect.
    /// </summary>
    /// <returns>The 2D Rect in Screen coordinates.</returns>
    /// <param name="cam">Camera to use.</param>
    /// <param name="bounds">3D bounding box.</param>
    private Rect _WorldBoundsToScreen(Camera cam, Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Bounds screenBounds = new Bounds(cam.WorldToScreenPoint(center), Vector3.zero);
        
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, +extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, +extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, -extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, -extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, +extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, +extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, -extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, -extents.y, -extents.z)));
        return Rect.MinMaxRect(screenBounds.min.x, screenBounds.min.y, screenBounds.max.x, screenBounds.max.y);
    }
}
