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
using UnityEngine;

/// <summary>
/// Abstract base class for anything that should be
/// draw in the OnRenderImage process.
/// </summary>
public abstract class IBaseOnRenderImage : MonoBehaviour 
{
    /// <summary>
    /// Abstract post process function from source texture to destination.
    /// </summary>
    /// <param name="source"> Source render texture. </param>
    /// <param name="destination"> Destination render texture. </param>
    public abstract void OnRenderImage(RenderTexture source, 
                                       RenderTexture destination);
}
