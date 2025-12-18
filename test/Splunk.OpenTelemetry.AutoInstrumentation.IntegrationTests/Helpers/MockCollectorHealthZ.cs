// <copyright file="MockCollectorHealthZ.cs" company="Splunk Inc.">
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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Net;
using Microsoft.AspNetCore.Http;
using Xunit.Abstractions;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

internal class MockCollectorHealthZ
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(1)
    };

    public static PathHandler CreateHealthZHandler()
    {
        return new PathHandler(HandleHealthZRequests, "/healthz");
    }

    public static void WarmupHealthZEndpoint(ITestOutputHelper output, string host, int port)
    {
        var healthZUrl = new Uri($"http://{host}:{port}/healthz");
        const int maxAttempts = 10;
        for (var i = 1; i <= maxAttempts; ++i)
        {
            try
            {
                var response = HttpClient.GetAsync(healthZUrl).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                output.WriteLine($"Exception while calling {healthZUrl}: {e}");
            }

            if (i == maxAttempts)
            {
                throw new InvalidOperationException($"Failed to warm up healthz endpoint at {healthZUrl} after {maxAttempts} attempts.");
            }
        }
    }

    private static async Task HandleHealthZRequests(HttpContext context)
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("OK");
    }
}
#endif
