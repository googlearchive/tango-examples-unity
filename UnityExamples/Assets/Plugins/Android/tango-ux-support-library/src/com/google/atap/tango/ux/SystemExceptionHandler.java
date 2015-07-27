/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;


/**
 * Class used to Handle System Exceptions. Holds the exceptions and check if they need to be raised
 * or dismissed.
 */
class SystemExceptionHandler extends BaseExceptionHandler {

    private TangoExceptionInfo mLastException;

    @Override
    public void exceptionDetected(TangoExceptionInfo exception) {
        mLastException = exception;
    }

    @Override
    public boolean raiseException() {
        if (mRaised) {
            return false;
        } else if (mLastException != null) {
            mRaised = true;
            return true;
        }
        return false;
    }

    @Override
    public boolean hideException() {
        return false;
    }
    
    @Override
    public void reset() {
    }

}
