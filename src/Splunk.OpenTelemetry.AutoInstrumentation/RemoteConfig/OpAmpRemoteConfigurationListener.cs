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

using System.Diagnostics;
using System.Text;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.RemoteConfig;

internal sealed class OpAmpRemoteConfigurationListener : IOpAmpListener<RemoteConfigMessage>
{
    private const int MaxApplicationAttempts = 3;

    private static readonly ILogger Log = new Logger();
    private static readonly long RetryIntervalTicks = 30 * Stopwatch.Frequency;
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly Action _onApplied;
    private readonly Func<RemoteConfigStatusReport, Task> _reportStatusAsync;
    private readonly Action<string> _applyYaml;
    private readonly Func<long> _getTimestamp;
    private readonly object _configurationLock = new();
    private string? _lastConfigHash;
    private int _applicationAttempts;
    private bool _handlingConfiguration;
    private RemoteConfigStatusCode _lastStatus;
    private string? _lastErrorMessage;
    private long _lastStatusReportTimestamp;
    private Task _lastStatusReportTask = Task.CompletedTask;

    public OpAmpRemoteConfigurationListener(Action onApplied, Func<RemoteConfigStatusReport, Task> reportStatusAsync)
        : this(onApplied, reportStatusAsync, RemoteConfiguration.ApplyYaml)
    {
    }

    internal OpAmpRemoteConfigurationListener(
        Action onApplied,
        Func<RemoteConfigStatusReport, Task> reportStatusAsync,
        Action<string> applyYaml,
        Func<long>? getTimestamp = null)
    {
        _onApplied = onApplied;
        _reportStatusAsync = reportStatusAsync;
        _applyYaml = applyYaml;
        _getTimestamp = getTimestamp ?? Stopwatch.GetTimestamp;
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
        lock (_configurationLock)
        {
            if (_handlingConfiguration)
            {
                return;
            }

            var configHashKey = Convert.ToBase64String(configHash);
            var sameConfiguration = configHashKey == _lastConfigHash;
            if (sameConfiguration &&
                (!_lastStatusReportTask.IsCompleted || _getTimestamp() - _lastStatusReportTimestamp < RetryIntervalTicks))
            {
                return;
            }

            if (!sameConfiguration)
            {
                _lastConfigHash = configHashKey;
                _applicationAttempts = 0;
            }

            _handlingConfiguration = true;
            try
            {
                if (sameConfiguration &&
                    (_lastStatus == RemoteConfigStatusCode.Applied || _applicationAttempts >= MaxApplicationAttempts))
                {
                    // Queueing a status does not acknowledge delivery. Resubmit the cached result at a bounded rate.
                    ReportStatus(configHash, _lastStatus, _lastErrorMessage);
                    return;
                }

                _applicationAttempts++;
                ApplyConfiguration(configHash, contentType, body);
            }
            finally
            {
                _handlingConfiguration = false;
            }
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

    private void ApplyConfiguration(byte[] configHash, string? contentType, byte[]? body)
    {
        try
        {
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

            var yaml = StrictUtf8.GetString(body);
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

    private void ReportStatus(byte[] configHash, RemoteConfigStatusCode status, string? errorMessage = null)
    {
        _lastStatus = status;
        _lastErrorMessage = errorMessage;
        _lastStatusReportTimestamp = _getTimestamp();

        if (configHash.Length == 0)
        {
            Log.Warning("Cannot report OpAMP remote configuration status because the remote configuration hash is empty.");
            return;
        }

        var statusReport = new RemoteConfigStatusReport(configHash, status, errorMessage);
        _lastStatusReportTask = ReportStatusAfterPreviousAsync(_lastStatusReportTask, statusReport);
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
