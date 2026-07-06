// <copyright file="UpstreamProfilerStateResolver.cs" company="Splunk Inc.">
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

using System.Collections;
using System.Reflection;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

internal static class UpstreamProfilerStateResolver
{
    private const BindingFlags StaticNonPublicFlags = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InstanceNonPublicFlags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly ILogger Log = new Logger();

    public static EffectiveProfilerFeatures Resolve()
    {
        try
        {
            var instrumentationType = UpstreamInstrumentationResolver.TryGetInstrumentationType();
            if (instrumentationType == null)
            {
                return WarnAndReturnDisabled("Instrumentation type was not found.");
            }

            var sampleExporterField = instrumentationType.GetField("_sampleExporter", StaticNonPublicFlags);
            if (sampleExporterField == null)
            {
                return WarnAndReturnDisabled("_sampleExporter field was not found.");
            }

            var sampleExporter = sampleExporterField.GetValue(null);
            if (sampleExporter == null)
            {
                return EffectiveProfilerFeatures.None;
            }

            var bufferProcessor = sampleExporter
                .GetType()
                .GetField("_bufferProcessor", InstanceNonPublicFlags)
                ?.GetValue(sampleExporter);
            if (bufferProcessor == null)
            {
                return WarnAndReturnDisabled("_bufferProcessor field was not found.");
            }

            var sampleHandlers = bufferProcessor
                .GetType()
                .GetField("_sampleHandlers", InstanceNonPublicFlags)
                ?.GetValue(bufferProcessor) as IDictionary;
            if (sampleHandlers == null)
            {
                return WarnAndReturnDisabled("_sampleHandlers field was not found or had an unexpected type.");
            }

            var profilerFeatures = EffectiveProfilerFeatures.None;

            foreach (var sampleType in sampleHandlers.Keys)
            {
                switch (sampleType?.ToString())
                {
                    case "Continuous":
                        profilerFeatures |= EffectiveProfilerFeatures.Cpu;
                        break;
                    case "Allocation":
#if NET
                        profilerFeatures |= EffectiveProfilerFeatures.Memory;
#endif
                        break;
                    case "SelectedThreads":
                        profilerFeatures |= EffectiveProfilerFeatures.Snapshot;
                        break;
                }
            }

            // These handlers prove that the managed export pipelines were registered.
            // Upstream does not expose whether native sampler or EventPipe startup succeeded.
            return profilerFeatures;
        }
        catch (Exception ex)
        {
            return WarnAndReturnDisabled(ex.Message);
        }
    }

    private static EffectiveProfilerFeatures WarnAndReturnDisabled(string reason)
    {
        Log.Warning($"Could not resolve upstream profiler state: {reason}");
        return EffectiveProfilerFeatures.None;
    }
}
