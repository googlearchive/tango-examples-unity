//-----------------------------------------------------------------------
// <copyright file="UxExceptionEventListener.cs" company="Google">
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
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tango User Experience exception listener.
/// </summary>
public class UxExceptionEventListener : AndroidJavaProxy
{
    private static UxExceptionEventListener m_instance;

    /// <summary>
    /// The lock object used as a mutex.
    /// </summary>
    private static System.Object m_lockObject = new System.Object();
 
    /// <summary>
    /// A queue that holds ux exception events waiting to be sent on the main Unity thread.
    /// </summary>
    private static Queue<Tango.UxExceptionEvent> m_tangoPendingEventQueue = new Queue<Tango.UxExceptionEvent>();
    
    /// <summary>
    /// Occurs when a UX Exception event happens.
    /// </summary>
    private static OnUxExceptionEventHandler m_onUxExceptionEvent;
          
    /// <summary>
    /// Occurs when a UX Exception event happens.
    /// </summary>
    private static OnUxExceptionEventHandler m_onUxExceptionEventMultithreadedAvailable;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UxExceptionEventListener"/> class.
    /// </summary>
    private UxExceptionEventListener() : base("com.google.atap.tango.ux.UxExceptionEventListener")
    {
    }
    
    /// <summary>
    /// Delegate for UX Exception events.
    /// </summary>
    /// <param name="tangoUxEvent">The exception event from Tango.</param>
    public delegate void OnUxExceptionEventHandler(Tango.UxExceptionEvent tangoUxEvent);
    
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static UxExceptionEventListener GetInstance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new UxExceptionEventListener();
            }

            return m_instance;
        }
    }

    /// <summary>
    /// Registers UX Exception event handler.
    /// </summary>
    /// <param name="handler">Event handler.</param>
    internal static void RegisterOnUxExceptionEventHandler(OnUxExceptionEventHandler handler)
    {
        if (handler != null)
        {
            m_onUxExceptionEvent += handler;
        }
    }

    /// <summary>
    /// Unregisters the on too few points.
    /// </summary>
    /// <param name="handler">Event handler.</param>
    internal static void UnregisterOnUxExceptionEventHandler(OnUxExceptionEventHandler handler)
    {
        if (handler != null)
        {
            m_onUxExceptionEvent -= handler;
        }
    }

    /// <summary>
    /// Raise a Tango event if there is new data.
    /// </summary>
    internal static void SendIfAvailable()
    { 
        Tango.UxExceptionEvent eventCopy;
        float startTime = Time.time;
       
        if (m_onUxExceptionEvent == null)
        {
            return;
        }

        int queueCount = m_tangoPendingEventQueue.Count;
        for (int i = 0; i < queueCount; i++)
        {
            // Copy the struct on the Unity main thread inside the lock and get out of the lock.
            lock (m_lockObject)
            {   
                eventCopy = m_tangoPendingEventQueue.Dequeue();
            }

            m_onUxExceptionEvent(eventCopy);
        }
    }

    /// <summary>
    /// Register a multithread handler for Tango events.
    /// </summary>
    /// <param name="handler">Event handler to register.</param>
    internal static void RegisterOnOnUxExceptionEventMultithreadedAvailable(OnUxExceptionEventHandler handler)
    {
        if (handler != null)
        {
            m_onUxExceptionEventMultithreadedAvailable += handler;
        }
    }

    /// <summary>
    /// Unregister a multithread handler for the Tango depth event.
    /// </summary>
    /// <param name="handler">Event handler to unregister.</param>
    internal static void UnregisterOnUxExceptionEventMultithreadedAvailable(OnUxExceptionEventHandler handler)
    {
        if (handler != null)
        {
            m_onUxExceptionEventMultithreadedAvailable -= handler;
        }
    }

    /// <summary>
    /// Called when a UX Exception event is dispatched.
    /// </summary>
    /// <param name="tangoUxEvent">A AndroidJavaObject containing information about the exception.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules",
                                                     "SA1300:ElementMustBeginWithUpperCaseLetter",
                                                     Justification = "Called from Java.")]
    private void onUxExceptionEvent(AndroidJavaObject tangoUxEvent)
    {
        // Copy the exception event data to UxEvent struct.
        Tango.UxExceptionEvent uxEvent;
        uxEvent.type = (Tango.TangoUxEnums.UxExceptionEventType)tangoUxEvent.Call<int>("getType");
        uxEvent.value = tangoUxEvent.Call<float>("getValue");
        uxEvent.status = (Tango.TangoUxEnums.UxExceptionEventStatus)tangoUxEvent.Call<int>("getStatus");

        // Immediately fire async event.
        if (m_onUxExceptionEventMultithreadedAvailable != null)
        {
            m_onUxExceptionEventMultithreadedAvailable(uxEvent);
        }

        // Enqueue event to fire synchronized event(s) later on Unity thread.
        lock (m_lockObject)
        {
            m_tangoPendingEventQueue.Enqueue(uxEvent);
        }  
    }
}
