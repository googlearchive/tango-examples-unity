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
using Tango;

/// <summary>
/// Controls of pointcloud scene of YS.
/// </summary>
using System;

/// <summary>
/// Pointcloud camera perspective switch.
/// </summary>
public class PointcloudSwitch : MonoBehaviour 
{
    public GUISkin guiSkin;
    public Pointcloud pointcloud;

	private TangoApplication m_tangoApplication;

	private void Start()
	{
		m_tangoApplication = FindObjectOfType<TangoApplication>();
	}
	
    /// <summary>
    /// GUI for switch getting data API and status.
    /// </summary>
    private void OnGUI()
    {
		if(m_tangoApplication.HasRequestedPermissions())
		{
			Color oldColor = GUI.color;
			GUI.color = Color.gray;
			
			GUI.Label(new Rect(Common.UI_LABEL_START_X, 
			                   Common.UI_LABEL_START_Y, 
			                   Common.UI_LABEL_SIZE_X , 
			                   Common.UI_LABEL_SIZE_Y), "<size=15>" + String.Format(Common.UX_TANGO_SERVICE_VERSION, TangoApplication.GetTangoServiceVersion()) + "</size>");


	        GUI.Label(new Rect(Common.UI_LABEL_START_X, 
			                   Common.UI_LABEL_START_Y + Common.UI_LABEL_OFFSET, 
	                           Common.UI_LABEL_SIZE_X , 
			                   Common.UI_LABEL_SIZE_Y), "<size=15>Average Depth (m): " + pointcloud.m_overallZ.ToString() + "</size>");

	        GUI.Label(new Rect(Common.UI_LABEL_START_X, 
			                   Common.UI_LABEL_START_Y + Common.UI_LABEL_OFFSET * 2.0f, 
	                           Common.UI_LABEL_SIZE_X , 
			                   Common.UI_LABEL_SIZE_Y), "<size=15>Point Count: " + pointcloud.m_pointsCount.ToString() + "</size>");


	        GUI.Label(new Rect(Common.UI_LABEL_START_X, 
			                   Common.UI_LABEL_START_Y + Common.UI_LABEL_OFFSET * 3.0f, 
	                           Common.UI_LABEL_SIZE_X , 
	                           Common.UI_LABEL_SIZE_Y), "<size=15>Frame delta time (ms): " + pointcloud.GetTimeSinceLastFrame().ToString("0.") + "</size>");
	    
			GUI.color = oldColor;
		}
	}
}
