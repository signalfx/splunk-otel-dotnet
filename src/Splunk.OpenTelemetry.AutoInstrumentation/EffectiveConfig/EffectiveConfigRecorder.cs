// <copyright file="EffectiveConfigRecorder.cs" company="Splunk Inc.">
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

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigRecorder
{
    private readonly EffectiveConfigStaticSettings _staticSettings;
    private readonly EffectiveProviderEndpointTracker<TracerProvider> _traceEndpointTracker;
    private readonly EffectiveProviderEndpointTracker<MeterProvider> _metricEndpointTracker;
    private readonly EffectiveLogEndpointTracker _logEndpointTracker;
    private readonly bool _openTelemetrySdkDisabled;

    public EffectiveConfigRecorder(
        EffectiveConfigStaticSettings staticSettings,
        bool openTelemetrySdkDisabled)
        : this(staticSettings, openTelemetrySdkDisabled, new EffectiveLogEndpointTracker())
    {
    }

    internal EffectiveConfigRecorder(
        EffectiveConfigStaticSettings staticSettings,
        bool openTelemetrySdkDisabled,
        Func<IReadOnlyList<EffectiveOtlpEndpoint>?> bridgeLogEndpointResolver)
        : this(
            staticSettings,
            openTelemetrySdkDisabled,
            new EffectiveLogEndpointTracker(bridgeLogEndpointResolver))
    {
    }

    private EffectiveConfigRecorder(
        EffectiveConfigStaticSettings staticSettings,
        bool openTelemetrySdkDisabled,
        EffectiveLogEndpointTracker logEndpointTracker)
    {
        _staticSettings = staticSettings;
        _openTelemetrySdkDisabled = openTelemetrySdkDisabled;
        _traceEndpointTracker = new EffectiveProviderEndpointTracker<TracerProvider>(
            OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints);
        _metricEndpointTracker = new EffectiveProviderEndpointTracker<MeterProvider>(
            OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints);
        _logEndpointTracker = logEndpointTracker;
    }

    public void CaptureTraceEndpoints(TracerProvider provider)
    {
        if (!_openTelemetrySdkDisabled)
        {
            _traceEndpointTracker.Capture(provider);
        }
    }

    public void CaptureMetricEndpoints(MeterProvider provider)
    {
        if (!_openTelemetrySdkDisabled)
        {
            _metricEndpointTracker.Capture(provider);
        }
    }

    public bool MarkOpenTelemetryLoggerConfigured()
    {
        return !_openTelemetrySdkDisabled && _logEndpointTracker.MarkOpenTelemetryLoggerConfigured();
    }

    public bool CaptureLogExporterOptions(OtlpExporterOptions options)
    {
        return !_openTelemetrySdkDisabled && _logEndpointTracker.CaptureLogExporterOptions(options);
    }

    internal void ValidateCompatibility()
    {
        if (!_openTelemetrySdkDisabled)
        {
#if NET
            OtlpLogEndpointOptionsResolver.ValidateCompatibility();
#endif
            _traceEndpointTracker.ValidateState();
            _metricEndpointTracker.ValidateState();
            _logEndpointTracker.ValidateState();
        }

        EffectiveConfigPayloadBuilder.ValidateCompatibility(_staticSettings.FileBasedConfigFileName != null);
    }

    internal EffectiveConfigSnapshot CreateSnapshot(EffectiveProfilerFeatures profilerFeatures)
    {
        IReadOnlyList<EffectiveOtlpEndpoint> traceEndpoints = [];
        IReadOnlyList<EffectiveOtlpEndpoint> metricEndpoints = [];
        IReadOnlyList<EffectiveOtlpEndpoint> logEndpoints = [];

        if (!_openTelemetrySdkDisabled)
        {
            traceEndpoints = _traceEndpointTracker.GetEndpoints();
            metricEndpoints = _metricEndpointTracker.GetEndpoints();
            logEndpoints = _logEndpointTracker.GetEndpoints();
        }

        return EffectiveConfigSnapshot.Create(
            _staticSettings,
            profilerFeatures,
            traceEndpoints,
            metricEndpoints,
            logEndpoints);
    }
}
