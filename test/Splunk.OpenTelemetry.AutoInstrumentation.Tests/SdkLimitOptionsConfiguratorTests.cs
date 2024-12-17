// <copyright file="SdkLimitOptionsConfiguratorTests.cs" company="Splunk Inc.">
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

using FluentAssertions.Execution;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class SdkLimitOptionsConfiguratorTests : IDisposable
    {
        public SdkLimitOptionsConfiguratorTests()
        {
            ClearEnvVars();
        }

        public void Dispose()
        {
            ClearEnvVars();
        }

        [Fact]
        public void ConfigureEmptyValues()
        {
            SdkLimitOptionsConfigurator.Configure();

            using (new AssertionScope())
            {
                Environment.GetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT").Should().Be("12000");
                Environment.GetEnvironmentVariable("OTEL_ATTRIBUTE_COUNT_LIMIT").Should().Be("2147483647");
                Environment.GetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT").Should().BeNullOrEmpty();
                Environment.GetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT").Should().BeNullOrEmpty();
                Environment.GetEnvironmentVariable("OTEL_SPAN_EVENT_COUNT_LIMIT").Should().Be("2147483647");
                Environment.GetEnvironmentVariable("OTEL_SPAN_LINK_COUNT_LIMIT").Should().Be("1000");
                Environment.GetEnvironmentVariable("OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT").Should().BeNullOrEmpty();
                Environment.GetEnvironmentVariable("OTEL_LINK_ATTRIBUTE_COUNT_LIMIT").Should().BeNullOrEmpty();
            }
        }

        [Fact]
        public void ConfigurePreservesPreconfiguredValues()
        {
            Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", "1");
            Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_COUNT_LIMIT", "2");
            Environment.SetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT", "3");
            Environment.SetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT", "4");
            Environment.SetEnvironmentVariable("OTEL_SPAN_EVENT_COUNT_LIMIT", "5");
            Environment.SetEnvironmentVariable("OTEL_SPAN_LINK_COUNT_LIMIT", "6");
            Environment.SetEnvironmentVariable("OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT", "7");
            Environment.SetEnvironmentVariable("OTEL_LINK_ATTRIBUTE_COUNT_LIMIT", "8");

            SdkLimitOptionsConfigurator.Configure();

            using (new AssertionScope())
            {
                Environment.GetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT").Should().Be("1");
                Environment.GetEnvironmentVariable("OTEL_ATTRIBUTE_COUNT_LIMIT").Should().Be("2");
                Environment.GetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT").Should().Be("3");
                Environment.GetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT").Should().Be("4");
                Environment.GetEnvironmentVariable("OTEL_SPAN_EVENT_COUNT_LIMIT").Should().Be("5");
                Environment.GetEnvironmentVariable("OTEL_SPAN_LINK_COUNT_LIMIT").Should().Be("6");
                Environment.GetEnvironmentVariable("OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT").Should().Be("7");
                Environment.GetEnvironmentVariable("OTEL_LINK_ATTRIBUTE_COUNT_LIMIT").Should().Be("8");
            }
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_COUNT_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_SPAN_EVENT_COUNT_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_SPAN_LINK_COUNT_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT", null);
            Environment.SetEnvironmentVariable("OTEL_LINK_ATTRIBUTE_COUNT_LIMIT", null);
        }
    }
}
