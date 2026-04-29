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

using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal sealed class EffectiveConfigReporter
{
    private static readonly ILogger Log = new Logger();

    private readonly EffectiveConfigValueAccumulator _effectiveConfigValues = new();
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

    public void CaptureOtlpEndpoint(string configurationKey, OtlpExporterOptions options)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        try
        {
            var endpoint = OtlpEndpointResolver.ResolveFromOptions(options, configurationKey);
            if (endpoint != null)
            {
                // File-based config can create multiple OTLP exporters for the same signal.
                // Keep one env-var-shaped key and append every resolved endpoint value.
                _effectiveConfigValues.Add(configurationKey, endpoint);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to resolve {configurationKey} from OtlpExporterOptions: {ex.Message}");
        }
    }

    public void ReportCapturedValue(string configurationKey)
    {
        if (!Log.IsDebugEnabled)
        {
            return;
        }

        var value = _effectiveConfigValues.GetValue(configurationKey);
        if (value != null)
        {
            Report(configurationKey, value);
        }
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
