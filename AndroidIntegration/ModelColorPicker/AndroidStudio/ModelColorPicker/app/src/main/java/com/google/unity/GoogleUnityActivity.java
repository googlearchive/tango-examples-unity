/*
 * Copyright 2016 Google Inc. All Rights Reserved.
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
package com.google.unity;

import android.app.Activity;
import android.app.NativeActivity;
import android.content.Intent;
import android.content.res.Configuration;
import android.graphics.Color;
import android.graphics.PixelFormat;
import android.hardware.display.DisplayManager;
import android.os.Bundle;
import android.util.Log;
import android.view.KeyEvent;
import android.view.LayoutInflater;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.widget.AdapterView;
import android.widget.Spinner;

import com.google.tango.modelcolorpicker.R;
import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;
import java.util.List;

/**
 * Custom Unity Activity that passes through Android lifecycle events from Unity appropriately.
 */
public class GoogleUnityActivity extends NativeActivity implements AdapterView.OnItemSelectedListener {
    private static final String TAG = GoogleUnityActivity.class.getSimpleName();

    /**
     * Callbacks for common Android lifecycle events.
     */
    public interface AndroidLifecycleListener {
        public void onPause();

        public void onResume();

        public void onActivityResult(int requestCode, int resultCode, Intent data);

        public void onDisplayChanged();
    }

    // don't change the name of this variable; referenced from native code
    protected UnityPlayer mUnityPlayer;

    protected AndroidLifecycleListener mAndroidLifecycleListener;

    private Spinner mColorSpinner;
    private List<String> mColorList = makeColorList();

    // Setup activity layout
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        getWindow().takeSurface(null);
        setTheme(android.R.style.Theme_NoTitleBar_Fullscreen);
        getWindow().setFormat(PixelFormat.RGB_565);

        DisplayManager displayManager = (DisplayManager) getSystemService(DISPLAY_SERVICE);
        if (displayManager != null) {
            displayManager.registerDisplayListener(new DisplayManager.DisplayListener() {
                @Override
                public void onDisplayAdded(int displayId) {}

                @Override
                public void onDisplayChanged(int displayId) {
                    synchronized (this) {
                        if (mAndroidLifecycleListener != null) {
                            mAndroidLifecycleListener.onDisplayChanged();
                        }
                    }
                }

                @Override
                public void onDisplayRemoved(int displayId) {}
            }, null);
        }

        mUnityPlayer = new UnityPlayer(this);
        if (mUnityPlayer.getSettings().getBoolean("hide_status_bar", true)) {
            getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN,
                    WindowManager.LayoutParams.FLAG_FULLSCREEN);
        }

        ((ViewGroup) findViewById(R.id.unity_view)).addView(mUnityPlayer.getView(), 0);
        mUnityPlayer.requestFocus();

        // Initialize color spinner
        mColorSpinner = (Spinner)findViewById(R.id.color_spinner);
        mColorSpinner.setAdapter(new ColorSpinnerAdapter(this, mColorList));
        mColorSpinner.setOnItemSelectedListener(this);
    }

    /**
     * Create a list of colors through the HUE spectrum.
     */
    private static List<String> makeColorList() {
        List<String> colorNames = new ArrayList<>();
        float[] hsv = new float[3];
        hsv[1] = hsv[2] = 0.85f;
        for (int i = 0; i < 360; i += 9) {
            hsv[0] = i;
            int color = Color.HSVToColor(hsv);
            colorNames.add("#" + Integer.toHexString(color).substring(2));
        }
        return colorNames;
    }

    /**
     * Click listener from the color spinner.
     */
    @Override
    public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
        // Send a message to Unity with the chosen color in RGB hex format
        UnityPlayer.UnitySendMessage("Color Controller", "ChangeModelColor", mColorList.get(i));
    }

    @Override
    public void onNothingSelected(AdapterView<?> adapterView) {}

    public void showAndroidViewLayer(final int layoutResId) {
        final Activity self = this;
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                ViewGroup androidViewContainer =
                        (ViewGroup) findViewById(R.id.android_view_container);
                androidViewContainer.removeAllViews();

                // Make it possible for the developer to specify their own layout.
                LayoutInflater.from(self).inflate(layoutResId, androidViewContainer);
            }
        });
    }

    public View getAndroidViewLayer() {
        return findViewById(R.id.android_view_container);
    }

    public void launchIntent(String packageName, String className, String[] args, int requestcode) {
        Intent intent = new Intent();
        intent.setClassName(packageName, className);
        if (args != null) {
            for (int i = 0; i < args.length; i++) {
                String[] keyVal = args[i].split(":");
                if (keyVal.length == 2) {
                    intent.putExtra(keyVal[0], keyVal[1]);
                }
            }
        }
        startActivityForResult(intent, requestcode);
    }

    // CHECKSTYLE:OFF
    public void LaunchIntent(String packageName, String className, String[] args, int requestcode) {
    // CHECKSTYLE:ON
        launchIntent(packageName, className, args, requestcode);
    }

    public void attachLifecycleListener(AndroidLifecycleListener listener) {
        mAndroidLifecycleListener = listener;
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (mAndroidLifecycleListener != null) {
            mAndroidLifecycleListener.onActivityResult(requestCode, resultCode, data);
        }
    }

    // Quit Unity
    @Override
    protected void onDestroy() {
        mUnityPlayer.quit();
        super.onDestroy();
    }

    // Pause Unity
    @Override
    protected void onPause() {
        super.onPause();
        if (mAndroidLifecycleListener != null) {
            mAndroidLifecycleListener.onPause();
        }
        mUnityPlayer.pause();
    }

    // Resume Unity
    @Override
    protected void onResume() {
        super.onResume();
        if (mAndroidLifecycleListener != null) {
            mAndroidLifecycleListener.onResume();
        }
        mUnityPlayer.resume();
    }

    public void logAndroidErrorMessage(String message) {
        Log.e(this.getPackageName(), message);
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        mUnityPlayer.configurationChanged(newConfig);
    }

    // Notify Unity of the focus change.
    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        mUnityPlayer.windowFocusChanged(hasFocus);
    }

    // For some reason the multiple keyevent type is not supported by the ndk.
    // Force event injection by overriding dispatchKeyEvent().
    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (event.getAction() == KeyEvent.ACTION_MULTIPLE) {
            return mUnityPlayer.injectEvent(event);
        }
        return super.dispatchKeyEvent(event);
    }

    // Pass any events not handled by (unfocused) views straight to UnityPlayer
    @Override
    public boolean onKeyUp(int keyCode, KeyEvent event) {

        return mUnityPlayer.injectEvent(event);
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        return mUnityPlayer.injectEvent(event);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        return mUnityPlayer.injectEvent(event);
    }

    /* API12 */
    @Override
    public boolean onGenericMotionEvent(MotionEvent event) {
        return mUnityPlayer.injectEvent(event);
    }
}
