/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import android.animation.Animator;
import android.animation.AnimatorListenerAdapter;
import android.animation.ArgbEvaluator;
import android.animation.ValueAnimator;
import android.animation.ValueAnimator.AnimatorUpdateListener;
import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Paint.Cap;
import android.graphics.Paint.Style;
import android.graphics.Rect;
import android.util.AttributeSet;
import android.view.View;

/**
 * This will visually represent if an exception was corrected or is still
 * active.
 */
class ExceptionStatusComponent extends View {

    private Paint mPaint;
    private Rect mRect;
    private float mFactor;
    private int mColor = 0xFFCC0E15;

    public ExceptionStatusComponent(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }

    public ExceptionStatusComponent(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public ExceptionStatusComponent(Context context) {
        super(context);
        init();
    }

    private void init() {
        mPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        mPaint.setColor(mColor);
        mPaint.setStyle(Style.STROKE);
        mPaint.setStrokeWidth(2);
        mPaint.setStrokeCap(Cap.ROUND);
    }

    public void setSrcColor(int color) {
        this.mColor = color;
        mPaint.setColor(mColor);
        invalidate();
    }

    public void setResolved(final ExceptionStatusComponentListener listener) {

        mFactor = 0;
        ValueAnimator animator = ValueAnimator.ofFloat(0, 1);
        final ArgbEvaluator argEvaluator = new ArgbEvaluator();
        animator.addUpdateListener(new AnimatorUpdateListener() {

            @Override
            public void onAnimationUpdate(ValueAnimator animation) {
                mFactor = (Float) animation.getAnimatedValue();
                mPaint.setColor((Integer) argEvaluator.evaluate(animation.getAnimatedFraction(),
                        mColor, 0xFF508C68));
                invalidate();
            }
        });
        if (listener != null) {
            animator.addListener(new AnimatorListenerAdapter() {
                public void onAnimationEnd(Animator animation) {
                    listener.onExceptionStatusAnimationCompleted();
                };
            });
        }
        animator.setDuration(1000);
        animator.start();
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);

        if (mRect == null) {
            mRect = new Rect(0, 0, getWidth(), getHeight());
        }

        canvas.drawCircle(getWidth() / 2.0f, getHeight() / 2.0f, getWidth() / 2.5f, mPaint);

        if (mFactor > 0) {
            canvas.save();
            canvas.clipRect(0, 0, getWidth() * mFactor, getHeight());
            canvas.drawLine(getWidth() / 4.4f, getHeight() / 1.9f, getWidth() / 2.3f,
                    getHeight() / 1.4f, mPaint);
            canvas.drawLine(getWidth() / 2.3f, getHeight() / 1.4f, getWidth() / 1.4f,
                    getHeight() / 2.7f, mPaint);
            canvas.restore();
        }
    }

    /**
     * This Exception Status interface reports the caller when the status change
     * animation was completed.
     */
    public interface ExceptionStatusComponentListener {
        void onExceptionStatusAnimationCompleted();
    }

}
