/*
 * Copyright 2014 Google Inc. All Rights Reserved.
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
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Helper class for Tango.
/// </summary>
public static class Helper 
{
    static public bool usingCLevelCode = false;
    
    /// <summary>
    /// Filter for depth occlusion use.
    /// </summary>
    /// <param name="sourceArr">The source array of depth.</param>
    /// <param name="destArr">The destination array of depth.</param>
    /// <param name="level">Level of rounds to filter.</param>
    public static void Filter(float[] sourceArr, float[] destArr, int level)
    {
        GCHandle sourceHandler = GCHandle.Alloc(sourceArr, GCHandleType.Pinned);
        GCHandle destHandler  = GCHandle.Alloc(destArr, GCHandleType.Pinned);
        DepthNoiseFilter(sourceHandler.AddrOfPinnedObject(), destHandler.AddrOfPinnedObject(), level);
        destHandler.Free();
        sourceHandler.Free();
    } 
    
    /// <summary>
    /// Native filter function import.
    /// </summary>
    /// <param name="srouce">The source array ptr of depth.</param>
    /// <param name="dest">The destination array ptr of depth.</param>
    /// <param name="level">Level of rounds to filter.</param>
    [DllImport("TangoHelpers")]
    public static extern void DepthNoiseFilter(System.IntPtr srouce, System.IntPtr dest, int level);
}
