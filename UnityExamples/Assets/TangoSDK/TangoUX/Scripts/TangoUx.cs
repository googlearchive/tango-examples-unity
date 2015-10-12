//-----------------------------------------------------------------------
// <copyright file="TangoUx.cs" company="Google">
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
using System.Collections;
using UnityEngine;

namespace Tango
{
    /// <summary>
    /// Main entry point for the Tango UX Library.
    /// 
    /// This component handles nearly all communication with the underlying Tango UX Library.  Customization of the 
    /// UX library can be done in the Unity editor or by programatically setting the member flags.
    /// </summary>
    [RequireComponent(typeof(TangoApplication))]
    public class TangoUx : MonoBehaviour, ITangoPose, ITangoEventMultithreaded, ITangoDepth
    {
        public bool m_enableUXLibrary = true;
        public bool m_drawDefaultUXExceptions = true;
        public bool m_showConnectionScreen = true;

        private TangoApplication m_tangoApplication;

        /// <summary>
        /// Start this instance.
        /// </summary>
        public void Start()
        {
            m_tangoApplication = GetComponent<TangoApplication>();
            m_tangoApplication.RegisterPermissionsCallback(_OnTangoPermissionsEvent);
            m_tangoApplication.RegisterOnTangoConnect(_OnTangoServiceConnected);
            m_tangoApplication.RegisterOnTangoDisconnect(_OnTangoServiceDisconnected);
            m_tangoApplication.Register(this);
            AndroidHelper.InitTangoUx();
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        public void OnDestroy()
        {
            if (m_tangoApplication)
            {
                m_tangoApplication.UnregisterPermissionsCallback(_OnTangoPermissionsEvent);
                m_tangoApplication.UnregisterOnTangoConnect(_OnTangoServiceConnected);
                m_tangoApplication.UnregisterOnTangoDisconnect(_OnTangoServiceDisconnected);
                m_tangoApplication.Unregister(this);
            }
        }

        /// <summary>
        /// Register the specified tangoObject.
        /// </summary>
        /// <param name="tangoObject">Tango object.</param>
        public void Register(Object tangoObject)
        {
            if (m_enableUXLibrary)
            {
                ITangoUX tangoUX = tangoObject as ITangoUX;
                
                if (tangoUX != null)
                {
                    UxExceptionEventListener.GetInstance.RegisterOnUxExceptionEventHandler(tangoUX.OnUxExceptionEventHandler);
                }
            }
        }

        /// <summary>
        /// Unregister the specified tangoObject.
        /// </summary>
        /// <param name="tangoObject">Tango object.</param>
        public void Unregister(Object tangoObject)
        {
            if (m_enableUXLibrary)
            {
                ITangoUX tangoUX = tangoObject as ITangoUX;
                
                if (tangoUX != null)
                {
                    UxExceptionEventListener.GetInstance.UnregisterOnUxExceptionEventHandler(tangoUX.OnUxExceptionEventHandler);
                }
            }
        }

        /// <summary>
        /// Raises the tango pose available event.
        /// </summary>
        /// <param name="poseData">Pose data.</param>
        public void OnTangoPoseAvailable(Tango.TangoPoseData poseData)
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.ParseTangoPoseStatus((int)poseData.status_code);
            }
        }

        /// <summary>
        /// Raises the tango event available event handler event.
        /// </summary>
        /// <param name="tangoEvent">Tango event.</param>
        public void OnTangoEventMultithreadedAvailableEventHandler(Tango.TangoEvent tangoEvent)
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.ParseTangoEvent(tangoEvent.timestamp,
                                              (int)tangoEvent.type,
                                              tangoEvent.event_key,
                                              tangoEvent.event_value);
            }
        }

        /// <summary>
        /// Raises the tango depth available event.
        /// </summary>
        /// <param name="tangoDepth">Tango depth.</param>
        public void OnTangoDepthAvailable(Tango.TangoUnityDepth tangoDepth)
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.ParseTangoDepthPointCount(tangoDepth.m_pointCount);
            }
        }

        /// <summary>
        /// Sets the recommended way to hold the device.
        /// </summary>
        /// <param name="holdPostureType">Hold posture type.</param>
        public void SetHoldPosture(TangoUxEnums.UxHoldPostureType holdPostureType)
        {
            AndroidHelper.SetHoldPosture((int)holdPostureType);
        }

        /// <summary>
        /// Start exceptions listener.
        /// </summary>
        /// <returns>The start exceptions listener.</returns>
        private IEnumerator _StartExceptionsListener()
        {
            AndroidHelper.ShowStandardTangoExceptionsUI(m_drawDefaultUXExceptions);
            AndroidHelper.SetUxExceptionEventListener();
            yield return 0;
        }
        
        /// <summary>
        /// On tango service connected.
        /// </summary>
        private void _OnTangoServiceConnected()
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.StartTangoUX(m_tangoApplication.m_enableMotionTracking && m_showConnectionScreen);
            }
        }
        
        /// <summary>
        /// On tango service disconnected.
        /// </summary>
        private void _OnTangoServiceDisconnected()
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.StopTangoUX();
            }
        }
        
        /// <summary>
        /// On tango permissions event.
        /// </summary>
        /// <param name="permissionsGranted">If set to <c>true</c> permissions granted.</param>
        private void _OnTangoPermissionsEvent(bool permissionsGranted)
        {
            if (m_enableUXLibrary && permissionsGranted)
            {
                StartCoroutine(_StartExceptionsListener());
            }
        }
    }
}