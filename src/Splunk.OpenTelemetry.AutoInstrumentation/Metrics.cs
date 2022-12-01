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
using OpenTelemetry.Metrics;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal class Metrics
{
    private readonly PluginSettings _settings;

    internal Metrics(PluginSettings settings)
    {
        _settings = settings;
    }

    public MeterProviderBuilder ConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder.ConfigureResource(ResourceConfigurator.Configure);
    }

    public void ConfigureMetricsOptions(OtlpExporterOptions options)
    {
        if (!_settings.IsOtlpEndpointSet && _settings.Realm != null)
        {
            options.Endpoint = new Uri(string.Format(Constants.Ingest.MetricsIngestTemplate, _settings.Realm));
        }

        if (_settings.AccessToken != null)
        {
            string accessHeader = $"X-Sf-Token={_settings.AccessToken}";

            if (string.IsNullOrEmpty(options.Headers))
            {
                options.Headers = accessHeader;
            }
            else
            {
                options.Headers = $"{options.Headers}, {accessHeader}";
            }
        }
    }
}
