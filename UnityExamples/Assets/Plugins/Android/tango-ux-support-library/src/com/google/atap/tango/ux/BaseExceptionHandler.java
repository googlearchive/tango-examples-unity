/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;


/**
 * Base abstract class implementation of ExceptionHandler.
 */
abstract class BaseExceptionHandler implements ExceptionHandler{

    static final long EXCEPTION_TIME_FRAME = 1000L;
    
    protected boolean mRaised;
    protected TangoExceptionInfo mLastException;

    public boolean isRaised() {
        return mRaised;
    }

    public TangoExceptionInfo getLastException() {
        return mLastException;
    }
    
    public void exceptionDismissed(){
        
    }
    
    @Override
    public long getExceptionTimeFrame() {
        return EXCEPTION_TIME_FRAME;
    }
}
