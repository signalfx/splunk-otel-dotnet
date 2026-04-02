// <copyright file="PluginTests.cs" company="Splunk Inc.">
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

#if NET

using System.Collections.Specialized;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class PluginTests
    {
        [Fact]
        public void WhenSnapshotsAreEnabled_BaggagePropagatorIsNotRequired()
        {
            // Force SDK init.
            _ = global::OpenTelemetry.Sdk.SuppressInstrumentation;

            var defaultPropagator = global::OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator;
            global::OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());

            var defaultFactory = Plugin.DefaultSettingsFactory;

            try
            {
                Plugin.DefaultSettingsFactory = () => new PluginSettings(new NameValueConfigurationSource(
                    new NameValueCollection
                    {
                        ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true"
                    }));

                var plugin = new Plugin();
                var builder = new TracerProviderBuilderBase();

                plugin.BeforeConfigureTracerProvider(builder);

                Assert.IsType<TraceContextPropagator>(Propagators.DefaultTextMapPropagator);
            }
            finally
            {
                global::OpenTelemetry.Sdk.SetDefaultTextMapPropagator(defaultPropagator);
                Plugin.DefaultSettingsFactory = defaultFactory;
            }
        }

        [Fact]
        public void WhenSnapshotsAreEnabled_DefaultPropagatorIsUnchanged()
        {
            // Force SDK init.
            _ = global::OpenTelemetry.Sdk.SuppressInstrumentation;

            var defaultPropagator = global::OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator;
            var defaultFactory = Plugin.DefaultSettingsFactory;

            try
            {
                Plugin.DefaultSettingsFactory = () => new PluginSettings(new NameValueConfigurationSource(
                    new NameValueCollection
                    {
                        ["SPLUNK_SNAPSHOT_PROFILER_ENABLED"] = "true"
                    }));

                var plugin = new Plugin();
                var builder = new TracerProviderBuilderBase();

                plugin.BeforeConfigureTracerProvider(builder);

                Assert.Same(defaultPropagator, Propagators.DefaultTextMapPropagator);
            }
            finally
            {
                global::OpenTelemetry.Sdk.SetDefaultTextMapPropagator(defaultPropagator);
                Plugin.DefaultSettingsFactory = defaultFactory;
            }
        }
    }
}
#endif
