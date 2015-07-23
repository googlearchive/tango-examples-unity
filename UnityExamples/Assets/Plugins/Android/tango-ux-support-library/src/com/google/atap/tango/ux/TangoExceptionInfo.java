/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import com.google.atap.tango.uxsupportlibrary.R;

import android.content.res.Resources;

/**
 * Data Object containing all the necessary info for each Exception.
 */
class TangoExceptionInfo {

    float mValue = Float.NaN;
    // UxExceptionEvent type.
    int mType;
    int mGroup;
    String mTitle = "";
    String mDescription = "";
    int mIcon;
    private int mPriority = -1;

    // Exception Groups.
    static final int GROUP_SYSTEM = 0;
    static final int GROUP_CANT_SEE = 1;
    static final int GROUP_LOST = 2;
    static final int GROUP_DIZZY = 3;
    
    TangoExceptionInfo(int type) {
        mType = type;
    }
    
    TangoExceptionInfo(int type, float value) {
        mType = type;
        mValue = value;
    }

    void loadExceptionData(Resources resources) {
        switch (mType) {
            case UxExceptionEvent.TYPE_TANGO_SERVICE_NOT_RESPONDING:
                mTitle = resources.getString(R.string.device_not_responding_title);
                mDescription = resources.getString(R.string.device_not_responding_description);
                mPriority = 0;
                mGroup = GROUP_SYSTEM;
                break;
            case UxExceptionEvent.TYPE_TANGO_UPDATE_NEEDED:
                mTitle = resources.getString(R.string.service_updated_title);
                mDescription = resources.getString(R.string.service_updated_description);
                mPriority = 0;
                mGroup = GROUP_SYSTEM;
                break;
            case UxExceptionEvent.TYPE_MOVING_TOO_FAST:
                mTitle = resources.getString(R.string.moving_too_fast_title);
                mDescription = resources.getString(R.string.moving_too_fast_description);
                mPriority = 6;
                mGroup = GROUP_DIZZY;
                break;
            case UxExceptionEvent.TYPE_OVER_EXPOSED:
                mTitle = resources.getString(R.string.too_much_light_title);
                mDescription = resources.getString(R.string.too_much_light_description);
                mPriority = 3;
                mGroup = GROUP_CANT_SEE;
                break;
            case UxExceptionEvent.TYPE_FEW_FEATURES:
                mTitle = resources.getString(R.string.space_not_recognized_title);
                mDescription = resources.getString(R.string.space_not_recognized_description);
                mPriority = 5;
                mGroup = GROUP_LOST;
                break;
            case UxExceptionEvent.TYPE_FEW_DEPTH_POINTS:
                mTitle = resources.getString(R.string.unable_to_detect_surface_title);
                mDescription = resources.getString(R.string.unable_to_detect_surface_description);
                mPriority = 4;
                mGroup = GROUP_CANT_SEE;
                break;
            case UxExceptionEvent.TYPE_UNDER_EXPOSED:
                mTitle = resources.getString(R.string.not_enough_light_title);
                mDescription = resources.getString(R.string.not_enough_light_description);
                mPriority = 3;
                mGroup = GROUP_CANT_SEE;
                break;
            case UxExceptionEvent.TYPE_LYING_ON_SURFACE:
                mTitle = resources.getString(R.string.lying_on_surface_title);
                mDescription = resources.getString(R.string.lying_on_surface_description);
                mPriority = 2;
                mGroup = GROUP_DIZZY;
                break;
            case UxExceptionEvent.TYPE_MOTION_TRACK_INVALID:
                mTitle = resources.getString(R.string.motion_track_title);
                mDescription = resources.getString(R.string.motion_track_description);
                mPriority = 1;
                mGroup = GROUP_LOST;
                break;
            case UxExceptionEvent.TYPE_INCOMPATIBLE_VM:
                mTitle = resources.getString(R.string.run_time_mismatch_title);
                mDescription = resources.getString(R.string.run_time_mismatch_description);
                mPriority = 0;
                mGroup = GROUP_SYSTEM;
                break;
        }

        switch (mGroup) {
            case GROUP_CANT_SEE:
                mIcon = R.drawable.ic_exception_i_cant_see;
                break;
            case GROUP_DIZZY:
                mIcon = R.drawable.ic_exception_i_am_dizzy;
                break;
            case GROUP_LOST:
                mIcon = R.drawable.ic_exception_i_am_lost;
                break;
            case GROUP_SYSTEM:
                mIcon = R.drawable.ic_exception_system;
                break;
        }
    }

    boolean hasHigherPriorityThan(TangoExceptionInfo info) {

        if (info == null) {
            return true;
        }

        return mPriority < info.mPriority;
    }
}
