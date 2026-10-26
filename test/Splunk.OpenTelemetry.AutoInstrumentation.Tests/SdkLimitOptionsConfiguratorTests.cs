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

            Assert.Equal("12000", Environment.GetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT"));
        }

        [Fact]
        public void ConfigurePreservesPreconfiguredValues()
        {
            Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", "1");

            SdkLimitOptionsConfigurator.Configure();

            Assert.Equal("1", Environment.GetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT"));
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", null);
        }
    }
}
