// <copyright file="ServiceNameWarningTests.cs" company="Splunk Inc.">
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
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class ServiceNameWarningTests : IDisposable
{
    private const string OtelServiceNameKey = "OTEL_SERVICE_NAME";
    private const string OtelResourceAttributesKey = "OTEL_RESOURCE_ATTRIBUTES";

    private Mock<ILogger> _logger = new Mock<ILogger>();

    [Fact]
    public void ServiceNameNotSet()
    {
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(1));
    }

    [Fact]
    public void ServiceNameNotSetCalledMoreThanOnce()
    {
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(1));
    }

    [Fact]
    public void ServiceNameSet()
    {
        Environment.SetEnvironmentVariable(OtelServiceNameKey, "nameset");
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(0));
    }

    [Fact]
    public void ResourceNameSet()
    {
        Environment.SetEnvironmentVariable(OtelResourceAttributesKey, "something=2,service.name=nameset");
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(0));
    }

    [Fact]
    public void ResourceWithoutServiceName()
    {
        Environment.SetEnvironmentVariable(OtelResourceAttributesKey, "something=2,other=whatever");
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(1));
    }

    [Fact]
    public void EmptyResourceName()
    {
        Environment.SetEnvironmentVariable(OtelResourceAttributesKey, "something=2,service.name=,b=3");
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(1));
    }

    [Fact]
    public void InvalidResourceName()
    {
        Environment.SetEnvironmentVariable(OtelResourceAttributesKey, "something=2,service.name==,b=3,");
        var serviceNameWarning = new ServiceNameWarning();
        serviceNameWarning.SendOnMissingServiceName(_logger.Object);
        _logger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Exactly(1));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(OtelServiceNameKey, null);
        Environment.SetEnvironmentVariable(OtelResourceAttributesKey, null);
    }
}
