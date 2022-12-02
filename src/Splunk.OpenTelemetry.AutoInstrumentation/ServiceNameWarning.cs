// <copyright file="ServiceNameWarning.cs" company="Splunk Inc.">
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
using System.Collections.Generic;
using System.Threading;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

internal static class ServiceNameWarning
{
    private const string ServiceNameEnvVarKey = "OTEL_SERVICE_NAME";
    private const string ResourcesEnvVarKey = "OTEL_RESOURCE_ATTRIBUTES";
    private const string AttributeName = "service.name";
    private const char AttributeListSplitter = ',';
    private const char AttributeKeyValueSplitter = '=';

    private static int _warningEmitted;

    internal static void SendOnMissingServiceName()
    {
        if (Interlocked.CompareExchange(ref _warningEmitted, 1, 0) != 0)
        {
            return;
        }

        var serviceName = Environment.GetEnvironmentVariable(ServiceNameEnvVarKey);
        if (!string.IsNullOrEmpty(serviceName))
        {
            return;
        }

        var attributes = Environment.GetEnvironmentVariable(ResourcesEnvVarKey);
        if (string.IsNullOrEmpty(attributes))
        {
            SendWarning();
            return;
        }

        IDictionary<string, string> serviceNameAttribute = new Dictionary<string, string>();
        var rawAttributes = attributes.Split(AttributeListSplitter);
        foreach (var rawKeyValuePair in rawAttributes)
        {
            var keyValuePair = rawKeyValuePair.Split(AttributeKeyValueSplitter);
            if (keyValuePair.Length != 2)
            {
                continue;
            }

            serviceNameAttribute[keyValuePair[0].Trim()] = keyValuePair[1].Trim();
        }

        if (!serviceNameAttribute.ContainsKey(AttributeName))
        {
            SendWarning();
            return;
        }

        if (string.IsNullOrEmpty(serviceNameAttribute[AttributeName]))
        {
            SendWarning();
            return;
        }
    }

    private static void SendWarning()
    {
        Logger.Warning(
            "The service.name attribute is not set, your service is unnamed and will be difficult to identify. " +
            "Set your service name using the OTEL_SERVICE_NAME or OTEL_RESOURCE_ATTRIBUTES environment variable.");
    }
}
