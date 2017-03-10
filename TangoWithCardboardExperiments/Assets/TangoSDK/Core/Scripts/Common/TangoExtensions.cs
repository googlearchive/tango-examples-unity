//-----------------------------------------------------------------------
// <copyright file="TangoExtensions.cs" company="Google">
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

namespace TangoExtensions
{
    /// <summary>
    /// A container for extension methods used by the Tango SDK.
    /// </summary>
    internal static class TangoExtensions
    {
        /// <summary>
        /// Attempts to convert an object to type T and, if successful, calls the supplied callback with
        /// the converted object as an argument.
        /// </summary>
        /// <typeparam name="T">The output type of the conversion.</typeparam>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <param name="callback">The callback that will be invoked if the conversion was successful.</param>
        /// <returns>true if conversion was successful, false otherwise.</returns>
        public static bool SafeConvert<T>(this object objectToConvert, System.Action<T> callback) where T : class
        {
            T castResult = objectToConvert as T;
            if (castResult != null)
            {
                callback(castResult);
                return true;
            }

            return false;
        }
    }
}
