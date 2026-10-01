// <copyright file="LoggerTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class LoggerTests
{
    [Theory]
    [InlineData("OTEL_EXPORTER_OTLP_HEADERS")]
    [InlineData("OTEL_EXPORTER_OTLP_TRACES_HEADERS")]
    [InlineData("OTEL_EXPORTER_OTLP_METRICS_HEADERS")]
    [InlineData("OTEL_EXPORTER_OTLP_LOGS_HEADERS")]
    public void ConfigurationLoggingHidesOtlpHeaders(string variableName)
    {
        const string secret = "authorization=Bearer%20super-secret-token";
        var previousValue = Environment.GetEnvironmentVariable(variableName);
        var logger = Substitute.For<ILogger>();

        try
        {
            Environment.SetEnvironmentVariable(variableName, secret);

            logger.LogConfigurationSetup();

            logger.Received().Debug(Arg.Is<string>(message => message.Contains($"{variableName}=<hidden>")));
            logger.DidNotReceive().Debug(Arg.Is<string>(message => message.Contains(secret)));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    [Fact]
    public void ConstructorDoesNotThrowExceptionWhenReflectionFails()
    {
        _ = new Logger();
    }

    [Fact]
    public void ImplementationDoesNotThrowExceptionWhenReflectionFails()
    {
        var logger = new Logger();
        logger.Warning("message");
    }
}
