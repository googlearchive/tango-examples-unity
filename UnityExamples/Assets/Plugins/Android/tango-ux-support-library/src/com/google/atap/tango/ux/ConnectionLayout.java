/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.uxsupportlibrary.R;

import android.animation.ValueAnimator;
import android.animation.ValueAnimator.AnimatorUpdateListener;
import android.content.Context;
import android.util.AttributeSet;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.FrameLayout;

/**
 * Connection screen layout component. Displays an indeterminate progress bar indicating an ongoing
 * connection and animates the layout when shake is detected.
 */
class ConnectionLayout extends FrameLayout {

    private static final long LAYOUT_FADE_DURATION = 500;
    private static final long OK_ANIMATION_DURATION = 250;

    private static final long START_ANGLE = 15;
    private static final long STEP_DURATION = 100;
    private static final float NUMBER_OF_STEPS = 6f;
    private static final float ANGLE_DELTA = START_ANGLE / NUMBER_OF_STEPS;

    private boolean mIsShowing;

    private float mShakeAngle = START_ANGLE;
    private boolean mIsShaking;

    private View mImageGroup;
    private View mImageErr;
    private ConnectionLayoutTick mImageOkTick;

    private ValueAnimator mIconOkTickClipAnimator;

    protected void onShakeDetected() {
        if (!mIsShowing) {
            return;
        }
        mShakeAngle = START_ANGLE;
        if (!mIsShaking) {
            mIsShaking = true;
            resetShakeLayout();
            triggerShakeAnimation();
        }
    }

    public ConnectionLayout(Context context) {
        super(context);
        init(context);
    }

    public ConnectionLayout(Context context, AttributeSet attrs) {
        super(context, attrs);
        init(context);
    }

    public ConnectionLayout(Context context, AttributeSet attrs, int defStyle) {
        super(context, attrs, defStyle);
        init(context);
    }

    protected boolean isShowing() {
        return mIsShowing;
    }

    protected void show() {
        if (mIsShowing) {
            return;
        }

        resetShakeLayout();
        mIsShowing = true;
        setVisibility(VISIBLE);
    }

    protected void hide(boolean animate) {
        if (!mIsShowing) {
            return;
        }

        mIsShowing = mIsShaking = false;

        if (animate) {
            triggerFadeOutAnimation();
        } else {
            onHideEnded();
        }
    }

    private void init(Context context) {
        LayoutInflater inflater = (LayoutInflater) context
                .getSystemService(Context.LAYOUT_INFLATER_SERVICE);
        View rootView = inflater.inflate(R.layout.layout_connection, this, true);
        mImageGroup = rootView.findViewById(R.id.group_shake_icon);
        mImageErr = rootView.findViewById(R.id.image_shake_err);
        mImageOkTick = (ConnectionLayoutTick) rootView.findViewById(R.id.image_shake_ok_tick);
        mIsShowing = View.VISIBLE == getVisibility();
        hide(false);
    }

    private void resetShakeLayout() {
        mImageErr.animate().cancel();
        if (mIconOkTickClipAnimator != null) {
            mIconOkTickClipAnimator.removeAllListeners();
            mIconOkTickClipAnimator.cancel();
            mIconOkTickClipAnimator = null;
        }

        mImageGroup.setRotation(0f);
        mShakeAngle = START_ANGLE;
        mImageErr.setAlpha(1f);
        mImageOkTick.setClipValue(0f);
    }

    private void onHideEnded() {
        setVisibility(GONE);
        setAlpha(1f);
        resetShakeLayout();
    }

    private void triggerShakeAnimation() {
        if (mShakeAngle > 0f) {
            mShakeAngle -= ANGLE_DELTA;
            mImageGroup.animate()
                    .rotation(mShakeAngle)
                    .setDuration(STEP_DURATION)
                    .withEndAction(new Runnable() {
                        @Override
                        public void run() {
                            mImageGroup.animate()
                                    .rotation(-mShakeAngle)
                                    .setDuration(STEP_DURATION)
                                    .withEndAction(new Runnable() {
                                        @Override
                                        public void run() {
                                            triggerShakeAnimation();
                                        }
                                    });
                        }
                    });
        } else {
            mIsShaking = false;
            triggerOkAnimation();
        }
    }

    private void triggerOkAnimation() {
        mImageErr.animate()
                .alpha(0f)
                .setDuration(OK_ANIMATION_DURATION);

        mIconOkTickClipAnimator = ValueAnimator.ofFloat(0f, mImageOkTick.getWidth());
        mIconOkTickClipAnimator.setDuration(OK_ANIMATION_DURATION);
        mIconOkTickClipAnimator.addUpdateListener(new AnimatorUpdateListener() {
            @Override
            public void onAnimationUpdate(ValueAnimator animation) {
                mImageOkTick.setClipValue((Float) animation.getAnimatedValue());
            }
        });
        mIconOkTickClipAnimator.start();
    }

    private void triggerFadeOutAnimation() {
        this.animate()
                .alpha(0f)
                .setDuration(LAYOUT_FADE_DURATION)
                .withEndAction(new Runnable() {
                    @Override
                    public void run() {
                        onHideEnded();
                    }
                });
    }
}
