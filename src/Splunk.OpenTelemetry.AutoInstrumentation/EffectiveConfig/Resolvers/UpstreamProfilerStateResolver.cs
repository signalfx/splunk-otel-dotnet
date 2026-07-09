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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

internal static class UpstreamProfilerStateResolver
{
    private const BindingFlags StaticNonPublicFlags = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InstanceNonPublicFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    public static EffectiveProfilerFeatures Resolve()
    {
        var instrumentationType = UpstreamInstrumentationResolver.GetInstrumentationType();

        var sampleExporterField = instrumentationType.GetField("_sampleExporter", StaticNonPublicFlags)
            ?? throw new MissingMemberException(instrumentationType.FullName, "_sampleExporter");

        var sampleExporter = sampleExporterField.GetValue(null);
        if (sampleExporter == null)
        {
            return EffectiveProfilerFeatures.None;
        }

        var sampleExporterType = sampleExporter.GetType();
        var bufferProcessor = sampleExporterType
            .GetField("_bufferProcessor", InstanceNonPublicFlags)
            ?.GetValue(sampleExporter)
            ?? throw new MissingMemberException(sampleExporterType.FullName, "_bufferProcessor");

        var bufferProcessorType = bufferProcessor.GetType();
        var sampleHandlers = bufferProcessorType
            .GetField("_sampleHandlers", InstanceNonPublicFlags)
            ?.GetValue(bufferProcessor) as IDictionary
            ?? throw new MissingMemberException(bufferProcessorType.FullName, "_sampleHandlers");

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
}
