using UnityEngine;
using System.Collections;

public partial class AndroidHelper
{
    
    #pragma warning disable 414
    private static AndroidJavaObject m_tangoUxHelper = null;
    #pragma warning restore 414
    
    /// <summary>
    /// Gets the Java tango helper object.
    /// </summary>
    /// <returns>The tango helper object.</returns>
    public static AndroidJavaObject GetTangoUxHelperObject()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if(m_tangoUxHelper == null)
        {
            m_tangoUxHelper = new AndroidJavaObject("com.projecttango.unityuxhelper.TangoUnityUxHelper", GetUnityActivity());
        }
        return m_tangoUxHelper;
        #else
        return null;
        #endif
    }

    /// <summary>
    /// Parses the tango event.
    /// </summary>
    /// <param name="timestamp">Timestamp.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="key">Key.</param>
    /// <param name="value">Value.</param>
    public static void ParseTangoEvent(double timestamp, int eventType, string key, string value)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("processTangoEvent", timestamp, eventType, key, value);
        }
    }  

    /// <summary>
    /// Parses the tango pose status.
    /// </summary>
    /// <param name="poseStatus">Pose status.</param>
    public static void ParseTangoPoseStatus(int poseStatus)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("processPoseDataStatus", poseStatus);
        }
    }
    
    /// <summary>
    /// Parses the tango depth point count.
    /// </summary>
    /// <param name="pointCount">Point count.</param>
    public static void ParseTangoDepthPointCount(int pointCount)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("processXyzIjPointCount", pointCount);
        }
    }
    
    /// <summary>
    /// Initialize tango ux library.
    /// </summary>
    /// <param name="isMotionTrackingEnabled">A flag to indicate if motion tracking is enabled.</param>
    public static void InitTangoUx(bool isMotionTrackingEnabled)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("initTangoUx", isMotionTrackingEnabled);
        }
    }

    /// <summary>
    /// Shows the standard tango exceptions UI.
    /// </summary>
    /// <param name="shouldUseDefaultUi">A flag to indicate if default TangoUx UI is enabled.</param>
    public static void ShowStandardTangoExceptionsUI(bool shouldUseDefaultUi)
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("showDefaultExceptionsUi", shouldUseDefaultUi);
        }
    }
    
    /// <summary>
    /// Starts the tango UX library.
    /// Should be called after connecting to Tango service.
    /// </summary>
    public static void StartTangoUX()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("start");
        }
    }
    
    /// <summary>
    /// Stops the tango UX library.
    /// Should be called before disconnect.
    /// </summary>
    public static void StopTangoUX()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("stop");
        }
    }

    /// <summary>
    /// Sets the Tango Ux exception event listener.
    /// </summary>
    public static void SetUxExceptionEventListener()
    {
        AndroidJavaObject tangoUxObject = GetTangoUxHelperObject();
        if(tangoUxObject != null)
        {
            tangoUxObject.Call("setUxExceptionEventListener", UxExceptionEventListener.GetInstance);
        }
    }
}
