// <copyright file="EffectiveYamlSerializerTests.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NET
using System.Reflection;
using System.Text;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Serialization;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class EffectiveYamlSerializerTests
{
    [Fact]
    public void Serialize_AllowsContentAtExactSizeLimitAfterRemovingTerminalLineEnding()
    {
        LoadUpstreamAssembly();
        var config = EffectiveYamlConfig.Create(new EffectiveConfigSnapshot(
            fileBasedConfigFileName: "stable.yaml",
            traceEndpoints: [],
            metricEndpoints: [],
            logEndpoints: [],
            cpuProfilerEnabled: false,
            memoryProfilerEnabled: false,
            snapshotProfilerEnabled: false,
            cpuProfilerCallStackInterval: 10000,
            snapshotSamplingInterval: 40));
        var expected = EffectiveYamlSerializer.Serialize(
            config,
            EffectiveConfigLimits.MaxFileContentSizeBytes);
        var exactSizeBytes = Encoding.UTF8.GetByteCount(expected);

        var actual = EffectiveYamlSerializer.Serialize(config, exactSizeBytes);

        Assert.Equal(expected, actual);
    }

    private static void LoadUpstreamAssembly()
    {
        const string FrameworkDirectory = "net";
        var assemblyPath = Path.Combine(
            EnvironmentHelper.GetNukeBuildOutput(),
            FrameworkDirectory,
            "OpenTelemetry.AutoInstrumentation.dll");
        _ = Assembly.LoadFrom(assemblyPath);
    }
}
#endif
