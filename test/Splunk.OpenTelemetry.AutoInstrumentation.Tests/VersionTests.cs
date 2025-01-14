﻿// <copyright file="VersionTests.cs" company="Splunk Inc.">
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

using System.Diagnostics;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests
{
    public class VersionTests
    {
        [Fact]
        public void SplunkPluginVersion()
        {
            var assembly = typeof(Plugin).Assembly;

            var info = FileVersionInfo.GetVersionInfo(assembly.Location);

            Assert.NotEqual("0.0.0.0", info.FileVersion);
            Assert.DoesNotContain("0.0.0-alpha.0", info.ProductVersion);
        }
    }
}
