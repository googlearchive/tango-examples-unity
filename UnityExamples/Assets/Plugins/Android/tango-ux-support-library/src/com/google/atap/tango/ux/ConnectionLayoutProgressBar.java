/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.uxsupportlibrary.R;

import android.animation.Animator;
import android.animation.AnimatorListenerAdapter;
import android.animation.ValueAnimator;
import android.animation.ValueAnimator.AnimatorUpdateListener;
import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Rect;
import android.os.Handler;
import android.util.AttributeSet;
import android.view.View;
import android.view.animation.DecelerateInterpolator;

import java.util.ArrayList;
import java.util.List;

/**
 * Tango connection indeterminate linear progress bar.
 */
class ConnectionLayoutProgressBar extends View {

    private static final long NEW_BAR_RATE_MS = 800;
    private static final long TIME_TO_BAR_REACH_END_MS = 2000;

    private boolean mStarted, mMeasured = false;

    private List<Bar> mBarQueue;

    private int mColorIndex = 0;
    private int[] mColors;

    private Rect mRect;
    private Paint mPaint;
    
    private int mMargin;

    private Handler mHandler = new Handler();
    private Runnable mNewBarRunnable = new Runnable() {
        @Override
        public void run() {
            if (mStarted) {
                addBar();
                mHandler.postDelayed(this, NEW_BAR_RATE_MS);
            }
        }
    };

    public ConnectionLayoutProgressBar(Context context) {
        super(context);
        init();
    }

    public ConnectionLayoutProgressBar(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public ConnectionLayoutProgressBar(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }

    @Override
    protected void onVisibilityChanged(View changedView, int visibility) {
        super.onVisibilityChanged(changedView, visibility);
        if (visibility != View.VISIBLE) {
            reset();
        } else if (mMeasured) {
            start();
        }
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);
        for (int i = 0; i < mBarQueue.size(); i++) {

            mRect.bottom = getHeight();
            mRect.left = mBarQueue.get(i).position;
            if (i < mBarQueue.size() - 1) {
                mRect.right = mBarQueue.get(i + 1).position - mMargin;
            } else {
                mRect.right = getWidth();
            }

            mPaint.setColor(mBarQueue.get(i).color);
            canvas.drawRect(mRect, mPaint);
        }
    }

    private void init() {
        mMargin = (int) getResources().getDimension(R.dimen.connection_progress_bar_sub_margin);
        
        mColorIndex = 0;
        mColors = new int[] {
                getContext().getResources().getColor(R.color.tango_green),
                getContext().getResources().getColor(R.color.tango_blue),
                getContext().getResources().getColor(R.color.tango_yellow)
        };

        mRect = new Rect();
        mPaint = new Paint();
        mPaint.setStyle(Paint.Style.FILL);

        mBarQueue = new ArrayList<Bar>();
    }
    
    @Override
    protected void onLayout(boolean changed, int left, int top, int right, int bottom) {
        super.onLayout(changed, left, top, right, bottom);
        start();
        mMeasured = true;
    }

    private void start() {
        if (!mStarted) {
            mStarted = true;
            addBar();
            mHandler.postDelayed(mNewBarRunnable, NEW_BAR_RATE_MS);
        }
    }

    private void reset() {
        if (mStarted) {
            mColorIndex = 0;
            mHandler.removeCallbacksAndMessages(null);

            for (Bar bar : mBarQueue) {
                bar.cancel();
            }
            mBarQueue.clear();
            mStarted = false;
        }
    }
    
    private void addBar() {
        ValueAnimator animator = ValueAnimator.ofInt(0, getWidth() + mMargin);

        final Bar bar = new Bar(getNextColor(), animator);
        mBarQueue.add(0, bar);

        animator.setStartDelay(NEW_BAR_RATE_MS);
        animator.setDuration(TIME_TO_BAR_REACH_END_MS);
        animator.setInterpolator(new DecelerateInterpolator());
        animator.addUpdateListener(new AnimatorUpdateListener() {
            @Override
            public void onAnimationUpdate(ValueAnimator animation) {
                bar.position = (Integer) animation.getAnimatedValue();
                postInvalidate();
            }
        });
        animator.addListener(new AnimatorListenerAdapter() {
            @Override
            public void onAnimationEnd(Animator animation) {
                mBarQueue.remove(bar);
            }
        });
        animator.start();
    }

    private int getNextColor() {
        mColorIndex = (mColorIndex + 1) % mColors.length;
        return mColors[mColorIndex];
    }

    /**
     * A bar that composes the overall progress bar.
     */
    private class Bar {
        int position;
        int color;
        ValueAnimator animator;

        public Bar(int color, ValueAnimator animator) {
            this.color = color;
            this.animator = animator;
        }

        public void cancel() {
            if (animator != null) {
                animator.removeAllListeners();
                animator.cancel();
                animator = null;
            }
        }
    }
}
