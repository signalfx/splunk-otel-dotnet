// <copyright file="Program.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Diagnostics;
#if NET
using Microsoft.AspNetCore.Builder;
#endif
using My.Custom.Test.Namespace;

ActivitySource activitySource = new("TestApplication.ContinuousProfiler", "1.0.0");

#if NET
if (args.Contains("--runtime-config-server", StringComparer.Ordinal))
{
    await RunRuntimeConfigServer(args, activitySource);
    return;
}
#endif

using var activity = activitySource.StartActivity();
ClassA.MethodA();

#if NET
static async Task RunRuntimeConfigServer(string[] args, ActivitySource activitySource)
{
    var builderArgs = args.Where(arg => !string.Equals(arg, "--runtime-config-server", StringComparison.Ordinal)).ToArray();
    var builder = WebApplication.CreateBuilder(builderArgs);
    var app = builder.Build();

    app.UseWelcomePage("/alive-check");
    app.MapGet("/work", () =>
    {
        using var activity = activitySource.StartActivity("runtime-config-profiler-work");
        ClassA.MethodA();
        return "Work complete";
    });
    app.MapGet("/shutdown", () =>
    {
        app.Lifetime.StopApplication();
        return "Stopping";
    });

    await app.RunAsync();
}
#endif
