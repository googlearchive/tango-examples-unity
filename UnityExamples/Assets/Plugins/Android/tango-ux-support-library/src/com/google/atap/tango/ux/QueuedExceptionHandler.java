/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import android.os.SystemClock;

import java.util.ArrayDeque;

/**
 * Class used to Handle Exceptions. Holds the exceptions and check if they need
 * to be raised or dismissed.
 */
class QueuedExceptionHandler extends BaseExceptionHandler{

    private static final int DEFAULT_EXCEPTION_TRESHOLD_COUNT = 8;

    private ArrayDeque<Long> queue = new ArrayDeque<Long>();
    private int exceptionThresholdCount;
    private long exceptionTimeFrame;

    public QueuedExceptionHandler() {
        exceptionThresholdCount = DEFAULT_EXCEPTION_TRESHOLD_COUNT;
        exceptionTimeFrame = EXCEPTION_TIME_FRAME;
    }

    public QueuedExceptionHandler(int thresholdCount) {
        exceptionThresholdCount = thresholdCount;
    }

    public void exceptionDetected(TangoExceptionInfo exception) {
        mLastException = exception;
        queue.add(SystemClock.elapsedRealtime());
        while (queue.size() > exceptionThresholdCount) {
            queue.poll();
        }
    }

    public boolean raiseException() {
        if (mRaised || queue.size() < exceptionThresholdCount) {
            return false;
        } else if (queue.getLast() - queue.getFirst() < exceptionTimeFrame) {
            mRaised = true;
            return true;
        }
        return false;
    }

    public boolean hideException() {
        if (mRaised && SystemClock.elapsedRealtime() - queue.getLast() > exceptionTimeFrame) {
            mRaised = false;
            return true;
        }
        return false;
    }

    public void reset() {
        queue.clear();
        mLastException = null;
        mRaised = false;
    }

}
