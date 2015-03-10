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
using Tango;
using System;

/// <summary>
/// This is a basic movement controller based on
/// pose estimation returned from the Tango Service.
/// </summary>
public class SampleController : PoseListener
{
	public enum TrackingTypes
	{
		NONE,
		MOTION,
		ADF,
		RELOCALIZED
	}
	
	public float m_movementScale = 1.0f;
	public bool m_useADF = false;
	
	// These matrix is used for compute extrinsics corrected pose transformation.
	// The corrected pose will take in the extrinsics between different sensor,
	// and compute the percise estimated pose from it.
	// These technics are useful when the percise pose is need, for example AR or
	// mesh reconstruction.
	// See the point cloud example to see how to use them, in this script, we just
	// queried the value from API.
	[HideInInspector]
	public Matrix4x4 m_deviceToIMUMatrix = new Matrix4x4 ();
	[HideInInspector]
	public Matrix4x4 m_cameraToIMUMatrix = new Matrix4x4 ();
	
	private bool m_alreadyInitialized = false;
	private TangoApplication m_tangoApplication;
	private Vector3 m_startingOffset;
	private Quaternion m_startingRotation;
	private const int DEVICE_TO_START = 0;
	private const int DEVICE_TO_ADF = 1;
	private const int START_TO_ADF = 2;
	
	// Tango pose data for debug logging and transform update.
	// Index 0: device with respect to start frame.
	// Index 1: device with respect to adf frame.
	// Index 2: start with respect to adf frame.
	private float[] m_frameDeltaTime;
	private float[] m_prevFrameTimestamp;
	private int[] m_frameCount;
	private TangoEnums.TangoPoseStatusType[] m_status;
	private Quaternion[] m_tangoRotation;
	private Vector3[] m_tangoPosition;
	private bool m_isRelocalized = false;
	private bool m_isDirty = false;
	private string m_tangoServiceVersionName = string.Empty;
	
	private bool m_shouldInitTango = false;
	
	/// <summary>
	/// Determines whether the motion tracking is localized.
	/// </summary>
	/// <returns><c>true</c> if the motion tracking is localized; otherwise, <c>false</c>.</returns>
	public bool IsLocalized()
	{
		return m_isRelocalized;
	}
	
	/// <summary>
	/// Initialize the controller.
	/// </summary>
	private void Awake()
	{
		m_isDirty = false;
		m_startingOffset = transform.position;
		m_startingRotation = transform.rotation;
		m_frameDeltaTime = new float[]{-1.0f,-1.0f,-1.0f};
		m_prevFrameTimestamp = new float[]{-1.0f,-1.0f,-1.0f};
		m_frameCount = new int[]{-1,-1,-1};
		m_status = new TangoEnums.TangoPoseStatusType[]{TangoEnums.TangoPoseStatusType.NA,
			TangoEnums.TangoPoseStatusType.NA, TangoEnums.TangoPoseStatusType.NA};
		m_tangoRotation = new Quaternion[]{Quaternion.identity,
			Quaternion.identity, Quaternion.identity};
		m_tangoPosition = new Vector3[]{Vector3.zero,Vector3.zero,Vector3.zero};
	}

	/// <summary>
	/// Start this instance.
	/// </summary>
	private void Start()
	{
		m_tangoApplication = FindObjectOfType<TangoApplication>();
		
		if(m_tangoApplication != null)
		{
			if(AndroidHelper.IsTangoCorePresent())
			{
				// Request Tango permissions
				m_tangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
				m_tangoApplication.RequestNecessaryPermissionsAndConnect();
				m_tangoServiceVersionName = TangoApplication.GetTangoServiceVersion();
			}
			else
			{
				// If no Tango Core is present let's tell the user to install it!
				StartCoroutine(_InformUserNoTangoCore());
			}
		}
		else
		{
			Debug.Log("No Tango Manager found in scene.");
		}
	}
	
	/// <summary>
	/// Informs the user that they should install Tango Core via Android toast.
	/// </summary>
	private IEnumerator _InformUserNoTangoCore()
	{
		AndroidHelper.ShowAndroidToastMessage("Please install Tango Core", false);
		yield return new WaitForSeconds(2.0f);
		Application.Quit();
	}
	
	/// <summary>
	/// Apply any needed changes to the pose.
	/// </summary>
	private void Update()
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(m_shouldInitTango)
		{
			m_tangoApplication.InitApplication();
			
			if(m_useADF)
			{
				// Query the full adf list.
				PoseProvider.RefreshADFList();
				// loading last recorded ADF
				string uuid = PoseProvider.GetLatestADFUUID().GetStringDataUUID();
				m_tangoApplication.InitProviders(uuid);
			}
			else
			{
				m_tangoApplication.InitProviders(string.Empty);
			}
			
			// Query extrinsics constant tranformations.
			TangoPoseData poseData = new TangoPoseData();
			double timestamp = 0.0;
			TangoCoordinateFramePair pair;
			
			pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
			pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
			PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
			Vector3 position = new Vector3((float)poseData.translation[0], (float)poseData.translation[1], (float)poseData.translation[2]);
			Quaternion quat = new Quaternion((float)poseData.orientation[0], (float)poseData.orientation[1], (float)poseData.orientation[2], (float)poseData.orientation[3]);
			m_deviceToIMUMatrix = Matrix4x4.TRS(position, quat, new Vector3 (1.0f, 1.0f, 1.0f));
			
			pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
			pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
			PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
			position = new Vector3((float)poseData.translation[0], (float)poseData.translation[1], (float)poseData.translation[2]);
			quat = new Quaternion((float)poseData.orientation[0], (float)poseData.orientation[1], (float)poseData.orientation[2], (float)poseData.orientation[3]);
			m_cameraToIMUMatrix = Matrix4x4.TRS(position, quat, new Vector3 (1.0f, 1.0f, 1.0f));
			
			m_alreadyInitialized = true;
			m_shouldInitTango = false;
			
			m_tangoApplication.ConnectToService();
		}

		if (m_isDirty)
		{
			// This rotation needs to be put into Unity coordinate space.
			Quaternion rotationFix = Quaternion.Euler(90.0f, 0.0f, 0.0f);
			
			if (!m_isRelocalized) 
			{
				Quaternion axisFix = Quaternion.Euler(-m_tangoRotation[0].eulerAngles.x,
				                                      -m_tangoRotation[0].eulerAngles.z,
				                                      m_tangoRotation[0].eulerAngles.y);
				
				transform.rotation = m_startingRotation * (rotationFix * axisFix);
				transform.position = (m_startingRotation * (m_tangoPosition[0] * m_movementScale)) + m_startingOffset;
				
			}
			else 
			{
				Quaternion axisFix = Quaternion.Euler(-m_tangoRotation[1].eulerAngles.x,
				                                      -m_tangoRotation[1].eulerAngles.z,
				                                      m_tangoRotation[1].eulerAngles.y);
				
				transform.rotation = m_startingRotation * (rotationFix * axisFix);
				transform.position = (m_startingRotation * (m_tangoPosition[1] * m_movementScale)) + m_startingOffset;
			}
			m_isDirty = false;
		}
		
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			if(m_tangoApplication != null)
			{
				m_tangoApplication.Shutdown();
			}
			
			// This is a temporary fix for a lifecycle issue where calling
			// Application.Quit() here, and restarting the application immediately,
			// results in a hard crash.
			AndroidHelper.AndroidQuit();
		}
		
		#else
		Vector3 tempPosition = transform.position;
		Quaternion tempRotation = transform.rotation;
		PoseProvider.GetMouseEmulation(ref tempPosition, ref tempRotation);
		transform.rotation = tempRotation;
		transform.position = tempPosition;
		#endif
	}
	
	/// <summary>
	/// Handle the callback sent by the Tango Service
	/// when a new pose is sampled.
	/// DO NOT USE THE UNITY API FROM INSIDE THIS FUNCTION!
	/// </summary>
	/// <param name="callbackContext">Callback context.</param>
	/// <param name="pose">Pose.</param>
	protected override void _OnPoseAvailable(IntPtr callbackContext, TangoPoseData pose)
	{
		int currentIndex = 0;
		
		// Get out of here if the pose is null
		if (pose == null)
		{
			Debug.Log("TangoPoseDate is null.");
			return;
		}
		
		// The callback pose is for device with respect to start of service pose.
		if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
		    pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
		{
			currentIndex = DEVICE_TO_START;
		}
		// The callback pose is for device with respect to area description file pose.
		else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
		         pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
		{
			currentIndex = DEVICE_TO_ADF;
		} 
		// The callback pose is for start of service with respect to area description file pose.
		else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
		         pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE)
		{
			currentIndex = START_TO_ADF;
		}
		
		// check to see if we are recently relocalized
		if(!m_isRelocalized)
		{
			m_isRelocalized = (currentIndex == 2);
		}
		
		if(pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
		{
			// Cache the position and rotation to be set in the update function.
			// This needs to be done because this callback does not
			// happen in the main game thread.
			m_tangoPosition[currentIndex] = new Vector3((float)pose.translation [0],
			                                            (float)pose.translation [2],
			                                            (float)pose.translation [1]);
			
			m_tangoRotation[currentIndex] = new Quaternion((float)pose.orientation [0],
			                                               (float)pose.orientation [2], // these rotation values are swapped on purpose
			                                               (float)pose.orientation [1],
			                                               (float)pose.orientation [3]);
		}
		else // if the current pose is not valid we set the pose to identity
		{
			m_tangoPosition[currentIndex] = Vector3.zero;
			m_tangoRotation[currentIndex] = Quaternion.identity;
			m_isRelocalized = false;
		}
		
		// Reset the current status frame count if the status code changed.
		if (pose.status_code != m_status[currentIndex])
		{
			m_frameCount[currentIndex] = 0;
		}
		
		// Update the stats for the pose for the debug text
		m_status[currentIndex] = pose.status_code;
		m_frameCount[currentIndex]++;
		
		// Compute delta frame timestamp.
		m_frameDeltaTime[currentIndex] = (float)pose.timestamp - m_prevFrameTimestamp[currentIndex];
		m_prevFrameTimestamp [currentIndex] = (float)pose.timestamp;
		
		// Switch m_isDirty to true, so that the new pose get rendered in update.
		m_isDirty = (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID);
	}
	
	private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
	{
		if(permissionsGranted && !m_alreadyInitialized)
		{
			Debug.Log("SampleController._OnApplicationPermissionsEvent()");
			m_shouldInitTango = true;
		}
		else if (!permissionsGranted)
		{
			AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
		}
		
		if(permissionsGranted && m_alreadyInitialized)
		{
			m_tangoApplication.ConnectToService();
		}
	}
	
	/// <summary>
	/// Construct readable string from TangoPoseStatusType.
	/// </summary>
	private string _GetLoggingStringFromPoseStatus(TangoEnums.TangoPoseStatusType status)
	{
		string statusString = "";
		switch (status)
		{
		case TangoEnums.TangoPoseStatusType.TANGO_POSE_INITIALIZING:
			statusString = "initializing";
			break;
		case TangoEnums.TangoPoseStatusType.TANGO_POSE_INVALID:
			statusString = "invalid";
			break;
		case TangoEnums.TangoPoseStatusType.TANGO_POSE_UNKNOWN:
			statusString = "unknown";
			break;
		case TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID:
			statusString = "valid";
			break;
		default:
			statusString = "N/A";
			break;
		}
		return statusString;
	}
	
	/// <summary>
	/// Reformat string from vector3 type for data logging.
	/// </summary>
	private string _GetLoggingStringFromVec3(Vector3 vec)
	{
		if(vec == Vector3.zero)
		{
			return "N/A";
		}
		else
		{
			return string.Format("{0}, {1}, {2}", 
			                     vec.x.ToString(Common.UI_FLOAT_FORMAT),
			                     vec.y.ToString(Common.UI_FLOAT_FORMAT),
			                     vec.z.ToString(Common.UI_FLOAT_FORMAT));
		}
	}
	
	/// <summary>
	/// Reformat string from quaternion type for data logging.
	/// </summary>
	private string _GetLoggingStringFromQuaternion(Quaternion quat)
	{
		if(quat == Quaternion.identity)
		{
			return "N/A";
		}
		else
		{
			return string.Format("{0}, {1}, {2}, {3}",
			                     quat.x.ToString(Common.UI_FLOAT_FORMAT),
			                     quat.y.ToString(Common.UI_FLOAT_FORMAT),
			                     quat.z.ToString(Common.UI_FLOAT_FORMAT),
			                     quat.w.ToString(Common.UI_FLOAT_FORMAT));
		}
	}
	
	/// <summary>
	/// Return a string to the get logging from frame count.
	/// </summary>
	/// <returns>The get logging string from frame count.</returns>
	/// <param name="frameCount">Frame count.</param>
	private string _GetLoggingStringFromFrameCount(int frameCount)
	{
		if(frameCount == -1.0)
		{
			return "N/A";
		}
		else
		{
			return frameCount.ToString();
		}
	}
	
	/// <summary>
	/// Return a string to get logging of FrameDeltaTime
	/// </summary>
	/// <returns>The get loggin string from frame delta time.</returns>
	/// <param name="frameDeltaTime">Frame delta time.</param>
	private string _GetLogginStringFromFrameDeltaTime(float frameDeltaTime)
	{
		if(frameDeltaTime == -1.0)
		{
			return "N/A";
		}
		else
		{
			return (frameDeltaTime * Common.SECOND_TO_MILLISECOND).ToString(Common.UI_FLOAT_FORMAT);
		}
	}
	
	/// <summary>
	/// Unity callback when application is paused.
	/// </summary>
	void OnApplicationPause(bool pauseStatus) {
		m_isDirty = false;
		m_frameDeltaTime = new float[3];
		m_prevFrameTimestamp = new float[3];
		m_frameCount = new int[3];
		m_status = new TangoEnums.TangoPoseStatusType[3];
		m_tangoRotation = new Quaternion[3];
		m_tangoPosition = new Vector3[3];
	}
	
	/// <summary>
	/// Data logging GUI.
	/// </summary>
	private void OnGUI()
	{
		Color oldColor = GUI.color;
		GUI.color = Color.black;
		
		if(m_tangoApplication.HasRequestedPermissions())
		{
			int guiIndex = 1;
			GUI.Label(new Rect(Common.UI_LABEL_START_X, 
			                   Common.UI_LABEL_START_Y, 
			                   Common.UI_LABEL_SIZE_X , 
			                   Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TANGO_SERVICE_VERSION, m_tangoServiceVersionName) + "</size>");
			
			// MOTION TRACKING
			GUI.Label( new Rect(Common.UI_LABEL_START_X, 
			                    Common.UI_POSE_LABEL_START_Y,
			                    Common.UI_LABEL_SIZE_X , 
			                    Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TARGET_TO_BASE_FRAME,
			                                                             "Device",
			                                                             "Start") + "</size>");
			
			GUI.Label( new Rect(Common.UI_LABEL_START_X, 
			                    Common.UI_POSE_LABEL_START_Y + Common.UI_LABEL_OFFSET * guiIndex,
			                    Common.UI_LABEL_SIZE_X , 
			                    Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_STATUS,
			                                                             _GetLoggingStringFromPoseStatus(m_status[DEVICE_TO_START]),
			                                                             _GetLoggingStringFromFrameCount(m_frameCount[DEVICE_TO_START]),
			                                                             _GetLogginStringFromFrameDeltaTime(m_frameDeltaTime[DEVICE_TO_START]),
			                                                             _GetLoggingStringFromVec3(m_tangoPosition[DEVICE_TO_START]),
			                                                             _GetLoggingStringFromQuaternion(m_tangoRotation[DEVICE_TO_START])) + "</size>");
			
			if(m_useADF || m_tangoApplication.m_enableAreaLearning)
			{
				// ADF
				GUI.Label( new Rect(Common.UI_LABEL_START_X, 
				                    Common.UI_POSE_LABEL_START_Y + Common.UI_LABEL_OFFSET * (++guiIndex),
				                    Common.UI_LABEL_SIZE_X , 
				                    Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TARGET_TO_BASE_FRAME,
				                                                             "Device",
				                                                             "ADF") + "</size>");
				
				GUI.Label( new Rect(Common.UI_LABEL_START_X, 
				                    Common.UI_POSE_LABEL_START_Y + Common.UI_LABEL_OFFSET * (++guiIndex),
				                    Common.UI_LABEL_SIZE_X , 
				                    Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_STATUS,
				                                                             _GetLoggingStringFromPoseStatus(m_status[DEVICE_TO_ADF]),
				                                                             _GetLoggingStringFromFrameCount(m_frameCount[DEVICE_TO_ADF]),
				                                                             _GetLogginStringFromFrameDeltaTime(m_frameDeltaTime[DEVICE_TO_ADF]),
				                                                             _GetLoggingStringFromVec3(m_tangoPosition[DEVICE_TO_ADF]),
				                                                             _GetLoggingStringFromQuaternion(m_tangoRotation[DEVICE_TO_ADF])) + "</size>");
				
				// RELOCALIZATION
				GUI.Label( new Rect(Common.UI_LABEL_START_X, 
				                    Common.UI_POSE_LABEL_START_Y + Common.UI_LABEL_OFFSET * (++guiIndex),
				                    Common.UI_LABEL_SIZE_X , 
				                    Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_TARGET_TO_BASE_FRAME,
				                                                             "Start",
				                                                             "ADF") + "</size>");
				
				GUI.Label( new Rect(Common.UI_LABEL_START_X, 
				                    Common.UI_POSE_LABEL_START_Y + Common.UI_LABEL_OFFSET * (++guiIndex),
				                    Common.UI_LABEL_SIZE_X , 
				                    Common.UI_LABEL_SIZE_Y), Common.UI_FONT_SIZE + String.Format(Common.UX_STATUS,
				                                                             _GetLoggingStringFromPoseStatus(m_status[START_TO_ADF]),
				                                                             _GetLoggingStringFromFrameCount(m_frameCount[START_TO_ADF]),
				                                                             _GetLogginStringFromFrameDeltaTime(m_frameDeltaTime[START_TO_ADF]),
				                                                             _GetLoggingStringFromVec3(m_tangoPosition[START_TO_ADF]),
				                                                             _GetLoggingStringFromQuaternion(m_tangoRotation[START_TO_ADF])) + "</size>");
			}
		}
		GUI.color = oldColor;
	}
}
