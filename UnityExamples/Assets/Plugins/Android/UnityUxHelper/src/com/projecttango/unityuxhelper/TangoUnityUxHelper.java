
package com.projecttango.unityuxhelper;

import com.google.atap.tango.ux.TangoUx;
import com.google.atap.tango.ux.TangoUxLayout;
import com.google.atap.tango.ux.UxExceptionEventListener;
import com.google.atap.tangoservice.TangoEvent;
import com.google.unity.GoogleUnityActivity;

import android.util.Log;
import android.view.View;

/**
 * Makes it easier to work with the Tango UX Support Library from within Unity.
 */
public class TangoUnityUxHelper {
    private static final String TAG = "TangoUnityUxHelper";
    private GoogleUnityActivity mParent;
    private volatile TangoUx mTangoUx;
    private volatile TangoUxLayout mTangoUxLayout;

    public TangoUnityUxHelper(GoogleUnityActivity googleUnityActivity) {
        mParent = googleUnityActivity;
    }

    public void initTangoUx(final boolean isMotionTrackingEnabled) {
        mParent.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                View androidOverlayView = mParent.getAndroidViewLayer();
                if (androidOverlayView == null) {
                    mParent.showAndroidViewLayer(R.layout.tango_ux_exceptions);
                    androidOverlayView = mParent.getAndroidViewLayer();
                }
                mTangoUxLayout = (TangoUxLayout) androidOverlayView
                        .findViewById(R.id.layout_exceptions);
                if (mTangoUxLayout != null) {
                    TangoUx.Builder builder = new TangoUx.Builder(mParent)
                            .setTangoUxLayout(mTangoUxLayout);
                    if (!isMotionTrackingEnabled) {
                        builder.disableMotionTracking();
                    }
                    mTangoUx = builder.build();
                } else {
                    Log.e(TAG, "Error initializing TangoUx");
                }
            }
        });
    }

    public void showDefaultExceptionsUi(final boolean shouldUseDefaultUi) {
        mParent.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (mTangoUxLayout != null) {
                    mTangoUxLayout.getUiSettings().setExceptionsEnabled(shouldUseDefaultUi);
                } else {
                    Log.e(TAG, "Error showing default exceptions Ui.");
                }

            }
        });
    }

    public void processXyzIjPointCount(int pointCount) {
        if (mTangoUx == null) {
            return;
        }
        mTangoUx.updateXyzCount(pointCount);
    }

    public void processPoseDataStatus(int statusCode) {
        if (mTangoUx == null) {
            return;
        }
        mTangoUx.updatePoseStatus(statusCode);
    }

    public void processTangoEvent(double timestamp, int eventType, String key, String value) {
        if (mTangoUx == null) {
            return;
        }
        final TangoEvent event = new TangoEvent();
        event.timestamp = timestamp;
        event.eventType = eventType;
        event.eventKey = key;
        event.eventValue = value;
        mTangoUx.onTangoEvent(event);
    }

    public void setUxExceptionEventListener(final UxExceptionEventListener exceptionEventListener) {
        mParent.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (mTangoUx != null) {
                    mTangoUx.setUxExceptionEventListener(exceptionEventListener);
                } else {
                    Log.e(TAG, "Error setting UxExceptionEventListener.");
                }
            }
        });
    }

    public void start() {
        mParent.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (mTangoUx != null) {
                    mTangoUx.start();
                } else {
                    Log.e(TAG, "Error starting TangoUx.");
                }
            }
        });
    }

    public void stop() {
        mParent.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (mTangoUx != null) {
                    mTangoUx.stop();
                }
                else {
                    Log.e(TAG, "Error stopping TangoUx.");
                }
            }
        });
    }
}
