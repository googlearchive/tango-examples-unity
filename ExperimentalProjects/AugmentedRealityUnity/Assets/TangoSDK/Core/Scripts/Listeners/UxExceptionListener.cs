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

public delegate void onMovingTooFastEventHandler(string movement);
public delegate void onCameraOverExposedEventHandler(string exposure);
public delegate void onCameraUnderExposedEventHandler(string value);
public delegate void onTooFewFeaturesEventHandler(string features);
public delegate void onTooFewPointsEventHandler(string points);
public delegate void onLyingOnSurfaceEventHandler(string value);
public delegate void onApplicationNotRespondingEventHandler();
public delegate void onUpdateNeededEventHandler();
public delegate void onMotionTrackingInvalidEventHandler(string exceptionStatus);
public delegate void onTangoServiceNotRespondingEventHandler();
public delegate void onAppNotRespondingEventHandler();
public delegate void onVersionUpdateNeededEventHandler();
public delegate void onIncompatibleVMFoundEventHandler();

/// <summary>
/// Tando User Experience exception listener.
/// </summary>
public class UxExceptionListener : AndroidJavaProxy
{
	private static UxExceptionListener m_instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="UxExceptionListener"/> class.
    /// </summary>
	private UxExceptionListener(): base("com.google.atap.tango.ux.listeners.UxExceptionListener"){}

	private event onMovingTooFastEventHandler m_onMovingTooFast;
	private event onCameraOverExposedEventHandler m_onCameraOverExposed;
	private event onCameraUnderExposedEventHandler m_onCameraUnderExposed;
	private event onTooFewFeaturesEventHandler m_onTooFewFeatures;
	private event onTooFewPointsEventHandler m_onTooFewPoints;
	private event onLyingOnSurfaceEventHandler m_onLyingOnSurface;
	private event onApplicationNotRespondingEventHandler m_onApplicationNotResponding;
	private event onUpdateNeededEventHandler m_onUpdateNeeded;
	private event onMotionTrackingInvalidEventHandler m_onMotionTrackingInvalid;
	private event onTangoServiceNotRespondingEventHandler m_onTangoServiceNotResponding;
	private event onAppNotRespondingEventHandler m_onAppNotResponding;
	private event onVersionUpdateNeededEventHandler m_onVersionUpdateNeeded;
	private event onIncompatibleVMFoundEventHandler m_onIncompatibleVMFound;

    /// <summary>
    /// Gets the get instance.
    /// </summary>
    /// <value>The get instance.</value>
	public static UxExceptionListener GetInstance
	{
		get
		{
			if(m_instance == null)
			{
				m_instance = new UxExceptionListener();
			}

			return m_instance;
		}
	}

    /// <summary>
    /// Registers the on moving too fast.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnMovingTooFast(onMovingTooFastEventHandler handler)
	{
		if(handler != null)
		{
			m_onMovingTooFast += handler;
		}
	}

    /// <summary>
    /// Registers the on camera over exposed.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnCameraOverExposed(onCameraOverExposedEventHandler handler)
	{
		if(handler != null)
		{
			m_onCameraOverExposed += handler;
		}
	}

    /// <summary>
    /// Registers the on camer under exposed.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnCamerUnderExposed(onCameraUnderExposedEventHandler handler)
	{
		if(handler != null)
		{
			m_onCameraUnderExposed += handler;
		}
	}

    /// <summary>
    /// Registers the on too few features.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnTooFewFeatures(onTooFewFeaturesEventHandler handler)
	{
		if(handler != null)
		{
			m_onTooFewFeatures += handler;
		}
	}

    /// <summary>
    /// Registers the on too few points.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnTooFewPoints(onTooFewPointsEventHandler handler)
	{
		if(handler != null)
		{
			m_onTooFewPoints += handler;
		}
	}

    /// <summary>
    /// Registers the on lying on surface.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnLyingOnSurface(onLyingOnSurfaceEventHandler handler)
	{
		if(handler != null)
		{
			m_onLyingOnSurface += handler;
		}
	}

    /// <summary>
    /// Registers the on application not responding.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnApplicationNotResponding(onApplicationNotRespondingEventHandler handler)
	{
		if(handler != null)
		{
			m_onApplicationNotResponding += handler;
		}
	}

    /// <summary>
    /// Registers the on update needed.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnUpdateNeeded(onUpdateNeededEventHandler handler)
	{
		if(handler != null)
		{
			m_onUpdateNeeded += handler;
		}
	}

    /// <summary>
    /// Registers the on motion tracking invalid.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnMotionTrackingInvalid(onMotionTrackingInvalidEventHandler handler)
	{
		if(handler != null)
		{
			m_onMotionTrackingInvalid += handler;
		}
	}

    /// <summary>
    /// Registers the on tango service not responding.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnTangoServiceNotResponding(onTangoServiceNotRespondingEventHandler handler)
	{
		if(handler != null)
		{
			m_onTangoServiceNotResponding += handler;
		}
	}

    /// <summary>
    /// Registers the on app not responding.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnAppNotResponding(onAppNotRespondingEventHandler handler)
	{
		if(handler != null)
		{
			m_onAppNotResponding += handler;
		}
	}

    /// <summary>
    /// Registers the on version update needed.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnVersionUpdateNeeded(onVersionUpdateNeededEventHandler handler)
	{
		if(handler != null)
		{
			m_onVersionUpdateNeeded += handler;
		}
	}

    /// <summary>
    /// Registers the on incompatible VM found.
    /// </summary>
    /// <param name="handler">Handler.</param>
	public void RegisterOnIncompatibleVMFound(onIncompatibleVMFoundEventHandler handler)
	{
		if(handler != null)
		{
			m_onIncompatibleVMFound += handler;
		}
	}

    /// <summary>
    /// Unregisters the on moving too fast.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnMovingTooFast(onMovingTooFastEventHandler handler)
    {
        if(handler != null)
        {
            m_onMovingTooFast -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on camera over exposed.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnCameraOverExposed(onCameraOverExposedEventHandler handler)
    {
        if(handler != null)
        {
            m_onCameraOverExposed -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on camer under exposed.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnCamerUnderExposed(onCameraUnderExposedEventHandler handler)
    {
        if(handler != null)
        {
            m_onCameraUnderExposed -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on too few features.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnTooFewFeatures(onTooFewFeaturesEventHandler handler)
    {
        if(handler != null)
        {
            m_onTooFewFeatures -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on too few points.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnTooFewPoints(onTooFewPointsEventHandler handler)
    {
        if(handler != null)
        {
            m_onTooFewPoints -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on lying on surface.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnLyingOnSurface(onLyingOnSurfaceEventHandler handler)
    {
        if(handler != null)
        {
            m_onLyingOnSurface -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on application not responding.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnApplicationNotResponding(onApplicationNotRespondingEventHandler handler)
    {
        if(handler != null)
        {
            m_onApplicationNotResponding -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on update needed.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnUpdateNeeded(onUpdateNeededEventHandler handler)
    {
        if(handler != null)
        {
            m_onUpdateNeeded -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on motion tracking invalid.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnMotionTrackingInvalid(onMotionTrackingInvalidEventHandler handler)
    {
        if(handler != null)
        {
            m_onMotionTrackingInvalid -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on tango service not responding.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnTangoServiceNotResponding(onTangoServiceNotRespondingEventHandler handler)
    {
        if(handler != null)
        {
            m_onTangoServiceNotResponding -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on app not responding.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnAppNotResponding(onAppNotRespondingEventHandler handler)
    {
        if(handler != null)
        {
            m_onAppNotResponding -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on version update needed.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnVersionUpdateNeeded(onVersionUpdateNeededEventHandler handler)
    {
        if(handler != null)
        {
            m_onVersionUpdateNeeded -= handler;
        }
    }

    /// <summary>
    /// Unregisters the on incompatible VM found.
    /// </summary>
    /// <param name="handler">Handler.</param>
    public void UnregisterOnIncompatibleVMFound(onIncompatibleVMFoundEventHandler handler)
    {
        if(handler != null)
        {
            m_onIncompatibleVMFound -= handler;
        }
    }

    void onMovingTooFast(string movement)
	{
		if(m_onMovingTooFast != null)
		{
			m_onMovingTooFast(movement);
		}
	}
    void onCameraOverExposed(string exposure)
	{
		if(m_onCameraOverExposed != null)
		{
			m_onCameraOverExposed(exposure);
		}
	}
    void onCameraUnderExposed(string value)
	{
		Debug.Log("onCameraUnderExposed");
		if(m_onCameraUnderExposed != null)
		{
			m_onCameraUnderExposed(value);
		}
	}
    void onSpaceNotRecognized(string features)
	{
		if(m_onTooFewFeatures != null)
		{
			m_onTooFewFeatures(features);
		}
	}
    void onUnableToDetectSurface(string points)
	{
		if(m_onTooFewPoints != null)
		{
			m_onTooFewPoints(points);
		}
	}
    void onLyingOnSurface(string value)
	{
		if(m_onLyingOnSurface != null)
		{
			m_onLyingOnSurface(value);
		}
	}
    void onApplicationNotResponding()
	{
		if(m_onApplicationNotResponding != null)
		{
			m_onApplicationNotResponding();
		}
	}
    void onUpdateNeeded()
	{
		if(m_onUpdateNeeded != null)
		{
			m_onUpdateNeeded();
		}
	}
    void onMotionTrackingInvalid(string exceptionStatus)
	{
		if(m_onMotionTrackingInvalid != null)
		{
			m_onMotionTrackingInvalid(exceptionStatus);
		}
	}
    void onDeviceNotResponding()
	{
		if(m_onTangoServiceNotResponding != null)
		{
			m_onTangoServiceNotResponding();
		}
	}
    void onAppNotResponding()
	{
		if(m_onAppNotResponding != null)
		{
			m_onAppNotResponding();
		}
	}
    void onVersionUpdateNeeded()
	{
		if(m_onVersionUpdateNeeded != null)
		{
			m_onVersionUpdateNeeded();
		}
	}
    void onIncompatibleVMFound()
	{
		if(m_onIncompatibleVMFound != null)
		{
			m_onIncompatibleVMFound();
		}
	}
}
