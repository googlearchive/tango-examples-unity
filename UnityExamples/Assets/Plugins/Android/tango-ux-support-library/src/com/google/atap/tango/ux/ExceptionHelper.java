/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tangoservice.TangoEvent;
import com.google.atap.tangoservice.TangoPoseData;

import android.os.SystemClock;
import android.text.TextUtils;
import android.util.Log;
import android.util.SparseArray;

import java.util.ArrayList;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

/**
 * Keeps track of exceptions.
 */
class ExceptionHelper {

    private static final String TAG = ExceptionHelper.class.getSimpleName();

    /**
     * Callback to show/hide exceptions.
     */
    interface ExceptionHelperListener {
        public void onException(TangoExceptionInfo exception);

        public void hideExceptions(TangoExceptionInfo[] exceptions);
    }

    private ExceptionHelperListener mExceptionListener;

    private SparseArray<ExceptionHandler> mExceptionTracking;

    // Needed since getHighestPriorityException runs on UI thread and all other exception related
    // methods run on the executor. Makes sure that getHighestPriorityException always reads the
    // correct value on memory.
    private Object mLock = new Object();

    private ScheduledThreadPoolExecutor mExecutor = (ScheduledThreadPoolExecutor) Executors
            .newScheduledThreadPool(1);

    private ScheduledFuture<?> mTimerFuture = null;

    private float mExposure;

    private float mFeature;

    private volatile long mTimeSinceGyro;

    private volatile boolean mIsPoseStatusValid = false;
    
    private final boolean mIsMotionTrackingEnabled;

    private volatile boolean mIgnore = false;

    private long mStartTime;

    private static final int MIN_CLOUD_POINTS = 200;

    private static final float INTERPOLATION_FACTOR_UNDER = 0.2f;
    private static final float INTERPOLATION_FACTOR_OVER = 0.01f;
    private static final float INTERPOLATION_FACTOR_FEATURE = 0.8f;

    private static final float OVER_EXPOSED_THRESHOLD = 150.0f;
    private static final float UNDER_EXPOSED_THRESHOLD = 30.0f;
    private static final float FEW_FEATURE_THRESHOLD = 25.0f;

    // Exposure value from Tango event range from 0 to 255, 
    // take the median from the range as initial value.
    private static final float EXPOSURE_START_VALUE = 128.0f;

    // Tango event start to throw FewFeature event if feature counts is less than 50.
    private static final float FEATURE_START_VALUE = 50.0f;

    private static final long GYRO_TIME_FRAME = 1000L;

    private static final long MAX_QUEUE_VALUE = 10L;

    private static final long DISMISSAL_TIME = 5000L;

    protected ExceptionHelper(ExceptionHelperListener exceptionListener, 
            boolean isMotionTrackingEnabled) {
        mExceptionTracking = new SparseArray<ExceptionHandler>();
        mExceptionTracking.put(UxExceptionEvent.TYPE_OVER_EXPOSED,
                new QueuedExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_UNDER_EXPOSED,
                new QueuedExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_FEW_FEATURES,
                new QueuedExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_MOVING_TOO_FAST,
                new QueuedExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_TANGO_UPDATE_NEEDED, 
                new SystemExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_FEW_DEPTH_POINTS,
                new NoQueueExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_LYING_ON_SURFACE,
                new QueuedExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_MOTION_TRACK_INVALID,
                new MotionTrackingExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_TANGO_SERVICE_NOT_RESPONDING,
                new SystemExceptionHandler());
        mExceptionTracking.put(UxExceptionEvent.TYPE_INCOMPATIBLE_VM,
                new SystemExceptionHandler());
        mExceptionListener = exceptionListener;
        mStartTime = SystemClock.elapsedRealtime();
        mIsMotionTrackingEnabled = isMotionTrackingEnabled;
        mFeature = FEATURE_START_VALUE;
        mExposure = EXPOSURE_START_VALUE;
    }

    /**
     * Gets the highest priority exception that is raised on the queue.
     * 
     * @return TangoExceptionInfo representing that highest priority exception, returns null if
     *         nothing is found.
     */
    protected TangoExceptionInfo getHighestPriorityException() {
        ExceptionHandler highPriorityException = null;
        synchronized (mLock) {
            ExceptionHandler handler;
            SparseArray<ExceptionHandler> exceptionTracking = mExceptionTracking;
            int len = exceptionTracking.size();
            for (int i = 0; i < len; ++i) {
                handler = exceptionTracking.get(i);
                if (handler.isRaised()) {
                    if (highPriorityException == null
                            || handler.getLastException().hasHigherPriorityThan(
                                    highPriorityException.getLastException())) {
                        highPriorityException = handler;
                    }
                }
            }

            if (highPriorityException == null) {
                return null;
            }

            return highPriorityException.getLastException();
        }
    }

    protected void reset() {
        stop();
        try {
            mExecutor.awaitTermination(BaseExceptionHandler.EXCEPTION_TIME_FRAME,
                    TimeUnit.MILLISECONDS);
        } catch (InterruptedException e) {
            Log.e(TAG, "Could not terminate execution of messages.", e);
        } finally {
            ExceptionHandler handler;
            SparseArray<ExceptionHandler> exceptionTracking = mExceptionTracking;
            int len = exceptionTracking.size();
            for (int i = 0; i < len; ++i) {
                handler = exceptionTracking.get(i);
                handler.reset();
            }

            mTimerFuture = null;
            mExposure = EXPOSURE_START_VALUE;
            mFeature = FEATURE_START_VALUE;
            mTimeSinceGyro = 0;
            mIsPoseStatusValid = false;
            mStartTime = SystemClock.elapsedRealtime();
            mExecutor = (ScheduledThreadPoolExecutor) Executors
                    .newScheduledThreadPool(1);
            mIgnore = false;
        }
    }

    protected void checkForIncompatibleVM() {
        final String vmVersion = System.getProperty("java.vm.version");
        boolean isArtInUse = vmVersion != null && vmVersion.startsWith("2");
        if (!isArtInUse) {
            onIncompatibleVM();
        }
    }

    protected void onTangoOutOfDate() {
        mIgnore = true;
        mExecutor.execute(new Runnable() {
            @Override
            public void run() {
                final TangoExceptionInfo info = new TangoExceptionInfo(
                        UxExceptionEvent.TYPE_TANGO_UPDATE_NEEDED);
                exceptionDetected(info);
            }
        });
    }

    protected void onTangoEvent(final TangoEvent ev) {
        if (!isTangoEventSystemException(ev) && ignoreEvent(mIsMotionTrackingEnabled)) {
            // Discard non-Tango system events while initializing.
            return;
        }
        mExecutor.execute(new Runnable() {
            @Override
            public void run() {
                final TangoExceptionInfo exceptionInfo = getTangoExceptionFromTangoEvent(ev);
                if (exceptionInfo == null) {
                    return;
                }
                if (mExceptionTracking.get(exceptionInfo.mType) == null) {
                    Log.w(TAG, "Exception not handled: " + ev.eventKey);
                    return;
                }
                exceptionDetected(exceptionInfo);
            }
        });

    }

    protected void onXyzCountAvailable(final int xyzCount) {
        if (ignoreEvent(mIsMotionTrackingEnabled)) {
            // Discard all events while initializing
            return;
        }
        mExecutor.execute(new Runnable() {
            @Override
            public void run() {
                if (xyzCount < MIN_CLOUD_POINTS) {
                    final TangoExceptionInfo info = new TangoExceptionInfo(
                            UxExceptionEvent.TYPE_FEW_DEPTH_POINTS, xyzCount);
                    exceptionDetected(info);
                }
            }
        });
    }

    protected void onPoseStatusAvailable(final int statusCode) {
        if (ignoreEvent(false)) {
            return;
        }

        if (!mIsPoseStatusValid && SystemClock.elapsedRealtime() - mStartTime > DISMISSAL_TIME 
                && statusCode == TangoPoseData.POSE_VALID) {
            mIsPoseStatusValid = true;
            mExecutor.execute(new Runnable() {
                @Override
                public void run() {
                    final TangoExceptionInfo info = new TangoExceptionInfo(
                            UxExceptionEvent.TYPE_MOTION_TRACK_INVALID);
                    exceptionDismissed(info);
                }
            });
        } else if (mIsPoseStatusValid && statusCode != TangoPoseData.POSE_VALID) {
            mIsPoseStatusValid = false;
            mExecutor.execute(new Runnable() {
                @Override
                public void run() {
                    final TangoExceptionInfo info = new TangoExceptionInfo(
                            UxExceptionEvent.TYPE_MOTION_TRACK_INVALID);
                    exceptionDetected(info);
                }
            });
        } else {
            return;
        }

    }

    protected void onShakeDetected() {
        if (ignoreEvent(false)) {
            return;
        }
        mExecutor.execute(new Runnable() {
            @Override
            public void run() {
                final TangoExceptionInfo info = new TangoExceptionInfo(
                        UxExceptionEvent.TYPE_MOVING_TOO_FAST);
                exceptionDetected(info);
            }
        });
    }

    protected void onLyingOnSurfaceDetected() {
        if (ignoreEvent(false)) {
            return;
        }
        mTimeSinceGyro = SystemClock.elapsedRealtime();
    }

    private void onIncompatibleVM() {
        mIgnore = true;
        mExecutor.execute(new Runnable() {
            @Override
            public void run() {
                final TangoExceptionInfo info = new TangoExceptionInfo(
                        UxExceptionEvent.TYPE_INCOMPATIBLE_VM);
                exceptionDetected(info);
            }
        });
    }

    private TangoExceptionInfo getTangoExceptionFromTangoEvent(TangoEvent event) {

        String exceptionIdentifier = event.eventKey;
        if (exceptionIdentifier.equals(TangoEvent.DESCRIPTION_FISHEYE_OVER_EXPOSED)) {
            float eventValue = getTangoEventValueAsFloat(event); 
            mExposure = mExposure * (1.0f - INTERPOLATION_FACTOR_OVER) + INTERPOLATION_FACTOR_OVER
                    * eventValue;
            if (mExposure > OVER_EXPOSED_THRESHOLD) {
                if (isLyingOnSurface()) {
                    return new TangoExceptionInfo(UxExceptionEvent.TYPE_LYING_ON_SURFACE);
                } else if (mFeature < FEW_FEATURE_THRESHOLD) {
                    return new TangoExceptionInfo(UxExceptionEvent.TYPE_OVER_EXPOSED, eventValue);
                }
            }
        } else if (exceptionIdentifier.equals(TangoEvent.DESCRIPTION_FISHEYE_UNDER_EXPOSED)) {
            float eventValue = getTangoEventValueAsFloat(event);
            mExposure = mExposure * (1.0f - INTERPOLATION_FACTOR_UNDER) + INTERPOLATION_FACTOR_UNDER
                    * eventValue;
            if (mExposure < UNDER_EXPOSED_THRESHOLD) {
                if (isLyingOnSurface()) {
                    return new TangoExceptionInfo(UxExceptionEvent.TYPE_LYING_ON_SURFACE);
                } else if (mFeature < FEW_FEATURE_THRESHOLD) {
                    return new TangoExceptionInfo(UxExceptionEvent.TYPE_UNDER_EXPOSED, eventValue);
                }
            }
        } else if (isTangoEventSystemException(event)) {
            return new TangoExceptionInfo(UxExceptionEvent.TYPE_TANGO_SERVICE_NOT_RESPONDING);
        } else if (exceptionIdentifier.equals(TangoEvent.DESCRIPTION_TOO_FEW_FEATURES_TRACKED)) {
            float eventValue = getTangoEventValueAsFloat(event); 
            mFeature = mFeature * (1.0f - INTERPOLATION_FACTOR_FEATURE) + 
                    INTERPOLATION_FACTOR_FEATURE * eventValue;
            if (isLyingOnSurface()) {
                return new TangoExceptionInfo(UxExceptionEvent.TYPE_LYING_ON_SURFACE);
            } else if (mFeature < FEW_FEATURE_THRESHOLD) {
                return new TangoExceptionInfo(UxExceptionEvent.TYPE_FEW_FEATURES, eventValue);
            }
        }
        return null;
    }

    private float getTangoEventValueAsFloat(TangoEvent event) {
        float valueToReturn = Float.NaN;
        if (!TextUtils.isEmpty(event.eventValue)) {
            try {
                valueToReturn = Float.parseFloat(event.eventValue);
            } catch (NumberFormatException e) {
                Log.e(TAG, "Error parsing " + event.eventType + "value!", e);
            }
        }
        return valueToReturn;
    }
    
    private boolean isLyingOnSurface() {
        return SystemClock.elapsedRealtime() - mTimeSinceGyro < GYRO_TIME_FRAME;
    }

    private boolean ignoreEvent(boolean checkPose) {
        return isQueueFull() || (checkPose && !mIsPoseStatusValid);
    }
    
    private boolean isQueueFull() {
        int size = mExecutor.getQueue().size();
        if (mIgnore || size > MAX_QUEUE_VALUE) {
            return true;
        }
        return false;
    }
    
    private void exceptionDetected(TangoExceptionInfo exception) {
        synchronized (mLock) {
            ExceptionHandler handler = mExceptionTracking.get(exception.mType);
            handler.exceptionDetected(exception);
            if (handler.raiseException()) {
                mExceptionListener.onException(exception);
                if (!mIgnore) {
                    startExceptionTimerIfNeeded(handler.getExceptionTimeFrame());
                }
            }
        }
    }

    private void exceptionDismissed(TangoExceptionInfo exception) {
        synchronized (mLock) {
            ExceptionHandler handler = mExceptionTracking.get(exception.mType);
            handler.exceptionDismissed();
            if (!mIgnore) {
                startExceptionTimerIfNeeded(handler.getExceptionTimeFrame());
            }
        }
    }

    private void startExceptionTimerIfNeeded(long delay) {
        if (!isTimerStopped()) {
            if (delay < mTimerFuture.getDelay(TimeUnit.MILLISECONDS)) {
                mTimerFuture.cancel(false);
            } else {
                return;
            }
        }
        if (delay == 0) {
            mExecutor.execute(mHideException);
            return;
        } else {
            mTimerFuture = mExecutor.schedule(mHideException, delay, TimeUnit.MILLISECONDS);
        }

    }

    Runnable mHideException = new Runnable() {

        @Override
        public void run() {
            boolean shouldTimerContinue = false;
            long delay = -1;

            ArrayList<TangoExceptionInfo> handlersToHide = new ArrayList<TangoExceptionInfo>();
            synchronized (mLock) {
                ExceptionHandler handler;
                SparseArray<ExceptionHandler> exceptionTracking = mExceptionTracking;
                int len = exceptionTracking.size();
                for (int i = 0; i < len; ++i) {
                    handler = exceptionTracking.get(i);
                    if (handler.hideException() && handler.getLastException() != null) {
                        handlersToHide.add(handler.getLastException());
                    }

                    if (handler.isRaised() && !mIgnore) {
                        if (delay == -1 || handler.getExceptionTimeFrame() < delay) {
                            delay = handler.getExceptionTimeFrame();
                        }
                        shouldTimerContinue = true;
                    }
                }
            }

            if (mExceptionListener != null && handlersToHide.size() > 0) {
                TangoExceptionInfo[] result = (TangoExceptionInfo[]) handlersToHide
                        .toArray(new TangoExceptionInfo[handlersToHide.size()]);
                mExceptionListener.hideExceptions(result);
            }

            if (shouldTimerContinue) {
                startExceptionTimerIfNeeded(delay);
            }

        }
    };

    private boolean isTimerStopped() {
        return mTimerFuture == null || mTimerFuture.getDelay(TimeUnit.MILLISECONDS) <= 0;
    }

    protected void stop() {
        mIgnore = true;
        mExecutor.shutdownNow();
    }

    private boolean isTangoEventSystemException(TangoEvent event) {
        return event.eventKey.equals(TangoEvent.KEY_SERVICE_EXCEPTION)
                || event.eventKey.equals(TangoEvent.VALUE_SERVICE_FAULT);
    }
}
