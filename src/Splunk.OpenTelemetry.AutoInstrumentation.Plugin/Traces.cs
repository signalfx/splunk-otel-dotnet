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
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Plugin
{
    /// <summary>
    /// Splunk OTel traces plugin
    /// </summary>
    public class Traces
    {
        private readonly PluginSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Traces"/> class.
        /// </summary>
        public Traces()
            : this(PluginSettings.FromDefaultSources())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Traces"/> class.
        /// This is constructor is for testing purposes.
        /// </summary>
        /// <param name="settings">PluginSettins instance</param>
        internal Traces(PluginSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Configures Traces
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/>  to configure</param>
        /// <returns>Returns <see cref="TracerProviderBuilder"/> for chaining.</returns>
        public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
        {
            return builder;
        }

        /// <summary>
        /// Configure Otlp exporter options
        /// </summary>
        /// <param name="options">Otlp options</param>
        public void ConfigureOptions(OtlpExporterOptions options)
        {
            if (_settings.Realm != null)
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
    }
}
