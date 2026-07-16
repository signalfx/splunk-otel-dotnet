// <copyright file="EffectiveConfigLimits.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

internal static class EffectiveConfigLimits
{
    public const int MaxPayloadSizeBytes = 512 * 1024;
    public const int MaxFileNameLength = 4096;
    public const int MaxEndpointLength = 8192;
    public const int MaxEndpointCount = 32;

    public static void ValidatePayloadSize(int payloadSizeBytes)
    {
        if (payloadSizeBytes > MaxPayloadSizeBytes)
        {
            throw new InvalidOperationException(
                $"Effective configuration payload size {payloadSizeBytes} bytes exceeds the {MaxPayloadSizeBytes}-byte limit.");
        }
    }

    public static void ValidateFileNameLength(string? fileName)
    {
        if (fileName?.Length > MaxFileNameLength)
        {
            throw new InvalidOperationException(
                $"Effective configuration file name length {fileName.Length} exceeds the {MaxFileNameLength}-character limit.");
        }
    }

    public static void ValidateEndpointLength(string endpoint)
    {
        if (endpoint.Length > MaxEndpointLength)
        {
            throw new InvalidOperationException(
                $"OTLP endpoint length {endpoint.Length} exceeds the {MaxEndpointLength}-character effective configuration limit.");
        }
    }

    public static void ValidateEndpointCount(int endpointCount)
    {
        if (endpointCount > MaxEndpointCount)
        {
            throw new InvalidOperationException(
                $"Effective configuration endpoint count {endpointCount} exceeds the per-signal limit of {MaxEndpointCount}.");
        }
    }
}
