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

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    private static readonly ILogger Log = new Logger();

    private int _serviceNameLogged;

    public void ReportInitialSettings(PluginSettings settings)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        try
        {
            ReportSplunkSettings(settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read effective configuration.");
        }
    }

    public void ReportServiceName(Resource resource)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        var serviceName = EffectiveResourceConfigReader.ReadServiceName(resource);
        if (serviceName == null)
        {
            return;
        }

        if (Interlocked.Exchange(ref _serviceNameLogged, value: 1) != 0)
        {
            return;
        }

        Report(EffectiveConfigKeys.ServiceName, serviceName);
    }

    public void ReportTraceEndpoints(TracerProvider provider)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        try
        {
            ReportEndpoints(EffectiveConfigKeys.TracesEndpoint, OtlpEndpointProviderGraphResolver.ResolveTraceEndpoints(provider));
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve {EffectiveConfigKeys.TracesEndpoint} from TracerProvider: {ex.Message}");
        }
    }

    public void ReportMetricEndpoints(MeterProvider provider)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        try
        {
            ReportEndpoints(EffectiveConfigKeys.MetricsEndpoint, OtlpEndpointProviderGraphResolver.ResolveMetricEndpoints(provider));
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve {EffectiveConfigKeys.MetricsEndpoint} from MeterProvider: {ex.Message}");
        }
    }

    private void ReportEndpoints(string key, IReadOnlyList<string> endpoints)
    {
        if (endpoints.Count == 0)
        {
            return;
        }

        Report(key, EffectiveConfigValueFormatter.FormatList(endpoints));
    }

    private void Report(string key, string value)
    {
        Log.Debug(EffectiveConfigLogFormatter.FormatEntry(key, value));
    }

    private void ReportSplunkSettings(PluginSettings settings)
    {
        Report(ConfigurationKeys.Splunk.AlwaysOnProfiler.CpuProfilerEnabled, settings.CpuProfilerEnabled.ToString());
#if NET
        Report(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, settings.MemoryProfilerEnabled.ToString());
#else
        Report(ConfigurationKeys.Splunk.AlwaysOnProfiler.MemoryProfilerEnabled, false.ToString());
#endif
        Report(ConfigurationKeys.Splunk.Snapshots.Enabled, settings.SnapshotsEnabled.ToString());
        Report(ConfigurationKeys.Splunk.Snapshots.SamplingIntervalMs, settings.SnapshotsSamplingInterval.ToString());
        Report(ConfigurationKeys.Splunk.AlwaysOnProfiler.CallStackInterval, settings.CpuProfilerCallStackInterval.ToString());
    }
}
