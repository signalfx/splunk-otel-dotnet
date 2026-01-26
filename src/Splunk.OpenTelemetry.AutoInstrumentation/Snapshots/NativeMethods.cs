// <copyright file="NativeMethods.cs" company="Splunk Inc.">
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
using System.Reflection;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

internal static class NativeMethods
{
    private static readonly ILogger Log = new Logger();

    static NativeMethods()
    {
        try
        {
            var nativeMethodsType = Type.GetType("OpenTelemetry.AutoInstrumentation.NativeMethods, OpenTelemetry.AutoInstrumentation");
            if (nativeMethodsType == null)
            {
                throw new Exception("OpenTelemetry.AutoInstrumentation.NativeMethods could not be found.");
            }

            var startMethod = nativeMethodsType.GetMethod("SelectiveSamplingStart", BindingFlags.Static | BindingFlags.Public, null, [typeof(ActivityTraceId)], null);
            var stopMethod = nativeMethodsType!.GetMethod("SelectiveSamplingStop", BindingFlags.Static | BindingFlags.Public, null, [typeof(ActivityTraceId)], null);

            StartSamplingDelegate = (Action<ActivityTraceId>)Delegate.CreateDelegate(typeof(Action<ActivityTraceId>), startMethod!);
            StopSamplingDelegate = (Action<ActivityTraceId>)Delegate.CreateDelegate(typeof(Action<ActivityTraceId>), stopMethod!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize sampling methods");
        }
    }

    public static Action<ActivityTraceId>? StopSamplingDelegate { get; }

    public static Action<ActivityTraceId>? StartSamplingDelegate { get; }
}
