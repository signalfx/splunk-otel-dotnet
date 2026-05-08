// <copyright file="EffectiveConfigValueFormatterTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class EffectiveConfigValueFormatterTests
{
    [Fact]
    public void FormatList_UsesCommaSeparatedQuotedValues()
    {
        Assert.Equal(
            "\"http://collector-1:4318/v1/traces\",\"http://collector-2:4318/v1/traces\"",
            EffectiveConfigValueFormatter.FormatList(
                [
                    "http://collector-1:4318/v1/traces",
                    "http://collector-2:4318/v1/traces"
                ]));
    }

    [Fact]
    public void FormatList_KeepsCommaInsideQuotedValue()
    {
        Assert.Equal(
            "\"http://collector/path,with%2Ccomma\"",
            EffectiveConfigValueFormatter.FormatList(["http://collector/path,with%2Ccomma"]));
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void FormatBoolean_UsesLowercaseValues(bool value, string expected)
    {
        Assert.Equal(expected, EffectiveConfigValueFormatter.FormatBoolean(value));
    }

    [Fact]
    public void TryFormatString_QuotesStringValue()
    {
        Assert.Equal("\"TestApplication.Smoke\"", EffectiveConfigValueFormatter.TryFormatString("TestApplication.Smoke"));
    }

    [Theory]
    [InlineData("service\"name")]
    [InlineData("service\rname")]
    [InlineData("service\nname")]
    public void TryFormatString_ReturnsNull_WhenValueCannotBeRepresented(string value)
    {
        Assert.Null(EffectiveConfigValueFormatter.TryFormatString(value));
    }

    [Fact]
    public void FormatMilliseconds_QuotesValueWithMillisecondsUnit()
    {
        Assert.Equal("\"10000ms\"", EffectiveConfigValueFormatter.FormatMilliseconds(10000));
    }
}
