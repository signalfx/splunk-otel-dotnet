// <copyright file="NativeContinuousProfilerConfigurator.cs" company="Splunk Inc.">
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

using System.Reflection;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class NativeContinuousProfilerConfigurator
{
    private static readonly ILogger Log = new Logger();
    private static readonly Lazy<MethodInfo?> ConfigureNativeContinuousProfilerMethod = new(ResolveConfigureNativeContinuousProfilerMethod);

    public static void Configure(ProfilerRuntimeSettings settings)
    {
        var method = ConfigureNativeContinuousProfilerMethod.Value;
        if (method == null)
        {
            Log.Warning("Could not resolve native continuous profiler reconfiguration method.");
            return;
        }

        try
        {
            method.Invoke(
                null,
                [
                    settings.CpuProfilerEnabled,
                    settings.CpuProfilerCallStackInterval,
#if NET
                    settings.MemoryProfilerEnabled,
                    settings.MemoryProfilerMaxMemorySamplesPerMinute,
#else
                    false,
                    0u,
#endif
                    settings.SnapshotsEnabled ? settings.SnapshotsSamplingInterval : 0u
                ]);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reconfigure native continuous profiler.");
        }
    }

    private static MethodInfo? ResolveConfigureNativeContinuousProfilerMethod()
    {
        var nativeMethodsType = Type.GetType("OpenTelemetry.AutoInstrumentation.NativeMethods, OpenTelemetry.AutoInstrumentation");
        return nativeMethodsType?.GetMethod(
            "ConfigureNativeContinuousProfiler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(bool), typeof(uint), typeof(bool), typeof(uint), typeof(uint)],
            modifiers: null);
    }
}
