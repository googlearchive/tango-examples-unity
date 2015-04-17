/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */
package com.google.atap.tangoservice;

import com.google.atap.tangoservice.ITangoListener;
import com.google.atap.tangoservice.ITangoLogRequestListener;
import com.google.atap.tangoservice.TangoAreaDescriptionMetaData;
import com.google.atap.tangoservice.TangoConfig;
import com.google.atap.tangoservice.TangoCoordinateFramePair;
import com.google.atap.tangoservice.TangoCameraIntrinsics;
import com.google.atap.tangoservice.TangoPoseData;

interface ITango {
  // Basic functionality.
  int connect(in ITangoListener listener, in TangoConfig config);
  int setPoseListenerFrames(in List<TangoCoordinateFramePair> framePairs);
  int disconnect();
  int getPoseAtTime(double timestamp, in TangoCoordinateFramePair framePair, out TangoPoseData pose);
  int getConfig(int configType, out TangoConfig config);
  int connectSurface(int cameraId, in Surface surface);
  int disconnectSurface(int cameraId);
  int resetMotionTracking();

  // ADF functionality.
  int saveAreaDescription(out List<String> uuid);
  int getAreaDescriptionUuidList(out List<String> uuidList);
  int loadAreaDescriptionMetaData(String uuid, out TangoAreaDescriptionMetaData metadata);
  int saveAreaDescriptionMetaData(String uuid, in TangoAreaDescriptionMetaData metadata);
  int importAreaDescriptionFile(out List<String> uuid, in String filepath);
  int exportAreaDescriptionFile(String uuid, in String filepath);
  int deleteAreaDescription(String uuid);
  int getCameraIntrinsics(int cameraId, out TangoCameraIntrinsics intrinsics);

  // Must be here to preserve enum code ordering. Not currently implemented. 
  int dummyGetTimestamp();

  // Logging functionality.
  int registerLogRequestListener(in ITangoLogRequestListener listener);
}
