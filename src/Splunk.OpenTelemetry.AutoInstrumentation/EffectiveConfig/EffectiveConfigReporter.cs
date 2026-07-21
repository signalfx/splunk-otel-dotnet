// <copyright file="EffectiveConfigReporter.cs" company="Splunk Inc.">
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

using OpenTelemetry.OpAmp.Client.Messages;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    private readonly EffectiveConfigRecorder _recorder;
    private readonly EffectiveProfilerFeatures _profilerFeatures;

    private EffectiveConfigReporter(
        EffectiveConfigRecorder recorder,
        EffectiveProfilerFeatures profilerFeatures)
    {
        _recorder = recorder;
        _profilerFeatures = profilerFeatures;
    }

    public static EffectiveConfigReporter CreateValidated(
        EffectiveConfigRecorder recorder,
        EffectiveProfilerFeatures profilerFeatures)
    {
        recorder.ValidateCompatibility();
        EffectiveConfigPayloadBuilder.Validate(recorder.CreateSnapshot(profilerFeatures));
        return new EffectiveConfigReporter(recorder, profilerFeatures);
    }

    internal EffectiveConfigFile BuildCurrentPayload()
    {
        return EffectiveConfigPayloadBuilder.Build(_recorder.CreateSnapshot(_profilerFeatures));
    }
}
