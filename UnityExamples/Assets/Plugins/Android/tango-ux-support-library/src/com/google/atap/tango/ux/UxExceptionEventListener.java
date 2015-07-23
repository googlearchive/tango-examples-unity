/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

/**
 * Listener used to notify UX exception. If apps want to handle themselves the exceptions, they will
 * be notified by this listener when an exceptions should be raised or dismissed.
 */
public interface UxExceptionEventListener {

    void onUxExceptionEvent(UxExceptionEvent event);
}
