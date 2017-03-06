//-----------------------------------------------------------------------
// <copyright file="TangoEnvironmentalLighting.cs" company="Google">
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tango;
using UnityEngine;

/// <summary>
/// TangoEnvironmentalLighting computes the spherical harmonic coefficients for the
/// diffuse environment map and passes the specular and diffuse data for image
/// based lighting materials.
///
/// If set, materials that use an image-based lighting shader such as
/// EnvironmentalLighting will work.
/// Note 1: Must have both TextureID and Raw Bytes enabled under Video
/// Overlay in the Tango Manager.
/// </summary>
public class TangoEnvironmentalLighting : MonoBehaviour, ITangoVideoOverlay, ITangoLifecycle
{
    /// <summary>
    /// Enable environmental lighting toggling.
    /// </summary>
    public bool m_enableDebugUI = false;

    /// <summary>
    /// Defines if environmental lighting is enabled. Enabled by default.
    /// </summary>
    public bool m_enableEnvironmentalLighting = true;

    /// <summary>
    /// Constants used to compute the spherical harmonic diffuse lighting
    /// matrix.
    /// </summary>
    private const float C1 = 0.492043f;
    private const float C2 = 0.511664f;
    private const float C3 = 0.743125f;
    private const float C4 = 0.886227f;
    private const float C5 = 0.247708f;

    /// <summary>
    /// Emulation constants.
    /// </summary>
    private const int EMULATED_CAMERA_WIDTH = 1280;
    private const int EMULATED_CAMERA_HEIGHT = 720;

    /// <summary>
    /// The square root of the number of samples used to compute the diffuse
    /// image based lighting buffer.
    /// </summary>
    private const int SQRT_N_SAMPLES = 50;

    /// <summary>
    /// The number of spherical harmonic bands used to approximate the diffuse
    /// lighting.
    /// </summary>
    private const int LEVELS = 3;

    /// <summary>
    /// The texture used for the specular lighting.
    /// </summary>
    private Texture m_environmentMap;

    /// <summary>
    /// The diffuse lighting samples with (theta, phi) coordinates and weighted
    /// coefficients for sampling.
    /// </summary>
    private SphericalHarmonicSample[] m_samples;

    /// <summary>
    /// The diffuse spherical harmonic coefficients for each band and order.
    /// </summary>
    private Vector3[] m_coefficients;

    /// <summary>
    /// Displays the button for toggling environmental lighting if debug is
    /// enabled.
    /// </summary>
    public void OnGUI()
    {
        if (m_enableDebugUI)
        {
            if (GUI.Button(new Rect(10, 10, 600, 100),
                    "<size=40>Toggle Environmental Lighting</size>"))
            {
                m_enableEnvironmentalLighting = !m_enableEnvironmentalLighting;
            }
        }
    }

    /// <summary>
    /// Awake for TangoEnvironmentalLighting. Compute the coefficients,
    /// polar coordinates, and Cartesian coordinates to be sampled.
    /// </summary>
    public void Awake()
    {
        m_samples = new SphericalHarmonicSample[SQRT_N_SAMPLES * SQRT_N_SAMPLES];
        int numCoefficients = LEVELS * LEVELS;
        m_coefficients = new Vector3[numCoefficients];
        for (int n = 0; n < numCoefficients; ++n)
        {
            m_coefficients[n] = Vector3.zero;
        }

        int i = 0;
        float oneOverN = 1.0f / SQRT_N_SAMPLES;
        for (int a = 0; a < SQRT_N_SAMPLES; ++a)
        {
            for (int b = 0; b < SQRT_N_SAMPLES; ++b)
            {
                SphericalHarmonicSample sample;
                float x = a * oneOverN;
                float y = b * oneOverN;
                float theta = 2.0f * Mathf.Cos(Mathf.Sqrt(1.0f - x));
                float phi = 2.0f * Mathf.PI * y;
                sample.sph = new Vector2(theta, phi);

                // Convert spherical coords to unit vector.
                Vector3 vec = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi),
                                          Mathf.Sin(theta) * Mathf.Sin(phi),
                                          Mathf.Cos(theta));
                sample.vec = vec;

                // Precompute all SH coefficients for this sample.
                sample.coeff = new float[numCoefficients];
                for (int l = 0; l < LEVELS; ++l)
                {
                    for (int m = -l; m <= l; ++m)
                    {
                        int index = (l * (l + 1)) + m;
                        sample.coeff[index] = SH(l, m, theta, phi);
                    }
                }

                m_samples[i] = sample;
                ++i;
            }
        }
    }

    /// <summary>
    /// Initialize the Environmental Lighting controller.
    /// </summary>
    public void Start()
    {
        TangoApplication tangoApplication = FindObjectOfType<TangoApplication>();
        if (tangoApplication != null)
        {
            tangoApplication.Register(this);
        }
    }

    /// <summary>
    /// Called on scene update.
    /// </summary>
    public void Update()
    {
        if (m_enableEnvironmentalLighting && m_environmentMap != null)
        {
            API.TangoUnity_updateEnvironmentMap(m_environmentMap.GetNativeTexturePtr().ToInt32(),
                                                m_environmentMap.width,
                                                m_environmentMap.height);

            // Rendering the latest frame changes a bunch of OpenGL state.  Ensure Unity knows the current OpenGL 
            // state.
            GL.InvalidateState();

            Shader.SetGlobalTexture("_TangoLightingEnvironmentMap", m_environmentMap);
        }
        else
        {
            Shader.SetGlobalFloat("_TangoLightingExposure", 0);
        }
    }

    /// <summary>
    /// This will be called when a new frame is available from the camera.
    /// </summary>
    /// <param name="cameraId">Camera identifier.</param>
    /// <param name="imageBuffer">Tango camera image buffer.</param>
    public void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId, TangoUnityImageData imageBuffer)
    {
        _ComputeDiffuseCoefficients(imageBuffer);
    }
    
    /// <summary>
    /// This is called when the permission-granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
    }

    /// <summary>
    /// This is called when successfully connected to the Tango Service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
#if UNITY_EDITOR
        // Format needs to be ARGB32 in editor to use Texture2D.ReadPixels() in emulation
        // in Unity 4.6.
        RenderTexture environmentMap = new RenderTexture(EMULATED_CAMERA_WIDTH, EMULATED_CAMERA_HEIGHT, 0, RenderTextureFormat.ARGB32);
        environmentMap.Create();

        m_environmentMap = environmentMap;
#else
        TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
        VideoOverlayProvider.GetIntrinsics(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics);
        m_environmentMap = new Texture2D((int)intrinsics.width, (int)intrinsics.height, TextureFormat.RGBA32, false);
#endif
    }

    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// <summary>
    /// Computes the spherical harmonic diffuse coefficients for a given
    /// TangoImageBuffer.
    /// </summary>
    /// <param name="imageBuffer">The TangoImageBuffer to sample.</param>
    private void _ComputeDiffuseCoefficients(TangoUnityImageData imageBuffer)
    {
        if (m_enableEnvironmentalLighting)
        {
            // Compute SH Coefficients.
            float weight = 4.0f * Mathf.PI;
            int numSamples = m_samples.Length;
            int numCoefficients = m_coefficients.Length;
            for (int coeffIdx = 0; coeffIdx < numCoefficients; ++coeffIdx)
            {
                m_coefficients[coeffIdx] = Vector3.zero;
            }

            for (int sampleIdx = 0; sampleIdx < numSamples; ++sampleIdx)
            {
                float theta = m_samples[sampleIdx].sph.x;
                float phi = m_samples[sampleIdx].sph.y;

                // Normalize between 0 and 1.
                float x = 1.0f - Mathf.Pow(Mathf.Cos(theta / 2.0f), 2.0f);
                float y = phi / Mathf.PI / 2.0f;

                int i = (int)(imageBuffer.height * x);
                int j = (int)(imageBuffer.width * y);

                Vector3 rgb = _GetRgbFromImageBuffer(imageBuffer, i, j);
                for (int coeffIdx = 0; coeffIdx < numCoefficients; ++coeffIdx)
                {
                    m_coefficients[coeffIdx] += rgb * m_samples[sampleIdx].coeff[coeffIdx];
                }
            }

            // Divide the result by weight and number of samples.
            float factor = weight / numSamples;
            for (int coeffIdx = 0; coeffIdx < numCoefficients; ++coeffIdx)
            {
                m_coefficients[coeffIdx] *= factor;
            }

            Shader.SetGlobalMatrix("_TangoLightingSphericalHarmonicMatrixR", _SetShmMatrix(0));
            Shader.SetGlobalMatrix("_TangoLightingSphericalHarmonicMatrixG", _SetShmMatrix(1));
            Shader.SetGlobalMatrix("_TangoLightingSphericalHarmonicMatrixB", _SetShmMatrix(2));
            Shader.SetGlobalFloat("_TangoLightingExposure", m_coefficients[0].magnitude);
        }
    }

    /// <summary>
    /// Returns the _Factorial at an index <c>num</c>.
    /// </summary>
    /// <param name="num">The index of the desired factorial value.</param>
    /// <returns>The resulting factorial.</returns>
    private float _Factorial(int num)
    {
        float result = 1.0f;
        for (int i = 2; i <= num; ++i)
        {
            result *= i;
        }

        return result;
    }

    /// <summary>
    /// Evaluate an Associated Legendre Polynomial P(l, m, x) at x.
    ///
    /// Referenced from the following paper:
    /// <![CDATA[http://www.research.scea.com/gdc2003/spherical-harmonic-lighting.pdf]]>.
    /// </summary>
    /// <param name="l">The spherical harmonic band level. Range from [0..N].</param>
    /// <param name="m">The order at band level l. Range from [-l..l].</param>
    /// <param name="x">The point along the polynomial.</param>
    /// <returns>The value on the polynomial at point x.</returns>
    private float P(int l, int m, float x)
    {
        float pmm = 1.0f;
        if (m > 0)
        {
            float somx2 = Mathf.Sqrt((1.0f - x) * (1.0f + x));
            float fact = 1.0f;
            for (int i = 1; i <= m; ++i)
            {
                pmm *= (-fact) * somx2;
                fact += 2.0f;
            }
        }

        if (l == m)
        {
            return pmm;
        }

        float pmmp1 = x * ((2.0f * m) + 1.0f) * pmm;
        if (l == m + 1)
        {
            return pmmp1;
        }

        float pll = 0.0f;
        for (int ll = m + 2; ll <= l; ++ll)
        {
            pll = ((((2.0f * ll) - 1.0f) * x * pmmp1) - ((ll + m - 1.0f) * pmm)) / (ll - m);
            pmm = pmmp1;
            pmmp1 = pll;
        }

        return pll;
    }

    /// <summary>
    /// Renormalization constant for SH function.
    ///
    /// Referenced from the following paper:
    /// <![CDATA[http://www.research.scea.com/gdc2003/spherical-harmonic-lighting.pdf]]>.
    /// </summary>
    /// <param name="l">The spherical harmonic band level. Range from [0..N].</param>
    /// <param name="m">The order at band level l. Range from [-l..l].</param>
    /// <returns>The renormalization constant for the basis function.</returns>
    private float K(int l, int m)
    {
        float temp = (((2.0f * l) + 1.0f) * _Factorial(l - m)) / (4.0f * Mathf.PI * _Factorial(l + m));
        return Mathf.Sqrt(temp);
    }

    /// <summary>
    /// Return a point sample of a Spherical Harmonic basis function.
    /// This function returns the weights of a certain (theta, phi) at a given
    /// level, list, and order, m.
    ///
    /// P is the orthogonal basis function.
    /// K is the scaling factor to normalize the function.
    ///
    /// Referenced from the following paper:
    /// <![CDATA[http://www.research.scea.com/gdc2003/spherical-harmonic-lighting.pdf]]>.
    /// </summary>
    /// <param name="l">The spherical harmonic band level. Range from [0..N].</param>
    /// <param name="m">The order at band level l. Range from [-l..l].</param>
    /// <param name="theta">Range from [0..Pi].</param>
    /// <param name="phi">Range from [0..2*Pi].</param>
    /// <returns>The weighted coefficient of this spherical harmonic point.</returns>
    private float SH(int l, int m, float theta, float phi)
    {
        float sqrt2 = Mathf.Sqrt(2.0f);
        if (m == 0)
        {
            return K(l, 0) * P(l, m, Mathf.Cos(theta));
        }

        if (m > 0)
        {
            return sqrt2 * K(l, m) * Mathf.Cos(m * phi) * P(l, m, Mathf.Cos(theta));
        }

        return sqrt2 * K(l, -m) * Mathf.Sin(-m * phi) * P(l, -m, Mathf.Cos(theta));
    }

    /// <summary>
    /// Returns the RGB value at a given theta and phi given a TangoImageBuffer.
    /// </summary>
    /// <param name="buffer">The TangoImageBuffer to sample.</param>
    /// <param name="i">Range from [0..height].</param>
    /// <param name="j">Range from [0..width].</param>
    /// <returns>The RGB value on the buffer at the given theta and phi.</returns>
    private Vector3 _GetRgbFromImageBuffer(Tango.TangoUnityImageData buffer, int i, int j)
    {
        int width = (int)buffer.width;
        int height = (int)buffer.height;
        int uv_buffer_offset = width * height;

        int x_index = j;
        if (j % 2 != 0)
        {
            x_index = j - 1;
        }

        // Get the YUV color for this pixel.
        int yValue = buffer.data[(i * width) + j];
        int uValue = buffer.data[uv_buffer_offset + ((i / 2) * width) + x_index + 1];
        int vValue = buffer.data[uv_buffer_offset + ((i / 2) * width) + x_index];

        // Convert the YUV value to RGB.
        float r = yValue + (1.370705f * (vValue - 128));
        float g = yValue - (0.689001f * (vValue - 128)) - (0.337633f * (uValue - 128));
        float b = yValue + (1.732446f * (uValue - 128));
        Vector3 result = new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);

        // Gamma correct color to linear scale.
        result.x = Mathf.Pow(Mathf.Max(0.0f, result.x), 2.2f);
        result.y = Mathf.Pow(Mathf.Max(0.0f, result.y), 2.2f);
        result.z = Mathf.Pow(Mathf.Max(0.0f, result.z), 2.2f);
        return result;
    }

    /// <summary>
    /// Compute the spherical harmonic matrix representation of a given RGB
    /// channel.
    ///
    /// Referenced from the following paper:
    /// <![CDATA[https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf]]>.
    /// </summary>
    /// <param name="i">The channel to compute. R is 0, G is 1, B is 2.</param>
    /// <returns>The coefficient matrix that encodes the color at given spherical harmonic points.</returns>
    private Matrix4x4 _SetShmMatrix(int i)
    {
        Matrix4x4 matrix = new Matrix4x4();
        Vector4 col0 = new Vector4(C1 * m_coefficients[8][i],
                                   C1 * m_coefficients[4][i],
                                   C1 * m_coefficients[7][i],
                                   C2 * m_coefficients[3][i]);
        Vector4 col1 = new Vector4(C1 * m_coefficients[4][i],
                                   -C1 * m_coefficients[8][i],
                                   C1 * m_coefficients[5][i],
                                   C2 * m_coefficients[1][i]);
        Vector4 col2 = new Vector4(C1 * m_coefficients[7][i],
                                   C1 * m_coefficients[5][i],
                                   C3 * m_coefficients[6][i],
                                   C2 * m_coefficients[2][i]);
        Vector4 col3 = new Vector4(C2 * m_coefficients[3][i],
                                   C2 * m_coefficients[1][i],
                                   C2 * m_coefficients[2][i],
                                   (C4 * m_coefficients[0][i]) - (C5 * m_coefficients[6][i]));
        matrix.SetColumn(0, col0);
        matrix.SetColumn(1, col1);
        matrix.SetColumn(2, col2);
        matrix.SetColumn(3, col3);
        return matrix;
    }

    /// <summary>
    /// The API for C level system calls.
    /// </summary>
    private struct API
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport(Common.TANGO_UNITY_DLL)]
        public static extern void TangoUnity_updateEnvironmentMap(Int32 glTextureId, int width, int height);
#else
        /// <summary>
        /// Updates the specular environment map.
        /// </summary>
        /// <param name="glTextureId">Gl texture identifier.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        public static void TangoUnity_updateEnvironmentMap(Int32 glTextureId, int width, int height)
        {
        }
#endif
    }

    /// <summary>
    /// A single sample point with its polar coordinates, Cartesian
    /// coordinates, and its weight for each spherical harmonic level and
    /// order.
    /// </summary>
    private struct SphericalHarmonicSample
    {
        public Vector2 sph;
        public Vector3 vec;
        public float[] coeff;
    }
}
