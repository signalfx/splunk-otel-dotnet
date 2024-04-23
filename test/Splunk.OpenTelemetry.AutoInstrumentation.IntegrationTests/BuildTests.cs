// <copyright file="BuildTests.cs" company="Splunk Inc.">
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

// <copyright file="BuildTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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

using System.Runtime.InteropServices;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

[UsesVerify]
public class BuildTests
{
    [Fact]
    public Task DistributionStructure()
    {
        var distributionFolder = EnvironmentHelper.GetNukeBuildOutput();
        var files = Directory.GetFiles(distributionFolder, "*", SearchOption.AllDirectories);

        var relativesPaths = new List<string>(files.Length);
        relativesPaths.AddRange(files.Select(file => file.Substring(distributionFolder.Length)));
        relativesPaths.Sort(StringComparer.Ordinal);

        var systemName = GetSystemName();

        return Verifier.Verify(relativesPaths)
            .UseTextForParameters(systemName)
            .DisableDiff();
    }

    private static string GetSystemName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var processArchitecture = RuntimeInformation.ProcessArchitecture;
            var architectureSuffix = processArchitecture.ToString().ToLowerInvariant();

            if (Environment.GetEnvironmentVariable("IsAlpine") == "true")
            {
                return $"alpine-linux-{architectureSuffix}";
            }

            return $"linux-{architectureSuffix}";
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "windows"
            : "unknown";
    }
}
