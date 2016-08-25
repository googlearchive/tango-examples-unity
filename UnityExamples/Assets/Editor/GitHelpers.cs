//-----------------------------------------------------------------------
// <copyright file="GitHelpers.cs" company="Google">
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
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Git helpers.
/// </summary>
public class GitHelpers
{
    private static readonly string SHELL_SCRIPT_DIRECTORY = "/Editor/ShellScripts/";
    private static readonly string HASH_PRETTY = "git_pretty_hash.sh";
    private static readonly string HASH_FULL = "git_full_hash.sh";
    private static readonly string REMOTE_BRANCH_NAME = "git_remote_branch_name.sh";
    private static readonly string TAG_INFO = "git_tag_name.sh";

    /// <summary>
    /// Gets the full git hash.
    /// </summary>
    /// <returns>The full git hash.</returns>
    public static string GetFullGitHash()
    {
        return _LaunchProcessWithOutput(Application.dataPath + SHELL_SCRIPT_DIRECTORY + HASH_FULL);
    }

    /// <summary>
    /// Gets the pretty git hash.
    /// </summary>
    /// <returns>The pretty git hash.</returns>
    public static string GetPrettyGitHash()
    {
        return _LaunchProcessWithOutput(Application.dataPath + SHELL_SCRIPT_DIRECTORY + HASH_PRETTY);
    }

    /// <summary>
    /// Gets the name of the remote branch.
    /// </summary>
    /// <returns>The remote branch name or "undefined" if there is no remote branch.</returns>
    public static string GetRemoteBranchName()
    {
        string rawName = _LaunchProcessWithOutput(Application.dataPath + SHELL_SCRIPT_DIRECTORY + REMOTE_BRANCH_NAME);
        if (rawName == String.Empty)
        {
            return "undefined";
        }
        else
        {
            return rawName;
        }
    }

    /// <summary>
    /// Gets the tag info.
    /// </summary>
    /// <returns>The tag info.</returns>
    public static string GetTagInfo()
    {
        return _LaunchProcessWithOutput(Application.dataPath + SHELL_SCRIPT_DIRECTORY + TAG_INFO);
    }

    /// <summary>
    /// Launch a process and return what is printed to standard output as a string.
    /// </summary>
    /// <returns>Output for the launched process.</returns>
    /// <param name="process">Command line to run.</param>
    private static string _LaunchProcessWithOutput(string process)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = process;
        psi.WindowStyle = ProcessWindowStyle.Normal;
        psi.UseShellExecute = false; 
        psi.RedirectStandardOutput = true;

        Process p = Process.Start(psi); 
        string processOutput = p.StandardOutput.ReadToEnd().Replace(Environment.NewLine, string.Empty);
        p.WaitForExit();

        return processOutput;
    }
}