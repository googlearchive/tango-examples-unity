/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.ux.ExceptionHelper.ExceptionHelperListener;
import com.google.atap.tango.ux.MotionDetectionHelper.MotionDetectionListener;
import com.google.atap.tangoservice.TangoEvent;

import android.content.Context;
import android.os.Handler;
import android.os.Looper;

import java.lang.ref.WeakReference;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * This helper class manages exceptions and provides a simple way to display notifications as well
 * as other UI patterns, such as a connection screen. Should be notified of all TangoEvents,
 * pose status and number of points in depth_data_buffer.
 */
public class TangoUx {

    private ExceptionHelper mExceptionHelper;
    private UxExceptionEventListener mUxExceptionEventListener;
    // UI Thread Handler.
    private Handler mHandler = new Handler(Looper.getMainLooper());
    // Detect Shake.
    private MotionDetectionHelper mMotionDetector;

    private WeakReference<TangoUxLayout> mWeakTangoUxLayout;
    private WeakReference<Context> mWeakContext;
    
    private AtomicBoolean mIsRunning = new AtomicBoolean(false);
    
    // Lock used to make start and stop synchronized.
    private Object mLock = new Object();
    
    final boolean mIsMotionTrackingEnabled;
    
    /**
     * Creates a new TangoUx object. 
     *
     * @param context
     * @param motionTrackingEnabled whether Tango motion tracking is enabled.
     */
    private TangoUx(Context context, boolean motionTrackingEnabled, TangoUxLayout tangoUxLayout) {
        mWeakContext = new WeakReference<Context>(context);
        mIsMotionTrackingEnabled = motionTrackingEnabled;
        if (tangoUxLayout != null) {
            mWeakTangoUxLayout = new WeakReference<TangoUxLayout>(tangoUxLayout);
            tangoUxLayout.setTangoUx(this);
        }
    }

    /**
     * Notify this helper on the status of the pose.
     * 
     * @param statusCode status of the pose.
     */
    public void updatePoseStatus(final int statusCode) {
        if (!mIsMotionTrackingEnabled) {
            throw new IllegalStateException("Motion Tracking is disabled.");
        }
        
        if (!mIsRunning.get()) {
            return;
        }
        
        if (mExceptionHelper != null) {
            mExceptionHelper.onPoseStatusAvailable(statusCode);
        }

        mHandler.post(new Runnable() {
            @Override
            public void run() {
                if (mWeakTangoUxLayout != null && mWeakTangoUxLayout.get() != null) {
                    mWeakTangoUxLayout.get().onPoseAvailable(statusCode);
                }
            }
        });
    }
    
    /**
     * Notify this helper on the number of points in depth_data_buffer.
     * 
     * @param xyzCount number of points in depth_data_buffer.
     */
    public void updateXyzCount(int xyzCount) {
        if (!mIsRunning.get()) {
            return;
        }

        if (mExceptionHelper != null) {
            mExceptionHelper.onXyzCountAvailable(xyzCount);
        }
    }
        
        
    /**
     * Notify this helper that a TangoEvent has been received.
     * 
     * @param event
     */
    public void onTangoEvent(TangoEvent event) {
        if (!mIsRunning.get()) {
            return;
        }

        if (mExceptionHelper != null) {
            mExceptionHelper.onTangoEvent(event);
        }
    }

    /**
     * Notify this helper that Tango version is out of date.
     * 
     */
    public void onTangoOutOfDate() {
        if (!mIsRunning.get()) {
            return;
        }

        if (mExceptionHelper != null) {
            mExceptionHelper.onTangoOutOfDate();
        }
    }

    /**
     * Register a callback to be invoked when a exception should be raised or dismissed. If not
     * null, the custom UI notifications won't show and this callback will be invoked.
     * 
     * @param uxExceptionListener the UxExceptionListener listener.
     */
    public void setUxExceptionEventListener(UxExceptionEventListener uxExceptionListener) {
        mUxExceptionEventListener = uxExceptionListener;
    }

    /**
     * Start TangoUX. Shows the connection screen if {@link UiSettings#isConnectionLayoutEnabled()}
     * is true, and starts notifying exceptions.
     */
    public void start() {
        synchronized (mLock) {
            if (mIsRunning.get()) {
                return;
            }
            mIsRunning.set(true);
            mExceptionHelper =
                    new ExceptionHelper(mExceptionListener, mIsMotionTrackingEnabled);
            mExceptionHelper.checkForIncompatibleVM();
            mHandler.post(new Runnable() {
                @Override
                public void run() {
                    if (mWeakTangoUxLayout != null && mWeakTangoUxLayout.get() != null) {
                        mWeakTangoUxLayout.get().reset();
                    }
                    // Initialize shake detector
                    if (mMotionDetector == null && mWeakContext != null
                            && mWeakContext.get() != null) {
                        mMotionDetector = new MotionDetectionHelper(mWeakContext.get());
                    }
                    if (mMotionDetector != null) {
                        mMotionDetector.start(mMotionDetectionListener);
                    }
                }
            });
        }
    }

    /**
     * Stop TangoUx. Should always be called when don't need to handle exceptions, especially when
     * Tango is disconnected.
     */
    public void stop() {
        synchronized (mLock) {
            mIsRunning.set(false);
            mHandler.removeCallbacksAndMessages(null);
            if (mExceptionHelper != null) {
                mExceptionHelper.stop();
            }
            mExceptionHelper = null;
            mHandler.post(new Runnable() {
                @Override
                public void run() {
                    if (mMotionDetector != null) {
                        mMotionDetector.stop();
                    }
                    if (mWeakTangoUxLayout != null && mWeakTangoUxLayout.get() != null) {
                        mWeakTangoUxLayout.get().hideExceptionsLayout();
                        mWeakTangoUxLayout.get().hideConnectionLayout();
                    }
                }
            });
        }
    }

    TangoExceptionInfo getHighestPriorityException() {
        if (mExceptionHelper != null) {
            return mExceptionHelper.getHighestPriorityException();
        }
        return null;
    }

    /**
     * Shake detector listener used to detect shake.
     */
    private MotionDetectionListener mMotionDetectionListener = new MotionDetectionListener() {

        @Override
        public void onShaking() {

            if (!mIsRunning.get()) {
                return;
            }

            if (mExceptionHelper != null) {
                mExceptionHelper.onShakeDetected();
            }
            if (mWeakTangoUxLayout != null && mWeakTangoUxLayout.get() != null) {
                mWeakTangoUxLayout.get().onShakeDetected();
            }
        }

        @Override
        public void onLyingOnSurface() {

            if (!mIsRunning.get()) {
                return;
            }

            if (mExceptionHelper != null) {
                mExceptionHelper.onLyingOnSurfaceDetected();
            }
        }
    };
    
    private ExceptionHelperListener mExceptionListener = new ExceptionHelperListener() {

        @Override
        public void onException(final TangoExceptionInfo exception) {
            if (!mIsRunning.get()) {
                return;
            }

            mHandler.post(new Runnable() {

                @Override
                public void run() {

                    if (mWeakTangoUxLayout != null && mWeakTangoUxLayout.get() != null) {
                        mWeakTangoUxLayout.get().onException(exception);
                    }

                    if (mUxExceptionEventListener != null) {
                        UxExceptionEvent exceptionEvent = new UxExceptionEvent();
                        exceptionEvent.mType = exception.mType;
                        exceptionEvent.mValue = exception.mValue;
                        exceptionEvent.mStatus = UxExceptionEvent.STATUS_DETECTED;
                        mUxExceptionEventListener.onUxExceptionEvent(exceptionEvent);
                    }
                }
            });
        }

        @Override
        public void hideExceptions(final TangoExceptionInfo[] exceptions) {
            if (!mIsRunning.get()) {
                return;
            }

            mHandler.post(new Runnable() {
                @Override
                public void run() {

                    if (mWeakTangoUxLayout != null && mWeakTangoUxLayout.get() != null) {
                        mWeakTangoUxLayout.get().hideExceptions(exceptions);
                    }

                    if (mUxExceptionEventListener != null) {
                        for (TangoExceptionInfo exception : exceptions) {
                            UxExceptionEvent exceptionEvent = new UxExceptionEvent();
                            exceptionEvent.mType = exception.mType;
                            exceptionEvent.mValue = Float.NaN;
                            exceptionEvent.mStatus = UxExceptionEvent.STATUS_RESOLVED;
                            mUxExceptionEventListener.onUxExceptionEvent(exceptionEvent);
                        }
                    }
                }
            });
        }
    };
    
    /**
     * Builder for {@link TangoUx} instances.
     * <p>
     * When a particular component is not explicitly this class will
     * use its default implementation.
     */
    public static class Builder {
        private final Context mContext;
        private boolean mIsMotionTrackingEnabled = true;
        private TangoUxLayout mTangoUxLayout;
        private UxExceptionEventListener mUxExceptionEventListener;
        
        /**
         * Constructor using a context for this builder and the {@link TangoUx} it creates.
         */
        public Builder(Context context) {
          mContext = context;
        }

        /**
         * Disables motion tracking.
         * This is only required if Tango is not using Motion Tracking 
         * and no pose status will be forward to TangoUx.
         *
         * @return This Builder object to allow for chaining of calls to set methods
         */
        public Builder disableMotionTracking() {
            mIsMotionTrackingEnabled = false;
            return this;
        }

        /**
         * Sets the {@link TangoUxLayout} to be notified by TangoUx.
         *
         * @return This Builder object to allow for chaining of calls to set methods
         */
        public Builder setTangoUxLayout(TangoUxLayout tangoUxLayout) {
            mTangoUxLayout = tangoUxLayout;
            return this;
        }
        
        /**
         * Sets the callback to be invoked when a exception should be raised or dismissed.
         *
         * @return This Builder object to allow for chaining of calls to set methods
         */
        public Builder setUxExceptionEventListener(UxExceptionEventListener uxExceptionListener) {
            mUxExceptionEventListener = uxExceptionListener;
            return this;
        }

        /**
         * Creates a {@link TangoUx} with the arguments supplied to this builder.
         */
        public TangoUx build() {
            final TangoUx tangoUx = new TangoUx(mContext, mIsMotionTrackingEnabled, mTangoUxLayout);
            tangoUx.setUxExceptionEventListener(mUxExceptionEventListener);
            return tangoUx;
        }
    }

}
