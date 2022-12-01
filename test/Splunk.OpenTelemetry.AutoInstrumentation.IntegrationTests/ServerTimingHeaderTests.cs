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

using System.Net.Http;
using System.Threading.Tasks;
using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class ServerTimingHeaderTests : TestHelper
{
    private const string ServiceName = "TestApplication.HttpServer";

    public ServerTimingHeaderTests(ITestOutputHelper output)
        : base("HttpServer", output)
    {
    }

    [Fact]
    public async Task SubmitRequest()
    {
        var port = 8000;
        var settings = new TestSettings() { Arguments = $"--aspnet-core-port {port}" };
        using var process = StartTestApplication(settings);
        Output.WriteLine($"ProcessName: " + process.ProcessName);
        using var helper = new ProcessHelper(process);

        var client = new HttpClient();
        var response = await client.GetAsync($"http://localhost:{port}/request");

        Output.WriteResult(helper);
    }
}
