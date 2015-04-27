/*
 * Copyright 2014 Google Inc. All Rights Reserved.
 * Distributed under the Project Tango Preview Development Kit (PDK) Agreement.
 * CONFIDENTIAL. AUTHORIZED USE ONLY. DO NOT REDISTRIBUTE.
 */
package com.google.atap.tangoservice;

import com.google.atap.tangoservice.TangoEvent;
import com.google.atap.tangoservice.TangoPoseData;
import com.google.atap.tangoservice.TangoXyzIjData;

interface ITangoListener {
  void onPoseAvailable(in TangoPoseData pose);
  void onXYZijAvailable(in TangoXyzIjData xyzIj);
  void onTangoEvent(in TangoEvent event);
}
