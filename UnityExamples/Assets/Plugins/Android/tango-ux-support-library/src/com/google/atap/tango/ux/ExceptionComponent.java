/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.ux.ExceptionStatusComponent.ExceptionStatusComponentListener;
import com.google.atap.tango.uxsupportlibrary.R;

import android.content.Context;
import android.util.AttributeSet;
import android.view.View;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.TextView;

/**
 * View that represents an Exception description and title.
 */
class ExceptionComponent extends FrameLayout {

    private ImageView mIcon;
    private TextView mTitle;
    private TextView mDescription;
    private TangoExceptionInfo mInfo;
    private ExceptionStatusComponent mStatus;

    public ExceptionComponent(Context context) {
        super(context);
    }

    public ExceptionComponent(Context context, AttributeSet attrs) {
        super(context, attrs);
    }

    public ExceptionComponent(Context context, AttributeSet attrs, int defStyle) {
        super(context, attrs, defStyle);
    }

    private void setupLayout() {

        View.inflate(getContext(), R.layout.exception_component, this);
        mIcon = (ImageView) findViewById(R.id.exception_icon);
        mTitle = (TextView) findViewById(R.id.exception_title);
        mDescription = (TextView) findViewById(R.id.exception_description);
        mStatus = (ExceptionStatusComponent) findViewById(R.id.exception_status);

        if (mInfo != null) {
            mTitle.setText(mInfo.mTitle);
            mDescription.setText(mInfo.mDescription);
            mIcon.setImageResource(mInfo.mIcon);
        }
    }

    @Override
    protected void onAttachedToWindow() {
        setupLayout();
        super.onAttachedToWindow();
    }

    public TangoExceptionInfo getExceptionInfo() {
        return mInfo;
    }

    public void setData(TangoExceptionInfo info) {
        mInfo = info;

        if (mTitle != null) {
            mTitle.setText(mInfo.mTitle);
        }
        if (mDescription != null) {
            mDescription.setText(mInfo.mDescription);
        }
        if (mIcon != null) {
            mIcon.setBackgroundResource(mInfo.mIcon);
        }
    }

    public void setResolved(ExceptionStatusComponentListener listener) {
        if (mStatus != null) {
            mStatus.setResolved(listener);
        } else {
            listener.onExceptionStatusAnimationCompleted();
        }
    }
}
