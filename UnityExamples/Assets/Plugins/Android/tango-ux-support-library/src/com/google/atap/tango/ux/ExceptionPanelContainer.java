/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.ux.ExceptionHelper.ExceptionHelperListener;
import com.google.atap.tango.ux.ExceptionStatusComponent.ExceptionStatusComponentListener;

import android.content.Context;
import android.util.AttributeSet;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewTreeObserver.OnGlobalLayoutListener;
import android.widget.LinearLayout;

/**
 * Exception panel component.
 */
class ExceptionPanelContainer extends LinearLayout implements ExceptionHelperListener {

    private static final String TAG = "ExceptionPanelContainer";

    private boolean mIsShowing = false;

    private TangoExceptionInfo mInfo;

    private boolean mIsUiEnabled = true;

    private TangoUxLayout mTangoUxLayout;

    public ExceptionPanelContainer(Context context) {
        super(context);
        init(context);
    }

    public ExceptionPanelContainer(Context context, AttributeSet attrs) {
        super(context, attrs);
        init(context);
    }

    public ExceptionPanelContainer(Context context, AttributeSet attrs, int defStyle) {
        super(context, attrs, defStyle);
        init(context);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        if (mInfo.mGroup != TangoExceptionInfo.GROUP_SYSTEM 
                && event.getAction() == MotionEvent.ACTION_UP) {
            dismiss();
        }
        return true;
    }
    
    @Override
    protected void onAttachedToWindow() {
        super.onAttachedToWindow();
        try {
            mTangoUxLayout = (TangoUxLayout) getParent().getParent();
        } catch (ClassCastException e) {
            throw new ClassCastException(getParent().getParent().toString()
                    + " must implement " + TangoUxLayout.class.getName());
        }
    }
    
    @Override
    protected void onDetachedFromWindow() {
        super.onDetachedFromWindow();
        mTangoUxLayout = null;
    }

    private void init(Context context) {
        this.setVisibility(View.GONE);
        this.getViewTreeObserver().addOnGlobalLayoutListener(
                new OnGlobalLayoutListener() {
                    @Override
                    public void onGlobalLayout() {
                        ExceptionPanelContainer.this.getViewTreeObserver()
                                .removeOnGlobalLayoutListener(this);
                        if (mIsUiEnabled && mTangoUxLayout != null) {
                            TangoExceptionInfo nextException = mTangoUxLayout
                                    .getHighestPriorityException();
                            if (nextException != null) {
                                Log.d(TAG, "Init with Exception " + nextException.mType
                                        + " Found in Queue");
                                addException(nextException);
                            }
                        }
                    }

                });

    }

    private void addException(TangoExceptionInfo info) {

        info.loadExceptionData(getResources());

        if (getChildCount() == 0) {
            show();

            ExceptionComponent exception = new ExceptionComponent(getContext());
            exception.setTag(info.mType);
            exception.setData(info);

            Log.d(TAG, "Adding Exception " + info.mType);
            mInfo = info;
            addView(exception);
        } else if (info.hasHigherPriorityThan(mInfo)) {
            Log.d(TAG, "Exception " + info.mType + " received replacing the current one");
            removeAllViews();
            addException(info);
        } else {
            Log.d(TAG, "Cannot add Exception " + info.mType + " there is already one");
        }

    }

    private void removeExceptions(final TangoExceptionInfo[] exceptions) {
        for (int i = 0; i < exceptions.length; i++) {
            TangoExceptionInfo info = exceptions[i];
            final ExceptionComponent view = (ExceptionComponent) findViewWithTag(info.mType);
            if (view != null) {
                view.setResolved(new ExceptionStatusComponentListener() {

                    @Override
                    public void onExceptionStatusAnimationCompleted() {
                        if (mIsUiEnabled && mTangoUxLayout != null) {
                            TangoExceptionInfo nextException = mTangoUxLayout
                                    .getHighestPriorityException();
                            if (nextException != null) {
                                Log.d(TAG, "Exception " + nextException.mType
                                        + " Found in Queue");
                                removeView(view);
                                addException(nextException);
                            } else {
                                Log.d(TAG, "No more exceptions found on queue, dismissing");
                                dismiss();
                            }
                        }
                    }
                });
                break;
            }
        }
    }

    protected void dismiss() {
        this.setPivotY(0);
        this.animate().scaleY(0.8f).yBy(-100).alpha(0).withEndAction(
                new Runnable() {
                    @Override
                    public void run() {
                        removeAllViews();
                        ExceptionPanelContainer.this.setVisibility(View.GONE);
                        mIsShowing = false;
                    }
                });
    }

    private void show() {
        if (mIsShowing) {
            return;
        }

        mIsShowing = true;

        this.animate().cancel();

        this.setAlpha(0);
        this.setY(-100);
        this.setScaleY(1);
        this.setVisibility(View.VISIBLE);

        this.animate().alpha(1).y(0);
    }

    protected void enableUi(boolean enabled) {
        mIsUiEnabled = enabled;
        if (!enabled) {
            dismiss();
        }
    }

    @Override
    public void onException(final TangoExceptionInfo exception) {
        if (mIsUiEnabled) {
            Log.d(TAG, "Exception " + exception.mType + " received");
            addException(exception);
        }
        if (mTangoUxLayout != null && exception.mGroup == TangoExceptionInfo.GROUP_SYSTEM) {
            mTangoUxLayout.hideConnectionLayout();
        }
    }

    @Override
    public void hideExceptions(final TangoExceptionInfo[] exceptions) {
        removeExceptions(exceptions);
    }
}
