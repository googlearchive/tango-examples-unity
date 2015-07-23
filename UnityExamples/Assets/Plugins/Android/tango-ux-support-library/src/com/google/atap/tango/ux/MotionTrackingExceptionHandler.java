/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;


/**
 * Class used to Handle Pose Status Exceptions. Holds the exceptions and check if they need
 * to be raised or dismissed.
 */
class MotionTrackingExceptionHandler extends BaseExceptionHandler {

    private boolean mIsInvalid = false;

    @Override
    public void exceptionDetected(TangoExceptionInfo exception) {
        mLastException = exception;
        mIsInvalid = true;
    }
    
    @Override
    public void exceptionDismissed() {
        mIsInvalid = false;
    }

    @Override
    public boolean raiseException() {
        if (mRaised) {
            return false;
        } else if (mIsInvalid) {
            mRaised = true;
            return true;
        }
        return false;
    }

    @Override
    public boolean hideException() {
        if (mRaised && !mIsInvalid) {
            mRaised = false;
            return true;
        }
        return false;
    }

    @Override
    public void reset() {
        mRaised = false;
        mLastException = null;
        mIsInvalid = false;
    }

    @Override
    public long getExceptionTimeFrame() {
        return 0;
    }

}
