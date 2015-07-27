/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.uxsupportlibrary.R;

import android.content.Context;
import android.util.AttributeSet;
import android.view.View;
import android.widget.FrameLayout;

/**
 * Displays the Tango splash screen.
 */
public class SplashLayout extends FrameLayout {

    public SplashLayout(Context context) {
        super(context);
        init();
    }

    public SplashLayout(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public SplashLayout(Context context, AttributeSet attrs, int defStyle) {
        super(context, attrs, defStyle);
        init();
    }
    
    private void init() {
        View.inflate(getContext(), R.layout.layout_splash, this);
    }
}
