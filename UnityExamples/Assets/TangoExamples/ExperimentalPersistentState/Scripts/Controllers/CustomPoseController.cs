/*
 * Copyright 2015 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using System.Collections;
using Tango;
using System;

public class CustomPoseController : MonoBehaviour, ITangoPose {
    public Vector3 positionOffest = new Vector3(0.0f, 1.35f, 0.0f);

    TangoApplication tangoApplication;
    Quaternion rotationFix = Quaternion.Euler(90.0f, 0.0f, 0.0f);
    Quaternion startingRotation;

    TangoPoseData motionTrackingPose;
    TangoPoseData adfPose;

    TangoPoseStates preTangoState;
    // Use this for initialization
    void Start () {
        Statics.currentTangoState = TangoPoseStates.Connecting;

        tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication == null) {
            tangoApplication = FindObjectOfType<TangoApplication>();
        }
        tangoApplication.InitProviders(Statics.curADFId);
        tangoApplication.Register(this);
        tangoApplication.ConnectToService();

        startingRotation = transform.rotation;
    }
    
    // Update is called once per frame
    void Update () {
        TangoPoseData pose;
        if (Statics.currentTangoState == TangoPoseStates.Running){
            if (adfPose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID) {
                pose = adfPose;
            }
            else {
                return;
            }
        } else if (Statics.currentTangoState == TangoPoseStates.Relocalizing) {
            if (motionTrackingPose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID) {
                pose = motionTrackingPose;
            }
            else {
                return;
            }
        } else {
            return;
        }

        Vector3 tangoPosition = new Vector3((float)pose.translation [0],
                                    (float)pose.translation [2],
                                    (float)pose.translation [1]);
        
        Quaternion tangoRotation = new Quaternion((float)pose.orientation [0],
                                       (float)pose.orientation [2], // these rotation values are swapped on purpose
                                       (float)pose.orientation [1],
                                       (float)pose.orientation [3]);

        Quaternion axisFix = Quaternion.Euler(-tangoRotation.eulerAngles.x,
                                              -tangoRotation.eulerAngles.z,
                                              tangoRotation.eulerAngles.y);
        
        transform.rotation = startingRotation * (rotationFix * axisFix);
        transform.position = (startingRotation * tangoPosition) + positionOffest;

        // Fire the state change event.
        if (preTangoState != Statics.currentTangoState) {
            EventManager.instance.TangoPoseStateChanged(Statics.currentTangoState);
        }
        preTangoState = Statics.currentTangoState;
    }

    public void OnTangoPoseAvailable(TangoPoseData pose) {
        if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
            pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE) {
            if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID) {
                Statics.currentTangoState = TangoPoseStates.Running;
                adfPose = pose;
            } else {
                Statics.currentTangoState = TangoPoseStates.Relocalizing;
            }
        } else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
            pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE) {
            if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID) {
                motionTrackingPose = pose;
            }
        } else {
            return;
        }
    }

    private void OnDestroy() {
        tangoApplication.Shutdown();
    }
}
