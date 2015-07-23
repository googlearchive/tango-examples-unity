/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;


/**
 * Used to Handle System Exceptions. Holds the exceptions and check if they need to be raised
 * or dismissed.
 */
interface ExceptionHandler {

    public void exceptionDetected(TangoExceptionInfo exception);
    
    public void exceptionDismissed();

    public boolean raiseException();
    
    public boolean hideException();
    
    public void reset();

    public boolean isRaised();

    public TangoExceptionInfo getLastException();
    
    public long getExceptionTimeFrame();

}
