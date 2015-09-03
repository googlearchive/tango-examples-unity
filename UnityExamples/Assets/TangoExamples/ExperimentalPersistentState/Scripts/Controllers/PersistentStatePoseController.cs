//-----------------------------------------------------------------------
// <copyright file="PersistentStatePoseController.cs" company="Google">
//
// Copyright 2015 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;
using Tango;

/// <summary>
/// Custom pose controller.
/// </summary>
public class PersistentStatePoseController : MonoBehaviour, ITangoPose
{
    public Vector3 positionOffest = new Vector3(0.0f, 1.35f, 0.0f);
    
    private TangoApplication tangoApplication;
    private Quaternion rotationFix = Quaternion.Euler(90.0f, 0.0f, 0.0f);
    private Quaternion startingRotation;
    
    private TangoPoseStates preTangoState;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start()
    {
        Statics.currentTangoState = TangoPoseStates.Connecting;
        
        tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication == null)
        {
            tangoApplication = FindObjectOfType<TangoApplication>();
        }
        tangoApplication.InitProviders(Statics.curADFId);
        tangoApplication.Register(this);
        tangoApplication.ConnectToService();
        
        startingRotation = transform.rotation;
    }

    /// <summary>
    /// Tango pose event.
    /// </summary>
    /// <param name="pose">Pose.</param>
    public void OnTangoPoseAvailable(TangoPoseData pose)
    {
        if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
            pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
        {
            if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                Statics.currentTangoState = TangoPoseStates.Running;
                UpdateTransform(pose);
                return;
            }
            else
            {
                Statics.currentTangoState = TangoPoseStates.Relocalizing;
            }
        }
        else if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                 pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
        {
            if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                UpdateTransform(pose);
            }
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// Updates the transform from a Tango pose data.
    /// </summary>
    /// <param name="pose">Tango pose.</param>
    private void UpdateTransform(TangoPoseData pose)
    {
        Vector3 tangoPosition = new Vector3((float)pose.translation[0],
                                            (float)pose.translation[2],
                                            (float)pose.translation[1]);
        
        Quaternion tangoRotation = new Quaternion((float)pose.orientation[0],
                                                  (float)pose.orientation[2], // these rotation values are swapped on purpose
                                                  (float)pose.orientation[1],
                                                  (float)pose.orientation[3]);
        
        Quaternion axisFix = Quaternion.Euler(-tangoRotation.eulerAngles.x,
                                              -tangoRotation.eulerAngles.z,
                                              tangoRotation.eulerAngles.y);
        
        transform.rotation = startingRotation * (rotationFix * axisFix);
        transform.position = (startingRotation * tangoPosition) + positionOffest;
        
        // Fire the state change event.
        if (preTangoState != Statics.currentTangoState)
        {
            EventManager.Instance.SendTangoPoseStateChanged(Statics.currentTangoState);
        }
        preTangoState = Statics.currentTangoState;
    }
}