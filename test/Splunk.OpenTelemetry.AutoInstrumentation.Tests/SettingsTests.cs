// <copyright file="SettingsTests.cs" company="Splunk Inc.">
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

using System;
using FluentAssertions.Execution;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class SettingsTests : IDisposable
    {
        public SettingsTests()
        {
            ClearEnvVars();
        }

        public void Dispose()
        {
            ClearEnvVars();
        }

        [Fact]
        internal void TracerSettings_DefaultValues()
        {
            var settings = PluginSettings.FromDefaultSources();

            using (new AssertionScope())
            {
                settings.Realm.Should().BeNull();
                settings.AccessToken.Should().BeNull();
            }
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.Realm, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.Splunk.AccessToken, null);
        }
    }
}
