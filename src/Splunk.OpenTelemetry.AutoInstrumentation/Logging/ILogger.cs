// <copyright file="ILogger.cs" company="Splunk Inc.">
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

namespace Splunk.OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Logger interface.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs warning message.
    /// </summary>
    /// <param name="message">Message to be logged.</param>
    void Warning(string message);

    /// <summary>
    /// Logs error message.
    /// </summary>
    /// <param name="message">Message to be logged.</param>
    void Error(string message);

    /// <summary>
    /// Logs error message.
    /// </summary>
    /// <param name="exception">Exception to be logged.</param>
    /// <param name="message">Message to be logged.</param>
    void Error(Exception exception, string message);
}
