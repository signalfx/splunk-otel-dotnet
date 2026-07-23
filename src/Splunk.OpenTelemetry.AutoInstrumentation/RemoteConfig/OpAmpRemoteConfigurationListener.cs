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
    private readonly Func<RemoteConfigStatusReport, Task> _reportStatusAsync;
    private readonly Action<string> _applyYaml;
    private readonly object _configHashLock = new();
    private readonly object _statusReportLock = new();
    private string? _lastConfigHash;
    private Task _lastStatusReportTask = Task.CompletedTask;

    public OpAmpRemoteConfigurationListener(Action onApplied, Func<RemoteConfigStatusReport, Task> reportStatusAsync)
        : this(onApplied, reportStatusAsync, RemoteConfiguration.ApplyYaml)
    {
    }

    internal OpAmpRemoteConfigurationListener(
        Action onApplied,
        Func<RemoteConfigStatusReport, Task> reportStatusAsync,
        Action<string> applyYaml)
    {
        _onApplied = onApplied;
        _reportStatusAsync = reportStatusAsync;
        _applyYaml = applyYaml;
    }

    public void HandleMessage(RemoteConfigMessage message)
    {
        var configHash = message.ConfigHash.ToArray();
        if (!message.AgentConfigMap.TryGetValue(RemoteConfiguration.RemoteConfigFileName, out var configFile))
        {
            HandleRemoteConfiguration(configHash, contentType: null, body: null);
            return;
        }

        HandleRemoteConfiguration(configHash, configFile.ContentType, configFile.Body.ToArray());
    }

    internal void HandleRemoteConfiguration(byte[] configHash, string? contentType, byte[]? body)
    {
        try
        {
            if (IsKnownConfigHash(configHash))
            {
                return;
            }

            ReportStatus(configHash, RemoteConfigStatusCode.Applying);

            if (body == null)
            {
                ReportStatus(configHash, RemoteConfigStatusCode.Applied);
                return;
            }

            if (!IsYamlContentType(contentType))
            {
                var errorMessage = $"Unsupported content type '{contentType}' for OpAMP remote configuration '{RemoteConfiguration.RemoteConfigFileName}'.";
                Log.Warning(errorMessage);
                ReportStatus(configHash, RemoteConfigStatusCode.Failed, errorMessage);
                return;
            }

            var yaml = Encoding.UTF8.GetString(body);
            _applyYaml(yaml);
            _onApplied();
            ReportStatus(configHash, RemoteConfigStatusCode.Applied);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply OpAMP remote configuration.");
            ReportStatus(configHash, RemoteConfigStatusCode.Failed, ex.Message);
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

    private bool IsKnownConfigHash(byte[] configHash)
    {
        var configHashKey = Convert.ToBase64String(configHash);
        lock (_configHashLock)
        {
            if (configHashKey == _lastConfigHash)
            {
                return true;
            }

            _lastConfigHash = configHashKey;
            return false;
        }
    }

    private void ReportStatus(byte[] configHash, RemoteConfigStatusCode status, string? errorMessage = null)
    {
        if (configHash.Length == 0)
        {
            Log.Warning("Cannot report OpAMP remote configuration status because the remote configuration hash is empty.");
            return;
        }

        var statusReport = new RemoteConfigStatusReport(configHash, status, errorMessage);
        lock (_statusReportLock)
        {
            _lastStatusReportTask = ReportStatusAfterPreviousAsync(_lastStatusReportTask, statusReport);
        }
    }

    private async Task ReportStatusAfterPreviousAsync(Task previousStatusReportTask, RemoteConfigStatusReport statusReport)
    {
        try
        {
            await previousStatusReportTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning($"Previous remote configuration status report failed: {ex.Message}");
        }

        await ReportStatusAsync(statusReport).ConfigureAwait(false);
    }

    private async Task ReportStatusAsync(RemoteConfigStatusReport statusReport)
    {
        try
        {
            await _reportStatusAsync(statusReport).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to report remote configuration status to OpAMP server: {ex.Message}");
        }
    }
}
