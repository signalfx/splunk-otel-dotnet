// <copyright file="Metrics.cs" company="Splunk Inc.">
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

using System;
using OpenTelemetry.Exporter;
using Splunk.OpenTelemetry.AutoInstrumentation.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal class Metrics
{
    private readonly ILogger _log;
    private readonly PluginSettings _settings;

    internal Metrics(PluginSettings settings)
    : this(settings, new Logger())
    {
    }

    internal Metrics(PluginSettings settings, ILogger logger)
    {
        _settings = settings;
        _log = logger;
    }

    public void ConfigureMetricsOptions(OtlpExporterOptions options)
    {
        if (!_settings.IsOtlpEndpointSet && _settings.Realm != Constants.None)
        {
            if (string.IsNullOrEmpty(_settings.AccessToken))
            {
                _log.Error($"'{ConfigurationKeys.Splunk.AccessToken}' is required when '{ConfigurationKeys.Splunk.Realm}' is set.");
                return;
            }

            options.Endpoint = new Uri(string.Format(Constants.Ingest.MetricsIngestTemplate, _settings.Realm));
            options.Headers = options.Headers.AppendAccessToken(_settings.AccessToken!);
        }
    }
}
