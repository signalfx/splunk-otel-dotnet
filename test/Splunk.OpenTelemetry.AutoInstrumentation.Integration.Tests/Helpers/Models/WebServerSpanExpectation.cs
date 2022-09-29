// <copyright file="WebServerSpanExpectation.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Integration.Tests.Helpers.Models;

public class WebServerSpanExpectation : SpanExpectation
{
    public WebServerSpanExpectation(
        string serviceName,
        string? serviceVersion,
        string operationName,
        string resourceName,
        string? component = "Web",
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        string statusCode = null,
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        string? httpMethod = null)
        : base(
            serviceName,
            serviceVersion,
            operationName,
            resourceName,
            component)
    {
        StatusCode = statusCode;
        HttpMethod = httpMethod;

        // Expectations for all spans of a web server variety should go here
        // RegisterTagExpectation(Tags.HttpStatusCode, expected: StatusCode);
        // RegisterTagExpectation(Tags.HttpMethod, expected: HttpMethod);
    }

    public string StatusCode { get; set; }

    public string? HttpMethod { get; set; }
}
