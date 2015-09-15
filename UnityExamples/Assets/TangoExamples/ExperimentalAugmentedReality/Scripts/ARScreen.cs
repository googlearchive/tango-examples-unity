//-----------------------------------------------------------------------
// <copyright file="ARScreen.cs" company="Google">
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
using System.Collections;
using UnityEngine;
using Tango;

/// <summary>
/// ARScreen takes the YUV image from the API, resize the image plane and passes
/// the YUV data and vertices data to the YUV2RGB shader to produce a properly
/// sized RGBA image.
/// 
/// Please note that all the YUV to RGB conversion is done through the YUV2RGB
/// shader, no computation is in this class, this class only passes the data to
/// shader.
/// </summary>
public class ARScreen : MonoBehaviour
{
    public Camera m_renderCamera;
    public Material m_screenMaterial;

    // Values for debug display.
    [HideInInspector]
    public TangoEnums.TangoPoseStatusType m_status;
    [HideInInspector]
    public int m_frameCount;
    
    private TangoApplication m_tangoApplication;
    private YUVTexture m_textures;

    // Matrix for Tango coordinate frame to Unity coordinate frame conversion.
    // Start of service frame with respect to Unity world frame.
    private Matrix4x4 m_uwTss;

    // Unity camera frame with respect to color camera frame.
    private Matrix4x4 m_cTuc;

    // Device frame with respect to IMU frame.
    private Matrix4x4 m_imuTd;

    // Color camera frame with respect to IMU frame.
    private Matrix4x4 m_imuTc;

    // Unity camera frame with respect to IMU frame, this is composed by
    // Matrix4x4.Inverse(m_imuTd) * m_imuTc * m_cTuc;
    // We pre-compute this matrix to save some computation in update().
    private Matrix4x4 m_dTuc;

    /// <summary>
    /// Initialize the AR Screen.
    /// </summary>
    private void Start()
    {
        // Constant matrix converting start of service frame to Unity world frame.
        m_uwTss = new Matrix4x4();
        m_uwTss.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        m_uwTss.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
        m_uwTss.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        
        // Constant matrix converting Unity world frame frame to device frame.
        m_cTuc.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        m_cTuc.SetColumn(1, new Vector4(0.0f, -1.0f, 0.0f, 0.0f));
        m_cTuc.SetColumn(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        m_cTuc.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        m_tangoApplication = FindObjectOfType<TangoApplication>();
        
        if (m_tangoApplication != null)
        {
            if (AndroidHelper.IsTangoCorePresent())
            {
                // Request Tango permissions
                m_tangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
                m_tangoApplication.RequestNecessaryPermissionsAndConnect();
                m_tangoApplication.Register(this);
            }
            else
            {
                // If no Tango Core is present let's tell the user to install it.
                Debug.Log("Tango Core is outdated.");
            }
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }
        if (m_tangoApplication != null)
        {
            m_textures = m_tangoApplication.GetVideoOverlayTextureYUV();

            // Pass YUV textures to shader for process.
            m_screenMaterial.SetTexture("_YTex", m_textures.m_videoOverlayTextureY);
            m_screenMaterial.SetTexture("_UTex", m_textures.m_videoOverlayTextureCb);
            m_screenMaterial.SetTexture("_VTex", m_textures.m_videoOverlayTextureCr);
        }
        
        m_tangoApplication.Register(this);
    }

    /// <summary>
    /// Unity update function, we update our texture from here.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_tangoApplication != null)
            {
                m_tangoApplication.Shutdown();
            }
            
            // This is a temporary fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application immediately,
            // results in a hard crash.
            AndroidHelper.AndroidQuit();
        }
        double timestamp = VideoOverlayProvider.RenderLatestFrame(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);
        _UpdateTransformation(timestamp);
        GL.InvalidateState();
    }

    /// <summary>
    /// This callback function is called after user appoved or declined the permission to use Motion Tracking.
    /// </summary>
    /// <param name="permissionsGranted">If the permissions were granted.</param>
    private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            m_tangoApplication.InitApplication();
            m_tangoApplication.InitProviders(string.Empty);
            m_tangoApplication.ConnectToService();
            
            // Ask ARScreen to query the camera intrinsics from Tango Service.
            _SetCameraIntrinsics();
            _SetCameraExtrinsics();
        }
        else
        {
            AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
        }
    }

    /// <summary>
    /// Update the camera gameobject's transformation to the pose that on current timestamp.
    /// </summary>
    /// <param name="timestamp">Time to update the camera to.</param>
    private void _UpdateTransformation(double timestamp)
    {
        TangoPoseData pose = new TangoPoseData();
        TangoCoordinateFramePair pair;
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        PoseProvider.GetPoseAtTime(pose, timestamp, pair);
        
        m_status = pose.status_code;
        if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
        {
            Vector3 m_tangoPosition = new Vector3((float)pose.translation[0],
                                                  (float)pose.translation[1],
                                                  (float)pose.translation[2]);
            
            Quaternion m_tangoRotation = new Quaternion((float)pose.orientation[0],
                                                        (float)pose.orientation[1],
                                                        (float)pose.orientation[2],
                                                        (float)pose.orientation[3]);
            
            Matrix4x4 ssTd = Matrix4x4.TRS(m_tangoPosition, m_tangoRotation, Vector3.one);
            
            // Here we are getting the pose of Unity camera frame with respect to Unity world.
            // This is the transformation of our current pose within the Unity coordinate frame.
            Matrix4x4 uwTuc = m_uwTss * ssTd * m_dTuc;
            
            // Extract new local position
            m_renderCamera.transform.position = uwTuc.GetColumn(3);
            
            // Extract new local rotation
            m_renderCamera.transform.rotation = Quaternion.LookRotation(uwTuc.GetColumn(2), uwTuc.GetColumn(1));
            m_frameCount++;
        }
        else
        {
            m_frameCount = 0;
        }
    }
    
    /// <summary>
    /// Set the screen (video overlay image plane) size and vertices. The image plane is not
    /// applying any project matrix or view matrix. So it's drawing space is the normalized
    /// screen space, that is [-1.0f, 1.0f] for both width and height.
    /// </summary>
    /// <param name="normalizedOffsetX">Horizontal padding to add to the left and right edges.</param>
    /// <param name="normalizedOffsetY">Vertical padding to add to top and bottom edges.</param> 
    private void _SetScreenVertices(float normalizedOffsetX, float normalizedOffsetY)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        mesh.Clear();
        
        // Set the vertices base on the offset, note that the offset is used to compensate
        // the ratio differences between the camera image and device screen.
        Vector3[] verts = new Vector3[4];
        verts[0] = new Vector3(-1.0f - normalizedOffsetX, -1.0f - normalizedOffsetY, 1.0f);
        verts[1] = new Vector3(-1.0f - normalizedOffsetX, 1.0f + normalizedOffsetY, 1.0f);
        verts[2] = new Vector3(1.0f + normalizedOffsetX, 1.0f + normalizedOffsetY, 1.0f);
        verts[3] = new Vector3(1.0f + normalizedOffsetX, -1.0f - normalizedOffsetY, 1.0f);
        
        // Set indices.
        int[] indices = new int[6];
        indices[0] = 0;
        indices[1] = 2;
        indices[2] = 3;
        indices[3] = 1;
        indices[4] = 2;
        indices[5] = 0;
        
        // Set UVs.
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, 1f);
        uvs[2] = new Vector2(1f, 1f);
        uvs[3] = new Vector2(1f, 0);
        
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.uv = uvs;
        meshFilter.mesh = mesh;
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// The function is for querying the camera extrinsic, for example: the transformation between
    /// IMU and device frame. These extrinsics is used to transform the pose from the color camera frame
    /// to the device frame. Because the extrinsic is being queried using the GetPoseAtTime()
    /// with a desired frame pair, it can only be queried after the ConnectToService() is called.
    ///
    /// The device with respect to IMU frame is not directly queryable from API, so we use the IMU
    /// frame as a temporary value to get the device frame with respect to IMU frame.
    /// </summary>
    private void _SetCameraExtrinsics()
    {
        double timestamp = 0.0;
        TangoCoordinateFramePair pair;
        TangoPoseData poseData = new TangoPoseData();
        
        // Getting the transformation of device frame with respect to IMU frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        Vector3 position = new Vector3((float)poseData.translation[0],
                                       (float)poseData.translation[1],
                                       (float)poseData.translation[2]);
        Quaternion quat = new Quaternion((float)poseData.orientation[0],
                                         (float)poseData.orientation[1],
                                         (float)poseData.orientation[2],
                                         (float)poseData.orientation[3]);
        m_imuTd = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));
        
        // Getting the transformation of IMU frame with respect to color camera frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
        PoseProvider.GetPoseAtTime(poseData, timestamp, pair);
        position = new Vector3((float)poseData.translation[0],
                               (float)poseData.translation[1],
                               (float)poseData.translation[2]);
        quat = new Quaternion((float)poseData.orientation[0],
                              (float)poseData.orientation[1],
                              (float)poseData.orientation[2],
                              (float)poseData.orientation[3]);
        m_imuTc = Matrix4x4.TRS(position, quat, new Vector3(1.0f, 1.0f, 1.0f));
        m_dTuc = Matrix4x4.Inverse(m_imuTd) * m_imuTc * m_cTuc;
    }
    
    /// <summary>
    /// Set up the size of ARScreen based on camera intrinsics.
    /// </summary>
    private void _SetCameraIntrinsics()
    {
        TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
        VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);
        
        float verticalFOV = 2.0f * Mathf.Rad2Deg * Mathf.Atan((intrinsics.height * 0.5f) / (float)intrinsics.fy);
        if (!float.IsNaN(verticalFOV))
        {
            m_renderCamera.projectionMatrix = 
                ProjectionMatrixForCameraIntrinsics((float)intrinsics.width,
                                                    (float)intrinsics.height,
                                                    (float)intrinsics.fx,
                                                    (float)intrinsics.fy,
                                                    (float)intrinsics.cx,
                                                    (float)intrinsics.cy,
                                                    0.1f, 1000.0f);

            // Here we are scaling the image plane to make sure the image plane's ratio is set as the
            // color camera image ratio.
            // If we don't do this, because we are drawing the texture fullscreen, the image plane will
            // be set to the screen's ratio.
            float widthRatio = (float)Screen.width / (float)intrinsics.width;
            float heightRatio = (float)Screen.height / (float)intrinsics.height;
            if (widthRatio >= heightRatio)
            {
                float normalizedOffset = ((widthRatio / heightRatio) - 1.0f) / 2.0f;
                _SetScreenVertices(0, normalizedOffset);
            }
            else
            {
                float normalizedOffset = ((heightRatio / widthRatio) - 1.0f) / 2.0f;
                _SetScreenVertices(normalizedOffset, 0);
            }
        }
    }

    /// <summary>
    /// Create a projection matrix from window size, camera intrinsics, and clip settings.
    /// </summary>
    /// <param name="width">The width of the camera image.</param>
    /// <param name="height">The height of the camera image.</param> 
    /// <param name="fx">The x-axis focal length of the camera.</param> 
    /// <param name="fy">The y-axis focal length of the camera.</param> 
    /// <param name="cx">The x-coordinate principal point in pixels.</param> 
    /// <param name="cy">The y-coordinate principal point in pixels.</param> 
    /// <param name="near">The desired near z-clipping plane.</param> 
    /// <param name="far">The desired far z-clipping plane.</param> 
    private Matrix4x4 ProjectionMatrixForCameraIntrinsics(float width, float height,
                                                          float fx, float fy,
                                                          float cx, float cy,
                                                          float near, float far) {
        float xscale = near / fx;
        float yscale = near / fy;
        
        float xoffset =  (cx - (width  / 2.0f)) * xscale;
        // OpenGL coordinates has y pointing downwards so we negate this term.
        float yoffset = -(cy - (height / 2.0f)) * yscale;
        
        return  Frustum(xscale * -width  / 2.0f - xoffset,
                        xscale *  width  / 2.0f - xoffset,
                        yscale * -height / 2.0f - yoffset,
                        yscale *  height / 2.0f - yoffset,
                        near, far);
    }

    /// <summary>
    /// This is function compute the projection matrix based on frustum size.
    /// This function's implementation is same as glFrustum.
    /// </summary>
    /// <param name="left">Specify the coordinates for the left vertical clipping planes.</param>
    /// <param name="right">Specify the coordinates for the right vertical clipping planes.</param> 
    /// <param name="bottom">Specify the coordinates for the bottom horizontal clipping planes.</param> 
    /// <param name="top">Specify the coordinates for the top horizontal clipping planes.</param> 
    /// <param name="zNear">Specify the distances to the near depth clipping planes. Both distances must be positive.</param> 
    /// <param name="zFar">Specify the distances to the far depth clipping planes. Both distances must be positive.</param> 
    private Matrix4x4 Frustum(float left,
                      float right,
                      float bottom,
                      float top,
                      float zNear,
                      float zFar) {
        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, new Vector4(2.0f * zNear / (right - left), 0.0f,                         (right + left) / (right - left) , 0.0f));
        m.SetRow(1, new Vector4(0.0f,                          2.0f * zNear/ (top - bottom), (top + bottom) / (top - bottom) , 0.0f));
        m.SetRow(2, new Vector4(0.0f,                          0.0f,                         -(zFar + zNear) / (zFar - zNear), -(2 * zFar * zNear) / (zFar - zNear)));
        m.SetRow(3, new Vector4(0.0f,                          0.0f,                         -1.0f, 0.0f));
        return m;
    }
}
