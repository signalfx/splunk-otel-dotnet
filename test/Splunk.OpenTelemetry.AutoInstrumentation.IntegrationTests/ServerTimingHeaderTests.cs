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

#if NET

using Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests;

public class ServerTimingHeaderTests : TestHelper
{
    public ServerTimingHeaderTests(ITestOutputHelper output)
        : base("HttpServer", output)
    {
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task SubmitRequest(bool isEnabled, bool captureHeaders)
    {
        SetEnvironmentVariable("SPLUNK_TRACE_RESPONSE_HEADER_ENABLED", isEnabled.ToString());
        if (captureHeaders)
        {
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "Custom-Request-Test-Header");
        }

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("Microsoft.AspNetCore", span =>
        {
            if (captureHeaders)
            {
                return span.Attributes.FirstOrDefault(x => x.Key == "http.request.header.custom-request-test-header")
                    ?.Value.StringValue == "Test-Value";
            }

            return true;
        });

        var port = TcpPortProvider.GetOpenPort();
        var url = $"http://localhost:{port}";

        using var process = StartTestApplication(new() { Arguments = $"--urls {url}" });
        Output.WriteLine($"ProcessName: {process?.ProcessName}");
        using var helper = new ProcessHelper(process);

        await HealthzHelper.TestAsync($"{url}/alive-check", Output);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Custom-Request-Test-Header", "Test-Value");
        var response = await client.GetAsync($"{url}/request");

        await client.GetAsync($"{url}/shutdown");

        bool processTimeout = !process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteResult(helper);

        if (isEnabled)
        {
            Assert.Single(response.Headers, x => x.Key == "Server-Timing");
            Assert.Single(response.Headers, x => x.Key == "Access-Control-Expose-Headers");
        }
        else
        {
            Assert.DoesNotContain(response.Headers, x => x.Key == "Server-Timing");
            Assert.DoesNotContain(response.Headers, x => x.Key == "Access-Control-Expose-Headers");
        }

        collector.AssertExpectations();
    }
}

#endif
