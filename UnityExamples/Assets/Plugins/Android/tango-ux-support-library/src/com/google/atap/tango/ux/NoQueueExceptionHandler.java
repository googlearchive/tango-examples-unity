/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import android.os.SystemClock;

/**
 * Class used to Handle Exceptions. Holds the exceptions and check if they need
 * to be raised or dismissed.
 */
class NoQueueExceptionHandler extends BaseExceptionHandler {

    private long mLastExceptionTime;

    @Override
    public void exceptionDetected(TangoExceptionInfo exception) {
        mLastException = exception;
        mLastExceptionTime = SystemClock.elapsedRealtime();
    }

    @Override
    public boolean raiseException() {
        if (mRaised) {
            return false;
        } else if (SystemClock.elapsedRealtime() - mLastExceptionTime < EXCEPTION_TIME_FRAME) {
            mRaised = true;
            return true;
        }
        return false;
    }

    @Override
    public boolean hideException() {
        if (mRaised && SystemClock.elapsedRealtime() - mLastExceptionTime > EXCEPTION_TIME_FRAME) {
            mRaised = false;
            return true;
        }
        return false;
    }

    @Override
    public void reset() {
        mRaised = false;
        mLastException = null;
    }

}
