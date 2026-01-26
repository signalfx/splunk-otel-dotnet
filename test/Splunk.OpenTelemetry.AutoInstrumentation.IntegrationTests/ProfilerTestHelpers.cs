// <copyright file="ProfilerTestHelpers.cs" company="Splunk Inc.">
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

using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests
{
    public static class ProfilerTestHelpers
    {
        public static List<KeyValue> ConstantValuedAttributes(string dataType)
        {
            return
            [
                new()
                {
                    Key = "com.splunk.sourcetype",
                    Value = new AnyValue { StringValue = "otel.profiling" }
                },

                new()
                {
                    Key = "profiling.data.format",
                    Value = new AnyValue { StringValue = "pprof-gzip-base64" }
                },

                new()
                {
                    Key = "profiling.data.type",
                    Value = new AnyValue { StringValue = dataType }
                }
            ];
        }

        public static void ProfilesContainAllocationValue(List<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                Assert.All(profile.Samples, x => Assert.Single(x.Values));
            }
        }

        public static void ProfilesDoNotContainAnyValue(List<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                Assert.All(profile.Samples, x => Assert.Empty(x.Values));
            }
        }

        public static void RecordsContainFrameCountAttribute(RepeatedField<LogRecord> logRecords)
        {
            foreach (var logRecord in logRecords)
            {
                Assert.Single(logRecord.Attributes, attr => attr.Key == "profiling.data.total.frame.count");
            }
        }

        public static void AllShouldHaveBasicAttributes(RepeatedField<LogRecord> logRecords, List<KeyValue> attributes)
        {
            foreach (var logRecord in logRecords)
            {
                foreach (var attribute in attributes)
                {
                    Assert.Contains(attribute, logRecord.Attributes);
                }
            }
        }

        public static void ResourceContainsExpectedAttributes(global::OpenTelemetry.Proto.Resource.V1.Resource resource, string serviceName)
        {
            ResourceExpectorExtensions.AssertProfileResources(resource, serviceName);
        }

        public static void HasNameAndVersionSet(InstrumentationScope instrumentationScope)
        {
            Assert.Equal("otel.profiling", instrumentationScope.Name);
            Assert.Equal("0.1.0", instrumentationScope.Version);
        }

        public static int GetContainsStackTraceCount(Profile profile, string expectedStackTrace)
        {
            var frames = profile.Locations
                .SelectMany(location => location.Lines)
                .Select(line => line.FunctionId)
                .Select(functionId => profile.Functions[(int)functionId - 1])
                .Select(function => profile.StringTables[(int)function.Name]);

            var stackTrace = string.Join("\n", frames);
            return stackTrace.Split([expectedStackTrace], StringSplitOptions.None).Length - 1;
        }
    }
}
