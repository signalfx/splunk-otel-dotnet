// <copyright file="IISContainerTestHelper.cs" company="Splunk Inc.">
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

// // Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

internal static class IISContainerTestHelper
{
    public static async Task<IContainer> StartContainerAsync(
        string imageName,
        int webPort,
        Dictionary<string, string> environmentVariables,
        ITestOutputHelper testOutputHelper)
    {
        var networkName = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync().ConfigureAwait(false);

        var logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "test-artifacts", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OpenTelemetry .NET AutoInstrumentation", "logs");
        Directory.CreateDirectory(logPath);
        testOutputHelper.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new ContainerBuilder(imageName)
            .WithCleanUp(cleanUp: true)
            .WithName($"{imageName}-{webPort}")
            .WithNetwork(networkName)
            .WithPortBinding(webPort, 80)
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs");

        foreach (var env in environmentVariables)
        {
            builder = builder.WithEnvironment(env.Key, env.Value);
        }

        var container = builder.Build();
        try
        {
            var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));
            Assert.True(wasStarted, $"Container based on {imageName} has to be operational for the test.");
            testOutputHelper.WriteLine("Container was started successfully.");

            // await HealthzHelper.TestAsync($"http://localhost:{webPort}/healthz", testOutputHelper).ConfigureAwait(false);
            testOutputHelper.WriteLine("IIS WebApp was started successfully.");
        }
        catch
        {
            await container.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return container;
    }
}

#endif
