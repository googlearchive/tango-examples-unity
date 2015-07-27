/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

/**
 * Defines the UI settings for Tango Ux layout.
 */
public final class UiSettings {

    private boolean mConnectionLayoutEnabled = true;
    private boolean mExceptionsEnabled = true;
    private UiSettingsListener mListener;

    /**
     * Listener used to notify Settings changes.
     */
    interface UiSettingsListener {
        /**
         * This method will be invoked if Exceptions are enabled.
         */
        void onExceptionsEnabled();

        /**
         * This method will be invoked if Exceptions are disabled.
         */
        void onExceptionsDisabled();

        /**
         * This method will be invoked if ConnectionScreen is enabled.
         */
        void onConnectionLayoutEnabled();

        /**
         * This method will be invoked if ConnectionScreen is disabled.
         */
        void onConnectionLayoutDisabled();
    }

    UiSettings(UiSettingsListener listener) {
        mListener = listener;
    }

    /**
     * Gets whether the Connection layout is enabled/disabled.
     * 
     * @return <code>true</code> if the Connection layout is enabled; <code>false</code> if the
     *         Connection layout is disabled.
     */
    public boolean isConnectionLayoutEnabled() {
        return mConnectionLayoutEnabled;
    }

    /**
     * Specifies whether the Connection layout should be enabled.
     * 
     * @param enabled <code>true</code> to enable the Connection layout; <code>false</code> to
     *            disable the Connection layout.
     */
    public void setConnectionLayoutEnabled(boolean enabled) {
        mConnectionLayoutEnabled = enabled;
        if (enabled) {
            mListener.onConnectionLayoutEnabled();
        } else {
            mListener.onConnectionLayoutDisabled();
        }
    }

    /**
     * Gets whether the Exceptions are enabled/disabled.
     * 
     * @return <code>true</code> if the Exceptions are enabled; <code>false</code> if the Exceptions
     *         are disabled.
     */
    public boolean isExceptionsEnabled() {
        return mExceptionsEnabled;
    }

    /**
     * Specifies whether the Exceptions should be enabled.
     * 
     * @param enabled <code>true</code> to enable the Exceptions; <code>false</code> to disable the
     *            Exceptions.
     */
    public void setExceptionsEnabled(boolean enabled) {
        mExceptionsEnabled = enabled;
        if (enabled) {
            mListener.onExceptionsEnabled();
        } else {
            mListener.onExceptionsDisabled();
        }
    }
}
