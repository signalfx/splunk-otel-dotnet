// <copyright file="ServerTimingHeaderTests.cs" company="Splunk Inc.">
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

#if NET6_0_OR_GREATER

using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class ServerTimingHeaderTests : TestHelper
{
    public ServerTimingHeaderTests(ITestOutputHelper output)
        : base("HttpServer", output)
    {
    }

    [Fact]
    public async Task SubmitRequest()
    {
        var port = TcpPortProvider.GetOpenPort();
        var url = $"http://localhost:{port}";

        SetEnvironmentVariable("ASPNETCORE_URLS", url);

        using var process = StartTestApplication();
        Output.WriteLine($"ProcessName: " + process.ProcessName);
        using var helper = new ProcessHelper(process);

        await HealthzHelper.TestAsync($"{url}/alive-check", Output);

        var client = new HttpClient();
        var response = await client.GetAsync($"{url}/request");

        bool processTimeout = !process.WaitForExit((int)Timeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteResult(helper);

        using (new AssertionScope())
        {
            response.Headers.Should().Contain(x => x.Key == "Server-Timing");
            response.Headers.Should().Contain(x => x.Key == "Access-Control-Expose-Headers");
        }
    }
}

#endif
