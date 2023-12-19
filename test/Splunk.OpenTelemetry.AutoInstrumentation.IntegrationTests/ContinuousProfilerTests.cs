// <copyright file="ContinuousProfilerTests.cs" company="Splunk Inc.">
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

// Continuous Profiler is not supported by .NET Framework.

#if NET6_0_OR_GREATER

using System.IO.Compression;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests
{
    [UsesVerify]
    public class ContinuousProfilerTests : TestHelper
    {
        public ContinuousProfilerTests(ITestOutputHelper output)
            : base("ContinuousProfiler", output)
        {
        }

        [Fact]
        public async void SubmitThreadSamples()
        {
            SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
            SetEnvironmentVariable("SPLUNK_PROFILER_ENABLED", "true");
            SetEnvironmentVariable("SPLUNK_PROFILER_CALL_STACK_INTERVAL", "1000");
            using var logsCollector = new MockLContinuousProfilerCollector(Output);
            SetExporter(logsCollector);
            RunTestApplication();
            var logsData = logsCollector.GetAllLogs();
            // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
            // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 2
            logsData.Length.Should().BeGreaterOrEqualTo(expected: 2);

            await DumpLogRecords(logsData);

            var containStackTraceForClassHierarchy = false;
            var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

            foreach (var data in logsData)
            {
                IList<Profile> profiles = new List<Profile>();
                var dataResourceLog = data.ResourceLogs[0];
                var instrumentationLibraryLogs = dataResourceLog.ScopeLogs[0];
                var logRecords = instrumentationLibraryLogs.LogRecords;

                foreach (var gzip in logRecords.Select(record => record.Body.StringValue).Select(Convert.FromBase64String))
                {
                    await using var memoryStream = new MemoryStream(gzip);
                    await using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                    var profile = Vendors.ProtoBuf.Serializer.Deserialize<Profile>(gzipStream);
                    profiles.Add(profile);
                }

                containStackTraceForClassHierarchy |= profiles.Any(profile => ContainStackTraceForClassHierarchy(profile, expectedStackTrace));

                using (new AssertionScope())
                {
                    AllShouldHaveBasicAttributes(logRecords, ConstantValuedAttributes());
                    RecordsContainFrameCountAttribute(logRecords);
                    ResourceContainsExpectedAttributes(dataResourceLog.Resource);
                    HasNameAndVersionSet(instrumentationLibraryLogs.Scope);
                }

                logRecords.Clear();
            }

            Assert.True(containStackTraceForClassHierarchy, "At least one stack trace containing class hierarchy should be reported.");
        }

        private static void RecordsContainFrameCountAttribute(RepeatedField<LogRecord> logRecords)
        {
            foreach (var logRecord in logRecords)
            {
                logRecord.Attributes.Should().Contain(attr => attr.Key == "profiling.data.total.frame.count");
            }
        }

        private static List<KeyValue> ConstantValuedAttributes()
        {
            return new List<KeyValue>
            {
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
                    Value = new AnyValue { StringValue = "cpu" }
                }
            };
        }

        private static void AllShouldHaveBasicAttributes(RepeatedField<LogRecord> logRecords, List<KeyValue> attributes)
        {
            foreach (var logRecord in logRecords)
            {
                foreach (var attribute in attributes)
                {
                    logRecord.Attributes.Should().ContainEquivalentOf(attribute);
                }
            }
        }

        private static IEnumerable<string> CreateExpectedStackTrace()
        {
            var stackTrace = new List<string>
            {
                "System.Threading.Thread.Sleep(System.TimeSpan)",
                "TestApplication.ContinuousProfiler.Fs.ClassFs.methodFs(System.String)",
                "TestApplication.ContinuousProfiler.Vb.ClassVb.MethodVb(System.String)",
                "My.Custom.Test.Namespace.TestDynamicClass.TryInvoke(System.Dynamic.InvokeBinder, System.Object[], System.Object\u0026)",
                "System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid3[T0, T1, T2](System.Runtime.CompilerServices.CallSite, T0, T1, T2)",
                "My.Custom.Test.Namespace.ClassENonStandardCharacters\u0104\u0118\u00D3\u0141\u017B\u0179\u0106\u0105\u0119\u00F3\u0142\u017C\u017A\u015B\u0107\u011C\u0416\u13F3\u2CC4\u02A4\u01CB\u2093\u06BF\u0B1F\u0D10\u1250\u3023\u203F\u0A6E\u1FAD_\u00601.GenericMethodDFromGenericClass[TMethod, TMethod2](TClass, TMethod, TMethod2)",
                "My.Custom.Test.Namespace.ClassD`21.MethodD(T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)",
                "My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass[T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20](T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)",
                "My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass(T)"
            };

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                stackTrace.Add("Unknown_Native_Function(unknown)");
            }

            stackTrace.Add("My.Custom.Test.Namespace.ClassA.InternalClassB`2.DoubleInternalClassB.TripleInternalClassB`1.MethodB[TB](System.Int32, TC[], TB, TD, System.Collections.Generic.IList`1[TA], System.Collections.Generic.IList`1[System.String])");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.<MethodAOthers>g__Action|7_0[T](System.Int32)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAOthers[T](System.String, System.Object, My.Custom.Test.Namespace.CustomClass, My.Custom.Test.Namespace.CustomStruct, My.Custom.Test.Namespace.CustomClass[], My.Custom.Test.Namespace.CustomStruct[], System.Collections.Generic.List`1[T])");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAPointer(unknown)");
            // TODO change above line to, it is not working for some reasons stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAPointer(System.Int32*)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAFloats(System.Single, System.Double)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAInts(System.UInt16, System.Int16, System.UInt32, System.Int32, System.UInt64, System.Int64, System.IntPtr, System.UIntPtr)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodABytes(System.Boolean, System.Char, System.SByte, System.Byte)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodA()");

            return stackTrace;
        }

        private static void ResourceContainsExpectedAttributes(global::OpenTelemetry.Proto.Resource.V1.Resource resource)
        {
            ResourceExpectorExtensions.AssertProfileResources(resource);
        }

        private static void HasNameAndVersionSet(InstrumentationScope instrumentationScope)
        {
            instrumentationScope.Name.Should().Be("otel.profiling");
            instrumentationScope.Version.Should().Be("0.1.0");
        }

        private static bool ContainStackTraceForClassHierarchy(Profile profile, string expectedStackTrace)
        {
            var frames = profile.Locations
                                .SelectMany(location => location.Lines)
                                .Select(line => line.FunctionId)
                                .Select(functionId => profile.Functions[(int)functionId - 1])
                                .Select(function => profile.StringTables[(int)function.Name]);

            var stackTrace = string.Join("\n", frames);
            return stackTrace.Contains(expectedStackTrace);
        }

        private async Task DumpLogRecords(ExportLogsServiceRequest[] logsData)
        {
            foreach (var data in logsData)
            {
                await using var memoryStream = new MemoryStream();
                await System.Text.Json.JsonSerializer.SerializeAsync(memoryStream, data);
                memoryStream.Position = 0;
                using var sr = new StreamReader(memoryStream);
                var readToEnd = await sr.ReadToEndAsync();

                Output.WriteLine(readToEnd);
            }
        }
    }
}

#endif
