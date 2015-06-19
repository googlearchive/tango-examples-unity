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

/// <summary>
/// Exception events fired by the UX exception listener.
/// </summary>
public interface ITangoUX
{
	void onMovingTooFastEventHandler(string movement);
	void onCameraOverExposedEventHandler(string exposure);
	void onCameraUnderExposedEventHandler(string value);
	void onTooFewFeaturesEventHandler(string features);
	void onTooFewPointsEventHandler(string points);
	void onLyingOnSurfaceEventHandler(string value);
	void onMotionTrackingInvalidEventHandler(string exceptionStatus);
	void onTangoServiceNotRespondingEventHandler();
	void onVersionUpdateNeededEventHandler();
	void onIncompatibleVMFoundEventHandler();
}
