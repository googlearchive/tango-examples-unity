/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.ux.UiSettings.UiSettingsListener;
import com.google.atap.tango.uxsupportlibrary.R;
import com.google.atap.tangoservice.TangoPoseData;

import android.content.Context;
import android.util.AttributeSet;
import android.util.Log;
import android.view.View;
import android.widget.FrameLayout;

/**
 * Layout that implements the management of exception notifications UI and the
 * connection screen. Automatically shows or dismisses UI notifications with information 
 * on problems that are currently happening and how to fix them.
 */
public class TangoUxLayout extends FrameLayout {

    private static final String TAG = "TangoUxLayout";

    private UiSettings mSettings;

    private ExceptionPanelContainer mExceptionContainer;
    private ConnectionLayout mConnectionLayout;
    private TangoUx mTangoUx;

    public TangoUxLayout(Context context) {
        super(context);
        init(context);
    }

    public TangoUxLayout(Context context, AttributeSet attrs) {
        super(context, attrs);
        init(context);
    }

    public TangoUxLayout(Context context, AttributeSet attrs, int defStyle) {
        super(context, attrs, defStyle);
        init(context);
    }

    public UiSettings getUiSettings() {
        return mSettings;
    }

    private void init(Context context) {
        View.inflate(getContext(), R.layout.layout_tango_ux, this);
        mSettings = new UiSettings(mUiSettingsListener);
        mExceptionContainer = (ExceptionPanelContainer) findViewById(R.id.exception_container);
        mConnectionLayout = (ConnectionLayout) findViewById(R.id.connection_layout);
    }

    @Override
    protected void onDetachedFromWindow() {
        super.onDetachedFromWindow();
        mTangoUx = null;
    }
    
    void setTangoUx(TangoUx tangoUx) {
        mTangoUx = tangoUx;
    }

    void reset() {
        hideExceptionsLayout();
        showConnectionLayout();
    }

    void onPoseAvailable(int statusCode) {
        if (mConnectionLayout.isShowing() && statusCode == TangoPoseData.POSE_VALID) {
            mConnectionLayout.hide(true);
        }
    }

    /**
     * Hide exceptions layout and remove current showing exceptions.
     */
    void hideExceptionsLayout() {
        mExceptionContainer.dismiss();
    }

    /**
     * Hide connection layout. 
     */
    void hideConnectionLayout() {
        mConnectionLayout.hide(false);
    }

    /**
     * Shows the connection layout; it will self-dismiss on the next valid pose.
     */
    void showConnectionLayout() {
        if (mTangoUx == null) {
            Log.w(TAG, "TangoUx null when showing connection layout.");
        } else if (mSettings.isConnectionLayoutEnabled() && mTangoUx.mIsMotionTrackingEnabled) {
            mConnectionLayout.show();
        }
    }

    private UiSettingsListener mUiSettingsListener = new UiSettingsListener() {

        @Override
        public void onExceptionsEnabled() {
            mExceptionContainer.enableUi(true);
        }

        @Override
        public void onExceptionsDisabled() {
            mExceptionContainer.enableUi(false);
        }

        @Override
        public void onConnectionLayoutEnabled() {
        }

        @Override
        public void onConnectionLayoutDisabled() {
            mConnectionLayout.hide(false);
        }
    };

    void onException(TangoExceptionInfo exception) {
        mExceptionContainer.onException(exception);
    }

    void hideExceptions(TangoExceptionInfo[] exceptions) {
        mExceptionContainer.hideExceptions(exceptions);
    }

    TangoExceptionInfo getHighestPriorityException() {
        if (mTangoUx != null) {
            return mTangoUx.getHighestPriorityException();
        } else {
            return null;
        }
    }

    void onShakeDetected() {
        mConnectionLayout.onShakeDetected();
    }
}
