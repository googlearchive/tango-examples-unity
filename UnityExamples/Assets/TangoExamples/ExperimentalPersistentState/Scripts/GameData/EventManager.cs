//-----------------------------------------------------------------------
// <copyright file="EventManager.cs" company="Google">
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

/// <summary>
/// Singleton event manager.
/// </summary>
public class EventManager : MonoBehaviour
{
    private static EventManager m_instance;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    /// <value>The instance.</value>
    public static EventManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType<EventManager>();
                DontDestroyOnLoad(m_instance.gameObject);
            }

            return m_instance;
        }
    }

    /// <summary>
    /// Called when the Tango service is initialized.
    /// </summary>
    public delegate void TangoServiceInitilizedHandler();

    /// <summary>
    /// Called when the Tango service is initialized.
    /// </summary>
    public static event TangoServiceInitilizedHandler TangoServiceInitialized;

    /// <summary>
    /// Called when a save is requested.
    /// </summary>
    /// <param name="successed">If the save succeded or not.</param>
    public delegate void GameDataSavedHandler(bool successed);

    /// <summary>
    /// Called when a save is requested.
    /// </summary>
    public static event GameDataSavedHandler GameDataSaved;

    /// <summary>
    /// Called when a pose state update happens.
    /// </summary>
    /// <param name="currentState">The current pose state.</param>
    public delegate void TangoPoseStateHandler(TangoPoseStates currentState);

    /// <summary>
    /// Called when a pose update happens.
    /// </summary>
    public static event TangoPoseStateHandler TangoPoseStateChanged;

    /// <summary>
    /// Called before Start.
    /// </summary>
    public void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != m_instance)
            {
                Destroy(this.gameObject);
            }
        }
    }

    /// <summary>
    /// Fire the TangoServiceInitilized event.
    /// </summary>
    public void SendTangoServiceInitialized()
    {
        if (TangoServiceInitialized != null)
        {
            TangoServiceInitialized();
        }
    }

    /// <summary>
    /// Fire the GameDataSaved event.
    /// </summary>
    /// <param name="successed">If set to <c>true</c> successed.</param>
    public void SendGameDataSaved(bool successed)
    {
        if (GameDataSaved != null)
        {
            GameDataSaved(successed);
        }
    }

    /// <summary>
    /// Fire the TangoPoseStateChanged event.
    /// </summary>
    /// <param name="currentState">Current state.</param>
    public void SendTangoPoseStateChanged(TangoPoseStates currentState)
    {
        if (TangoPoseStateChanged != null)
        {
            TangoPoseStateChanged(currentState);
        }
    }
}
