/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.uxsupportlibrary.R;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.util.AttributeSet;
import android.view.View;

/**
 * Animated tick component for Connection layout.
 */
class ConnectionLayoutTick extends View {

    private Bitmap mBitmapTick;
    private float mClipValue = 0f;
    
    public ConnectionLayoutTick(Context context) {
        super(context);
        init();
    }

    public ConnectionLayoutTick(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }
    
    public ConnectionLayoutTick(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }
    
    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);
        canvas.clipRect(0f, 0f, mClipValue, canvas.getHeight());
        canvas.drawBitmap(mBitmapTick, 0, 0, null);
    }
    
    public void setClipValue(float animatedValue) {
        mClipValue = animatedValue;
        invalidate();
    }
    
    private void init() {
        mBitmapTick = BitmapFactory.decodeResource(getResources(),
                R.drawable.ic_shake_ok_tick);
    }
}
