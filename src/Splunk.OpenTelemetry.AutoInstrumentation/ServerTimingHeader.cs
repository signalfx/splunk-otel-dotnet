// <copyright file="ServerTimingHeader.cs" company="Splunk Inc.">
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
using System.Diagnostics;

namespace Splunk.OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Adds Server-Timing (and Access-Control-Expose-Headers) header to the HTTP
/// response. The Server-Timing header contains the traceId and spanId of the server span.
/// </summary>
public static class ServerTimingHeader
{
    /// <summary>
    /// Key of the "Server-Timing" header.
    /// </summary>
    public const string Key = "Server-Timing";

    private const string ExposeHeadersHeaderName = "Access-Control-Expose-Headers";

    /// <summary>
    /// Sets the Server-Timing (and Access-Control-Expose-Headers) headers.
    /// </summary>
    /// <param name="activity">Current <see cref="Activity"/></param>
    /// <param name="carrier">Object on which the headers will be set</param>
    /// <param name="setter">Action for how to set the header</param>
    /// <typeparam name="T">Type of the carrier</typeparam>
    public static void SetHeaders<T>(Activity activity, T carrier, Action<T, string, string> setter)
    {
        setter(carrier, Key, ToHeaderValue(activity));
        setter(carrier, ExposeHeadersHeaderName, Key);
    }

    private static string ToHeaderValue(Activity activity)
    {
        var sampled = ((int)activity.Context.TraceFlags).ToString("D2");
        return $"traceparent;desc=\"00-{activity.TraceId}-{activity.SpanId}-{sampled}\"";
    }
}
