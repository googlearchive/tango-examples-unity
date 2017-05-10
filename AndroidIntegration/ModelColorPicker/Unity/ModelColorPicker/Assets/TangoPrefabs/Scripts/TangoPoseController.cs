//-----------------------------------------------------------------------
// <copyright file="TangoPoseController.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
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
using Tango;
using UnityEngine;

/// <summary>
/// A movement controller that updates the attached GameObject's transform to reflect
/// poses from a Tango device.
/// </summary>
public class TangoPoseController : MonoBehaviour
{
    /// <summary>
    /// When enabled, TangoPoseController will limit transform updates to rotational pitch and
    /// roll.  External transform updates will be preserved (as always) when the clutch is subsequently
    /// disabled.  This is useful for allowing the device to move in physical space without updating
    /// the controller's position and yaw.
    /// </summary>
    public bool m_clutchEnabled;

    /// <summary>
    /// The modes for selecting the relative base frame from which TangoPoseController will calculate transform
    /// updates.  If set to AUTO_DETECT, the controller will match the settings of TangoApplication
    /// which is correct for most use cases.  Alternatively, the base frame can be statically set.
    /// </summary>
    public BaseFrameSelectionModeEnum m_baseFrameMode;

    /// <summary>
    /// Holds reference to any TangoARScreen component attached to the same GameObject. TangoPoseController will syncronize pose
    /// updates every frame with the most recent timestamp used by 'm_tangoARScreen' to render a frame.
    /// This can be useful to correct for color camera latency; thus preventing a mismatch between the newer
    /// controller position (virtual-reality render) and the older camera frame (reality-reality render).
    /// </summary>
    private TangoARScreen m_tangoARScreen;

    /// <summary>
    /// If supplied, positional updates from TangoPoseController will be sent to the sent
    /// to the Move method of 'm_characterController' rather applied directly to the
    /// transform.
    /// </summary>
    private CharacterController m_characterController;

    /// <summary>
    /// A tranformation matrix representing the postion and rotation of the last camera pose
    /// relative to the unity world.  This does not factor in external offsets applied to
    /// the transform.
    /// </summary>
    private Matrix4x4 m_unityWorld_T_unityCamera = Matrix4x4.identity;

    /// <summary>
    /// A tranformation matrix representing the translation and rotational yaw externally
    /// applied to the PoseController's transform relative to the unity world.
    /// </summary>
    private Matrix4x4 m_unityWorldTransformOffset_T_unityWorld = Matrix4x4.identity;

    /// <summary>
    /// Reference to TangoApplication object.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// An enumeration of selection modes for the base frame from which TangoPoseController
    /// will calculate tango pose => transform updates.
    /// </summary>
    public enum BaseFrameSelectionModeEnum
    {
        AUTO_DETECT,
        USE_START_OF_SERVICE,
        USE_AREA_DESCRIPTION,
    }

    /// <summary>
    /// Gets the timestamp of the last pose.
    /// </summary>
    /// <value>The timestamp of the last pose.</value>
    public double LastPoseTimestamp { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the target frame is the color camera, otherwise presumed to be device.
    /// </summary>
    private bool IsTargetingColorCamera
    {
        get
        {
            return m_tangoARScreen != null && m_tangoARScreen.IsRendering;
        }
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        if (m_tangoApplication == null)
        {
            Debug.LogError("An instance of TangoApplication was not found in the scene.");
        }

        m_tangoARScreen = GetComponent<TangoARScreen>();
        m_characterController = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Update is called every frame.
    /// </summary>
    public void Update()
    {
        _UpdateTransformOffset();
        _UpdatePose();
    }

    /// <summary>
    /// Updates the offset matrix that tracks external changes to the transform of attached GameObject.
    /// </summary>
    private void _UpdateTransformOffset()
    {
        Quaternion deviceRotation =
            Quaternion.LookRotation(m_unityWorld_T_unityCamera.GetColumn(2), m_unityWorld_T_unityCamera.GetColumn(1));

        Quaternion deviceRotationWithTransformYaw =
            Quaternion.Euler(deviceRotation.eulerAngles.x, transform.eulerAngles.y, deviceRotation.eulerAngles.z);

        // Calculate offset based on the difference between the last pose and the transform, filtering out pitch and roll.
        m_unityWorldTransformOffset_T_unityWorld = Matrix4x4.TRS(transform.position, deviceRotationWithTransformYaw, Vector3.one) *
            m_unityWorld_T_unityCamera.inverse;
    }

    /// <summary>
    /// Updates the transformation to the latest pose.
    /// </summary>
    private void _UpdatePose()
    {
        // Query a new pose.
        TangoPoseData pose = new TangoPoseData();
        double queryTimestamp = IsTargetingColorCamera ? m_tangoARScreen.m_screenUpdateTime : 0.0f;
        PoseProvider.GetPoseAtTime(pose, queryTimestamp, _GetFramePair());

        // Do not update with invalide poses.
        if (pose.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
        {
            return;
        }

        // Do not update if the last update was for the same timestamp.
        if (pose.timestamp == LastPoseTimestamp)
        {
            return;
        }

        LastPoseTimestamp = pose.timestamp;

        DMatrix4x4 globalTLocal;
        if (!m_tangoApplication.GetGlobalTLocal(out globalTLocal))
        {
            Debug.LogError("Unable to obtain GlobalTLocal from TangoApplication.");
            return;
        }

        DMatrix4x4 unityWorld_T_device =
            DMatrix4x4.FromMatrix4x4(TangoSupport.UNITY_WORLD_T_START_SERVICE) * globalTLocal.Inverse * DMatrix4x4.TR(pose.translation, pose.orientation);

        // Calculate matrix for the camera in the Unity world
        if (IsTargetingColorCamera)
        {
            m_unityWorld_T_unityCamera =
                unityWorld_T_device.ToMatrix4x4() * TangoSupport.COLOR_CAMERA_T_UNITY_CAMERA * TangoSupport.m_colorCameraPoseRotation;
        }
        else
        {
            m_unityWorld_T_unityCamera =
                unityWorld_T_device.ToMatrix4x4() * TangoSupport.DEVICE_T_UNITY_CAMERA * TangoSupport.m_devicePoseRotation;
        }

        // Extract final position and rotation from matrix.
        Matrix4x4 unityWorldOffset_T_unityCamera = m_unityWorldTransformOffset_T_unityWorld * m_unityWorld_T_unityCamera;
        Vector3 finalPosition = unityWorldOffset_T_unityCamera.GetColumn(3);
        Quaternion finalRotation = Quaternion.LookRotation(unityWorldOffset_T_unityCamera.GetColumn(2), unityWorldOffset_T_unityCamera.GetColumn(1));

        // Filter out yaw if the clutch is enabled.
        if (m_clutchEnabled)
        {
            finalPosition = transform.position;
            finalRotation = Quaternion.Euler(finalRotation.eulerAngles.x, transform.eulerAngles.y, finalRotation.eulerAngles.z);
        }

        // Apply the final position.
        if (m_characterController)
        {
            m_characterController.Move(finalPosition - transform.position);
        }
        else
        {
            transform.position = finalPosition;
        }

        transform.rotation = finalRotation;
    }

    /// <summary>
    /// Gets the current tango coordinate frame pair used for pose queries based on controller options.
    /// </summary>
    /// <returns>The current tango coordinate frame pair.</returns>
    private Tango.TangoCoordinateFramePair _GetFramePair()
    {
        Tango.TangoCoordinateFramePair framePair;

        if (m_baseFrameMode == BaseFrameSelectionModeEnum.AUTO_DETECT && m_tangoApplication.m_enableCloudADF)
        {
            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_GLOBAL_WGS84;
        }
        else if ((m_baseFrameMode == BaseFrameSelectionModeEnum.AUTO_DETECT && m_tangoApplication.m_enableAreaDescriptions) ||
                (m_baseFrameMode == BaseFrameSelectionModeEnum.USE_AREA_DESCRIPTION))
        {
            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION;
        }
        else
        {
            framePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
        }

        if (IsTargetingColorCamera)
        {
            framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
        }
        else
        {
            framePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        }

        return framePair;
    }
}
