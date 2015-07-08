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

// This controller shows an example of subscribing to exception callbacks from the Project Tango UX Library.
// Here, we only print, but these callbacks could be used to show custom content when certain exception
// types occur.
public class UXController : MonoBehaviour {
	void Start () {
		UxExceptionListener.GetInstance.RegisterOnMovingTooFast(_onMovingTooFast);
		UxExceptionListener.GetInstance.RegisterOnCameraOverExposed(_onCameraOverExposed);
		UxExceptionListener.GetInstance.RegisterOnCamerUnderExposed(_onCameraUnderExposed);
		UxExceptionListener.GetInstance.RegisterOnLyingOnSurface (_onLyingOnSurface);
		UxExceptionListener.GetInstance.RegisterOnTooFewFeatures (_onTooFewFeatures);
		UxExceptionListener.GetInstance.RegisterOnTooFewPoints (_onTooFewPoints);
		UxExceptionListener.GetInstance.RegisterOnMotionTrackingInvalid (_onMotionTrackingInvalid);
		UxExceptionListener.GetInstance.RegisterOnTangoServiceNotResponding(_onTangoServiceNotResponding);
		UxExceptionListener.GetInstance.RegisterOnVersionUpdateNeeded(_onVersionUpdateNeeded);
		UxExceptionListener.GetInstance.RegisterOnIncompatibleVMFound (_onIncompatibleVMFound);
	}
	
	private void _onMovingTooFast(string value)
	{
		Debug.Log("UX onMovingTooFast : " + value);
	}
	
	private void _onCameraOverExposed(string value)
	{
		Debug.Log("UX onCameraOverExposed : " + value);
	}
	
	private void _onCameraUnderExposed(string value)
	{
		Debug.Log("UX onCameraUnderExposed : " + value);
	}
	
	private void _onLyingOnSurface(string value)
	{
		Debug.Log("UX onLyingOnSurface : " + value);
	}
	
	private void _onTooFewFeatures(string value)
	{
		Debug.Log("UX _onTooFewFeatures : " + value);
	}
	
	private void _onTooFewPoints(string value)
	{
		Debug.Log("UX _onTooFewPoints : " + value);
	}
	
	private void _onMotionTrackingInvalid(string value)
	{
		Debug.Log("UX _onMotionTrackingInvalid : " + value);
	}
	
	private void _onTangoServiceNotResponding()
	{
		Debug.Log("Tango Service Not Responding");
	}
	
	private void _onApplicationNotResponding()
	{
		Debug.Log("Application Not Responding");
	}
	
	private void _onVersionUpdateNeeded()
	{
		Debug.Log("Service Update Needed");
	}
	
	private void _onIncompatibleVMFound()
	{
		Debug.Log ("VM Is Not Compatible");			
	}
}
