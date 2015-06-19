using UnityEngine;
using System.Collections;

public class PopupManager : MonoBehaviour {
    
    [HideInInspector]
    public string debugText;


    [HideInInspector]
    public bool tangoInitialized = false;

    public bool showText = true;
    public Vector2 textPosition = new Vector2 (230, 30);

    public GameObject viewController;
    public Vector3 chartPosition = new Vector3 (-0.5f, 0, 1);

    private float FPSSmoothing = 0.95f;
    private float updateFPS = 60;
    private float apiFPS = 0;
    private float lastUpdateTime = 0;
    private float lastApiTime = 0;
    private LineChart apiChart;
    private LineChart renderChart;
    private LineChart baselineChart;
    private bool isApiFailCheckingStarted = false;

    public bool showPlots = true;
    public GameObject tangoServiceTroublePopup;
    public GameObject tangoInitializePopup;
    public bool isShowingDebugButton = false;

    // Use this for initialization
    void Start () {
        tangoServiceTroublePopup.SetActive(false);
        tangoInitializePopup.SetActive(false);

        apiChart = new LineChart (viewController, chartPosition, Color.red, 100);
        renderChart = new LineChart (viewController, chartPosition, Color.green, 100);
        baselineChart = new LineChart (viewController, chartPosition, Color.gray, 100);

        apiChart.line.enabled = showPlots;
        renderChart.line.enabled = showPlots;
        baselineChart.line.enabled = showPlots;
    }
    
    // Update is called once per frame
    void Update () {
        //timeout popup if we are running without getting any data from the service
        #if UNITY_ANDROID && !UNITY_EDITOR
        tangoInitializePopup.SetActive (!tangoInitialized);
        tangoServiceTroublePopup.SetActive(((lastUpdateTime - lastApiTime) > 5));
        #endif

        apiChart.Update ();
        renderChart.Update ();
        baselineChart.Update ();
    }

    public void TriggerUpdateFPS() {
        if (isApiFailCheckingStarted) {
            float now = Time.realtimeSinceStartup;
            float dt = now - lastUpdateTime;
            lastUpdateTime = now;
            if (dt < float.Epsilon) {
                return;
            }
            if (renderChart != null)
                renderChart.AddData (10 * dt);
            updateFPS = updateFPS * FPSSmoothing + (1.0f - FPSSmoothing) / dt;
        }
    }
    
    public void StartApiFailCheck() {
        isApiFailCheckingStarted = true;
    }

    public void TriggerAPICallbackFPS() {
        float now = Time.realtimeSinceStartup;
        float dt = now - lastApiTime;
        lastApiTime = now;
        if(dt < float.Epsilon) {
            return;
        }
        if(apiChart != null)
            apiChart.AddData (10*dt);
        apiFPS = apiFPS*FPSSmoothing + (1.0f-FPSSmoothing)/dt;
    }

    void OnGUI()
    {
        if (showText) {
            int textLineSpacing = 20;
            GUI.Label(new Rect(textPosition.x, textPosition.y,1000,30), "Update FPS: " + updateFPS.ToString("F2"));
            GUI.Label(new Rect(textPosition.x, textPosition.y + textLineSpacing*1,1000,30), "API FPS: " + apiFPS.ToString("F2"));
            GUI.Label(new Rect(textPosition.x, textPosition.y + textLineSpacing*2,1000,30), "Position: " + transform.position.ToString("F3"));
            GUI.Label(new Rect(textPosition.x, textPosition.y + textLineSpacing*3,1000,30), "Debug: " + debugText);
        }

        // TODO(jason): temporarily comment out this part, to do is to move this button to someother debug functionality class.
        if (isShowingDebugButton) {
            if (GUI.Button (new Rect (Screen.width - 200, 250, 150, 80), "Toggle Time Plots")) {
                showPlots = !showPlots;
                apiChart.line.enabled = showPlots;
                renderChart.line.enabled = showPlots;
                baselineChart.line.enabled = showPlots;
            }
        }
    }
}
