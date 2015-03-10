/*
 * Copyright 2015 Google Inc. All Rights Reserved.
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

public enum TangoPoseStates {
	Connecting,
	Relocalizing, // Relocalizing means running under motion tracking mode.
	Running,
	Unknown
}

public class Statics {
	// Game states.
	public static bool isPlacingObject = false;

	// Tango states.
	public static TangoPoseStates currentTangoState;

	// String consts.
	public static string curADFId = "";
	public static string curADFName = "";
	public static string debugString = "statics";
	public static string uiPanelConnectingService = "Connecting Tango Service";
	public static string uiPanelRelocalizing = "Walk around to relocalize";

	// UI const.
	public static float buildingPickerSlideInPosX = 6.65f;
	public static float buildingPickerSlideOutPosX = 8.3f;
}
