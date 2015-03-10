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
using System;

public abstract class VideoOverlayListener : MonoBehaviour 
{
	Tango.VideoOverlayProvider.TangoService_onImageAvailable m_onImageAvailable;

	/// <summary>
	/// Sets the callback for image updates.
	/// </summary>
	/// <param name="cameraId">Camera identifier.</param>
	public virtual void SetCallback(Tango.TangoEnums.TangoCameraId cameraId)
	{
		m_onImageAvailable = new Tango.VideoOverlayProvider.TangoService_onImageAvailable(_OnImageAvailable);
		Tango.VideoOverlayProvider.SetCallback(cameraId, m_onImageAvailable);
	}
	
	/// <summary>
	/// Handle the callback sent by the Tango Service
	/// when a new image is sampled.
	/// </summary>
	/// <param name="cameraId">Camera identifier.</param>
	/// <param name="callbackContext">Callback context.</param>
	/// <param name="imageBuffer">Image buffer.</param>
	protected abstract void _OnImageAvailable(IntPtr callbackContext,
	                                          Tango.TangoEnums.TangoCameraId cameraId, 
	                                          Tango.TangoImageBuffer imageBuffer);
}
