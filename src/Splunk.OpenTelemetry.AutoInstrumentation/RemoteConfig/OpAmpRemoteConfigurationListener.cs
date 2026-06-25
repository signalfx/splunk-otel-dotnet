// <copyright file="OpAmpRemoteConfigurationListener.cs" company="Splunk Inc.">
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

using System.Text;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal sealed class OpAmpRemoteConfigurationListener : IOpAmpListener<RemoteConfigMessage>
{
    private static readonly ILogger Log = new Logger();

    private readonly Action _onApplied;
    private readonly object _lock = new();
    private string? _lastConfigHash;

    public OpAmpRemoteConfigurationListener(Action onApplied)
    {
        _onApplied = onApplied;
    }

    public void HandleMessage(RemoteConfigMessage message)
    {
        try
        {
            if (!message.AgentConfigMap.TryGetValue(RemoteConfiguration.RemoteConfigFileName, out var configFile))
            {
                return;
            }

            if (!IsYamlContentType(configFile.ContentType))
            {
                Log.Warning($"Ignoring OpAMP remote configuration '{RemoteConfiguration.RemoteConfigFileName}' with unsupported content type '{configFile.ContentType}'.");
                return;
            }

            var configHash = Convert.ToBase64String(message.ConfigHash.ToArray());
            lock (_lock)
            {
                if (configHash == _lastConfigHash)
                {
                    return;
                }

                _lastConfigHash = configHash;
            }

            var yaml = Encoding.UTF8.GetString(configFile.Body.ToArray());
            RemoteConfiguration.ApplyYaml(yaml);
            _onApplied();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply OpAMP remote configuration.");
        }
    }

    private static bool IsYamlContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var mediaType = contentType!.Split(';')[0].Trim();
        return mediaType.Equals(RemoteConfiguration.RemoteConfigContentType, StringComparison.OrdinalIgnoreCase);
    }
}
