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
using System.Linq;
using System.IO;
using UnityEngine;

/// <summary>
/// Static class to perform following file operations
/// - Retreive list of files from a directory.
/// </summary>
public static class FileAccessUtilities
{
    /// <summary>
    /// Used to retreive a list of files in the directory.
    /// </summary>
    /// <param name="directory"> Directory to retreive files from.</param>
    /// <param name="ignorePattern"> Ignores certain files from the list of files found if needed.
    /// Eg: if ignorePattern is ".txt", it ignores all text files.</param>
    /// <returns> List of files found in directory.</returns>
    public static string[] RetrieveFilesList(string directory, string ignorePattern)
    {
        string[] temp_Files = Directory.GetFiles(directory);

        // this loop separates file name from full path
        for (int i = temp_Files.Length - 1; i >= 0; i--)
        {
            temp_Files[i] = Path.GetFileName(temp_Files[i]);
        }

        if (ignorePattern != string.Empty)
        {
            // filter out .txt files and don't display them
            foreach (string fileName in temp_Files)
            {
                if (fileName.Contains(ignorePattern))
                {
                    temp_Files = temp_Files.Where(name => name != fileName).ToArray();
                }
            }
        }
        return temp_Files;
    }
}
