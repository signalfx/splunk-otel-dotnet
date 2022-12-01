// <copyright file="Traces.cs" company="Splunk Inc.">
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
using System.Collections.Generic;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

#if NETFRAMEWORK
using System.Web;
using OpenTelemetry.Instrumentation.AspNet;
#else
using OpenTelemetry.Instrumentation.AspNetCore;
#endif

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal class Traces
{
    private readonly PluginSettings _settings;

    internal Traces(PluginSettings settings)
    {
        _settings = settings;
    }

    public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder.ConfigureResource(ResourceConfigurator.Configure);
    }

    public void ConfigureTracesOptions(OtlpExporterOptions options)
    {
        if (!_settings.IsOtlpEndpointSet && _settings.Realm != null)
        {
            options.Endpoint = new Uri(string.Format(Constants.Ingest.TracesIngestTemplate, _settings.Realm));
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

#if NETFRAMEWORK
    public void ConfigureTracesOptions(AspNetInstrumentationOptions options)
    {
        if (_settings.TraceResponseHeaderEnabled)
        {
            options.Enrich = (activity, eventName, obj) =>
            {
                if (eventName == "OnStartActivity" && obj is HttpRequest request)
                {
                    var response = request.RequestContext.HttpContext.Response;

                    ServerTimingHeader.SetHeaders(activity, response.Headers, (headers, key, value) =>
                    {
                        headers[key] = value;
                    });
                }
            };
        }
    }
#endif

#if NET6_0_OR_GREATER
    public void ConfigureTracesOptions(AspNetCoreInstrumentationOptions options)
    {
        if (_settings.TraceResponseHeaderEnabled)
        {
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                var response = request.HttpContext.Response;

                ServerTimingHeader.SetHeaders(activity, response.Headers, (headers, key, value) =>
                {
                    headers.TryAdd(key, value);
                });
            };
        }
    }
#endif
}
