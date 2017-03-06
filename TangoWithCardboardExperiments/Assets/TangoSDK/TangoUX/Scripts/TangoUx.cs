//-----------------------------------------------------------------------
// <copyright file="TangoUx.cs" company="Google">
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

namespace Tango
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Main entry point for the Tango UX Library.
    /// 
    /// This component handles nearly all communication with the underlying Tango UX Library.  Customization of the 
    /// UX library can be done in the Unity editor or by programmatically setting the member flags.
    /// </summary>
    [RequireComponent(typeof(TangoApplication))]
    public class TangoUx : MonoBehaviour, ITangoLifecycle, ITangoPose, ITangoEventMultithreaded, ITangoDepthMultithreaded
    {
        public bool m_enableUXLibrary = true;
        public bool m_drawDefaultUXExceptions = true;
        public bool m_showConnectionScreen = true;
        public TangoUxEnums.UxHoldPostureType m_holdPosture = TangoUxEnums.UxHoldPostureType.NONE;

        private TangoApplication m_tangoApplication;
        private bool m_isTangoUxStarted = false;

        /// <summary>
        /// Start this instance.
        /// </summary>
        public void Start()
        {
            m_tangoApplication = GetComponent<TangoApplication>();
            m_tangoApplication.Register(this);
            AndroidHelper.InitTangoUx();
            SetHoldPosture(m_holdPosture);
        }

        /// <summary>
        /// Disperse any events related to TangoUX functionality.
        /// </summary>
        public void Update()
        {
            UxExceptionEventListener.SendIfAvailable();
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        public void OnDestroy()
        {
            if (m_tangoApplication)
            {
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
                    UxExceptionEventListener.RegisterOnUxExceptionEventHandler(tangoUX.OnUxExceptionEventHandler);
                }

                ITangoUXMultithreaded tangoUXMultithreaded = tangoObject as ITangoUXMultithreaded;
                
                if (tangoUXMultithreaded != null)
                {
                    UxExceptionEventListener.RegisterOnOnUxExceptionEventMultithreadedAvailable(tangoUXMultithreaded.OnUxExceptionEventMultithreadedAvailableEventHandler);
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
                    UxExceptionEventListener.UnregisterOnUxExceptionEventHandler(tangoUX.OnUxExceptionEventHandler);
                }

                ITangoUXMultithreaded tangoUXMultithreaded = tangoObject as ITangoUXMultithreaded;
                
                if (tangoUXMultithreaded != null)
                {
                    UxExceptionEventListener.UnregisterOnUxExceptionEventMultithreadedAvailable(tangoUXMultithreaded.OnUxExceptionEventMultithreadedAvailableEventHandler);
                }
            }
        }

        /// <summary>
        /// Callback to handle Android permissions being granted.
        /// </summary>
        /// <param name="permissionsGranted">If set to <c>true</c>, then permissions were granted; otherwise <c>false</c>.</param>
        public void OnAndroidPermissions(bool permissionsGranted)
        {
            if (m_enableUXLibrary && permissionsGranted)
            {
                _StartExceptionsListener();

                if (m_tangoApplication.m_autoConnectToService)
                {
                    if (!m_isTangoUxStarted)
                    {
                        AndroidHelper.StartTangoUX(m_tangoApplication.m_enableMotionTracking && m_showConnectionScreen);
                        m_isTangoUxStarted = true;
                    }
                }
            }
        }

        /// <summary>
        /// Callback to handle all tango permissions being granted.
        /// </summary>
        /// <param name="permissionsGranted">If set to <c>true</c>, then permissions were granted; otherwise <c>false</c>.</param>
        public void OnTangoPermissions(bool permissionsGranted)
        {
        }

        /// <summary>
        /// This is called when successfully connected to the Tango Service.
        /// </summary>
        public void OnTangoServiceConnected()
        {
            if (m_enableUXLibrary)
            {
                if (!m_isTangoUxStarted)
                {
                    AndroidHelper.StartTangoUX(m_tangoApplication.m_enableMotionTracking && m_showConnectionScreen);
                    m_isTangoUxStarted = true;
                }
            }
        }

        /// <summary>
        /// This is called when disconnected from the Tango Service.
        /// </summary>
        public void OnTangoServiceDisconnected()
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.StopTangoUX();
                m_isTangoUxStarted = false;
            }
        }

        /// <summary>
        /// Raises the Tango pose available event.
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
        /// Raises the Tango event available event handler event.
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
        /// Raises the Tango depth available event.
        /// </summary>
        /// <param name="tangoDepth">Tango depth.</param>
        public void OnTangoDepthMultithreadedAvailable(Tango.TangoXYZij tangoDepth)
        {
            if (m_enableUXLibrary)
            {
                AndroidHelper.ParseTangoDepthPointCount(tangoDepth.xyz_count);
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
        /// Display Tango Service out-of-date notification.
        /// </summary>
        public void ShowTangoOutOfDate()
        {
            AndroidHelper.ShowTangoOutOfDate();
        }

        /// <summary>
        /// Start exceptions listener.
        /// </summary>
        private void _StartExceptionsListener()
        {
#if  UNITY_5_0 || UNITY_5_1 || UNITY_5_2
#else
            // The UX library exception feature will cause a crash on Tango Development Tablet with apk built from
            // Unity 5.3 and above version. We disabled this feature temporarily.
            if (SystemInfo.deviceModel.Equals("Google Project Tango Tablet Development Kit"))
            {
                Debug.Log("Force disabling Tango UX library exception drawing.");
                m_drawDefaultUXExceptions = false;
            }
#endif
            AndroidHelper.ShowStandardTangoExceptionsUI(m_drawDefaultUXExceptions);
            AndroidHelper.SetUxExceptionEventListener();
        }
    }
}
