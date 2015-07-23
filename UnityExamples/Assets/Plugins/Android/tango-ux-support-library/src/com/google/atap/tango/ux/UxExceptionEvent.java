/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

/**
 * Object used to report exception events. 
 * Whenever an exception is detected or resolved, an event is triggered.
 * Exception events may hold a value, depending on the type of the exception.
 */
public class UxExceptionEvent {

    int mType;
    
    float mValue;

    int mStatus;

    /**
     * Constant for {@link #getStatus}: The exception was detected.
     */
    public static final int STATUS_DETECTED = 1;

    /**
     * Constant for {@link #getStatus}: The exception was resolved.
     */
    public static final int STATUS_RESOLVED = 0;

    /**
     * Constant for {@link #getType}: Camera is over exposed exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     * 
     * <p>
     * Use {@link #getValue} to retrieve exposure value.
     * </p>
     */
    public static final int TYPE_OVER_EXPOSED = 0;

    /**
     * Constant for {@link #getType}: Camera is under exposed exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     * 
     * <p>
     * Use {@link #getValue} to retrieve exposure value.
     * </p>
     */
    public static final int TYPE_UNDER_EXPOSED = 1;

    /**
     * Constant for {@link #getType}: Device is being moved too fast exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     */
    public static final int TYPE_MOVING_TOO_FAST = 2;

    /**
     * Constant for {@link #getType}: Too few features exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     * 
     * <p>
     * Use {@link #getValue} to retrieve the number of features tracked.
     * </p>
     */
    public static final int TYPE_FEW_FEATURES = 3;

    /**
     * Constant for {@link #getType}: Unable to detect any surface exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     * 
     * <p>
     * Use {@link #getValue} to retrieve points the number of points detected.
     * </p>
     */
    public static final int TYPE_FEW_DEPTH_POINTS = 4;

    /**
     * Constant for {@link #getType}: Device is lying on a surface exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     */
    public static final int TYPE_LYING_ON_SURFACE = 5;

    /**
     * Constant for {@link #getType}: Motion tracking is invalid exception event.
     * 
     * <p>
     * Use {@link #getStatus} to know if the exception was detected or resolved.
     * </p>
     */
    public static final int TYPE_MOTION_TRACK_INVALID = 6;

    /**
     * Constant for {@link #getType}: Tango Service stopped responding exception event.
     * 
     * <p>
     * {@link #getStatus} is always {@link #STATUS_DETECTED}.
     * </p>
     */
    public static final int TYPE_TANGO_SERVICE_NOT_RESPONDING = 7;

    /**
     * Constant for {@link #getType}: Incompatible vm is found exception event.
     * 
     * <p>
     * {@link #getStatus} is always {@link #STATUS_DETECTED}.
     * </p>
     */
    public static final int TYPE_INCOMPATIBLE_VM = 8;

    /**
     * Constant for {@link #getType}: Tango version update is needed exception event.
     * 
     * <p>
     * {@link #getStatus} is always {@link #STATUS_DETECTED}.
     * </p>
     */
    public static final int TYPE_TANGO_UPDATE_NEEDED = 9;

    /**
     * Get the exception type.
     * 
     * @return The type of this exception event, 
     *              it will be one of the exception types described above.
     */
    public int getType(){
        return mType;
    }
    
    /**
     * Get the exception value.
     * 
     * @return The value of this exception event. Can be {@code NaN} if no value is available.
     */
    public float getValue() {
        return mValue;
    }
    
    /**
     * Get the exception status. 
     * 
     * @return {@link #STATUS_DETECTED} if the exception is detected, 
     *              {@link #STATUS_RESOLVED} if the exception is resolved.
     */
    public int getStatus(){
        return mStatus;
    }

}
