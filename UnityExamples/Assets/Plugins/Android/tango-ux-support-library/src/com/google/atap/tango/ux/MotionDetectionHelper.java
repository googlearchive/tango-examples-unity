/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */

package com.google.atap.tango.ux;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;

/**
 * Uses motion sensors to detect if the device is shaking or lying down on a surface.
 */
class MotionDetectionHelper {

    // Thresholds at which the device is considered to be lying down on a surface.
    // TODO(jafonso): Look for a more device independent way to get this value
    
    // Squared threshold at which the device is considered to be static, in (m/s^2)^2.
    private static final float ACCELERATION_LYING_ON_SURFACE_THRESHOLD_SQUARED = 0.008f * 0.008f;

    // Orientation offset of the device compare to gravity vector, in degree.
    private static final float Y_ORIENTATION_THRESHOLD = 15.0f;

    // Squared threshold at which the device is considered to be rotating too fast, in (m/s^2)^2.
    private static final float ACCELERATION_ROTATION_THRESHOLD_SQUARED = 7.0f;

    // Squared threshold at which the device is considered to be moving too fast, in (m/s^2)^2.
    private static final float ACCELERATION_TRANSLATION_THRESHOLD_SQUARED = 27.0f;

    private SensorManager mSensorManager;

    private float[] mGravity, mLinearAcceleration, mRotateAcceleration, mGeomagnetic;
    private float[] mR, mI;

    private float mAccelerationRotationSquared, mAccelerationTranslationSquared, mYOrientation;

    private MotionDetectionListener mListener;

    private final SensorEventListener mSensorEventListener = new SensorEventListener() {

        public void onSensorChanged(SensorEvent event) {
            if (mListener == null) {
                return;
            }

            final float alpha = 0.99f;
            switch (event.sensor.getType()) {

                case Sensor.TYPE_ACCELEROMETER:
                    mGravity[0] = alpha * mGravity[0] + (1 - alpha) * event.values[0];
                    mGravity[1] = alpha * mGravity[1] + (1 - alpha) * event.values[1];
                    mGravity[2] = alpha * mGravity[2] + (1 - alpha) * event.values[2];
                    break;

                case Sensor.TYPE_LINEAR_ACCELERATION:
                    mLinearAcceleration[0] = 
                        alpha * mLinearAcceleration[0] + (1 - alpha) * event.values[0];
                    mLinearAcceleration[1] = 
                        alpha * mLinearAcceleration[1] + (1 - alpha) * event.values[1];
                    mLinearAcceleration[2] = 
                        alpha * mLinearAcceleration[2] + (1 - alpha) * event.values[2];

                    mAccelerationTranslationSquared = 
                        mLinearAcceleration[0] * mLinearAcceleration[0] +
                        mLinearAcceleration[1] * mLinearAcceleration[1] +
                        mLinearAcceleration[2] * mLinearAcceleration[2];
                    break;

                case Sensor.TYPE_MAGNETIC_FIELD:
                    mGeomagnetic[0] = alpha * mGeomagnetic[0] + (1 - alpha) * event.values[0];
                    mGeomagnetic[1] = alpha * mGeomagnetic[1] + (1 - alpha) * event.values[1];
                    mGeomagnetic[2] = alpha * mGeomagnetic[2] + (1 - alpha) * event.values[2];
                    break;

                case Sensor.TYPE_GYROSCOPE:
                    mRotateAcceleration[0] = 
                        alpha * mRotateAcceleration[0] + (1 - alpha) * event.values[0];
                    mRotateAcceleration[1] = 
                        alpha * mRotateAcceleration[1] + (1 - alpha) * event.values[1];
                    mRotateAcceleration[2] = 
                        alpha * mRotateAcceleration[2] + (1 - alpha) * event.values[2];
                    
                    mAccelerationRotationSquared = 
                              mRotateAcceleration[0] * mRotateAcceleration[0]
                            + mRotateAcceleration[1] * mRotateAcceleration[1]
                            + mRotateAcceleration[2] * mRotateAcceleration[2];
                    break;
            }

            if (SensorManager.getRotationMatrix(mR, mI, mGravity, mGeomagnetic)) {
                float orientation[] = new float[3];
                SensorManager.getOrientation(mR, orientation);
                mYOrientation = (float) Math.toDegrees(orientation[1]);
            }

            if (mListener != null) {
                if (Math.abs(mYOrientation) < Y_ORIENTATION_THRESHOLD
                        && mAccelerationRotationSquared < 
                        ACCELERATION_LYING_ON_SURFACE_THRESHOLD_SQUARED) {
                    mListener.onLyingOnSurface();
                    return;
                }
                if (mAccelerationRotationSquared > ACCELERATION_ROTATION_THRESHOLD_SQUARED ||
                    mAccelerationTranslationSquared > ACCELERATION_TRANSLATION_THRESHOLD_SQUARED) {
                    mListener.onShaking();
                    return;
                }
            }
        }

        @Override
        public void onAccuracyChanged(Sensor sensor, int accuracy) {
        }
    };

    /**
     * Notifies relevant motion events.
     */
    interface MotionDetectionListener {

        void onLyingOnSurface();
        
        void onShaking();
    }

    MotionDetectionHelper(Context context) {
        mSensorManager = (SensorManager) context.getSystemService(Context.SENSOR_SERVICE);
    }

    void start(MotionDetectionListener listener) {
        reset();

        mListener = listener;

        mSensorManager.registerListener(mSensorEventListener,
                mSensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER),
                SensorManager.SENSOR_DELAY_FASTEST);
        mSensorManager.registerListener(mSensorEventListener,
                mSensorManager.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD),
                SensorManager.SENSOR_DELAY_FASTEST);
        mSensorManager.registerListener(mSensorEventListener,
                mSensorManager.getDefaultSensor(Sensor.TYPE_GYROSCOPE),
                SensorManager.SENSOR_DELAY_FASTEST);
        mSensorManager.registerListener(mSensorEventListener,
                mSensorManager.getDefaultSensor(Sensor.TYPE_LINEAR_ACCELERATION),
                SensorManager.SENSOR_DELAY_FASTEST);
    }

    void stop() {
        mListener = null;
        mSensorManager.unregisterListener(mSensorEventListener);
    }

    private void reset() {
        mGravity = new float[3];
        mLinearAcceleration = new float[3];
        mRotateAcceleration = new float[3];
        mGeomagnetic = new float[3];

        mR = new float[9];
        mI = new float[9];

        mAccelerationRotationSquared = ACCELERATION_LYING_ON_SURFACE_THRESHOLD_SQUARED;
        mYOrientation = Y_ORIENTATION_THRESHOLD;
    }
}
