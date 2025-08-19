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
using System.Runtime.CompilerServices;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Splunk.OpenTelemetry.AutoInstrumentation.Configuration;
using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;
using Splunk.OpenTelemetry.AutoInstrumentation.Snapshots;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class PluginTests
    {
        [Fact]
        public void WhenSnapshotsAreEnabledAndBaggagePropagatorIsNotConfigured_ExceptionIsThrown()
        {
            // Force SDK init.
            _ = global::OpenTelemetry.Sdk.SuppressInstrumentation;

            var defaultPropagator = global::OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator;

            // Replace default propagator which propagates baggage with something else.
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
                Assert.Throws<NotSupportedException>(() => plugin.BeforeConfigureTracerProvider(builder));
            }
            finally
            {
                global::OpenTelemetry.Sdk.SetDefaultTextMapPropagator(defaultPropagator);
                Plugin.DefaultSettingsFactory = defaultFactory;
            }
        }

        [Fact]
        public void WhenSnapshotsAreEnabledAndBaggagePropagatorIsConfigured_SdkIsCustomized()
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

                var configuredPropagator = Propagators.DefaultTextMapPropagator;
                var propagators = GetPropagators((CompositeTextMapPropagator)configuredPropagator);

                Assert.Equal(2, propagators.Count);

                Assert.Equal(propagators[0], defaultPropagator);
                Assert.IsType<SnapshotVolumePropagator>(propagators[1]);
            }
            finally
            {
                global::OpenTelemetry.Sdk.SetDefaultTextMapPropagator(defaultPropagator);
                Plugin.DefaultSettingsFactory = defaultFactory;
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "propagators")]
        private static extern ref IReadOnlyList<TextMapPropagator> GetPropagators(CompositeTextMapPropagator @this);
    }
}
#endif
