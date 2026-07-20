// <copyright file="IOpAmpReportDispatcher.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.OpAmp.Client;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Dispatches reports through an OpAMP client.
/// </summary>
internal interface IOpAmpReportDispatcher
{
    /// <summary>
    /// Dispatches the current effective configuration.
    /// </summary>
    /// <param name="client">The OpAMP client.</param>
    /// <param name="effectiveConfigReporter">The effective-configuration reporter.</param>
    /// <param name="sessionCancellationToken">The reporting-session cancellation token.</param>
    /// <returns>The outcome of the dispatch attempt.</returns>
    Task<OpAmpDispatchResult> DispatchEffectiveConfigAsync(
        OpAmpClient client,
        EffectiveConfigReporter effectiveConfigReporter,
        CancellationToken sessionCancellationToken);

    /// <summary>
    /// Dispatches a full-state report.
    /// </summary>
    /// <param name="client">The OpAMP client.</param>
    /// <param name="effectiveConfigReporter">The optional effective-configuration reporter.</param>
    /// <param name="sessionCancellationToken">The reporting-session cancellation token.</param>
    /// <returns>The outcome of the dispatch attempt.</returns>
    Task<OpAmpDispatchResult> DispatchFullStateReportAsync(
        OpAmpClient client,
        EffectiveConfigReporter? effectiveConfigReporter,
        CancellationToken sessionCancellationToken);
}
