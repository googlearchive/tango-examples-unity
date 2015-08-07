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
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Tango SDK about window.
/// </summary>
public class TangoSDKAbout : EditorWindow
{
	public static readonly string TANGO_VERSION_DATA_PATH = "/TangoSDK/Core/Tango.dat";
	public string m_branch = string.Empty;
	public string m_versionTag = string.Empty;
	public string m_gitHash = string.Empty;
	public bool m_validData = false;

	/// <summary>
	/// Show the About Tango Window
	/// </summary>
	[MenuItem("Tango/About SDK")]
	public static void GetSDKVersion()
	{
		EditorWindow thisWindow = EditorWindow.GetWindow(typeof(TangoSDKAbout));

		((TangoSDKAbout)thisWindow).ReadVersionFile();
	}

	/// <summary>
	/// Reads the version file.
	/// </summary>
	public void ReadVersionFile()
	{
		if(File.Exists(Application.dataPath + TANGO_VERSION_DATA_PATH))
		{
			BinaryReader binaryReader = new BinaryReader(File.Open(Application.dataPath + TANGO_VERSION_DATA_PATH, FileMode.Open));
			m_versionTag = binaryReader.ReadString();
			m_gitHash = binaryReader.ReadString();
			m_branch = binaryReader.ReadString();
			m_validData = true;
        }
    }

	/// <summary>
	/// Raises the GU event.
	/// </summary>
	void OnGUI()
	{
		if(m_validData)
		{
			EditorGUILayout.LabelField("Version: ", m_versionTag);
			EditorGUILayout.LabelField("Branch: ", m_branch);
			EditorGUILayout.LabelField("Hash: ", m_gitHash);
		}
		else
		{
			EditorGUILayout.LabelField("Version data not found");
		}
	}
}
