// <copyright file="UpstreamLoggerProviderResolver.cs" company="Splunk Inc.">
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

using System.Reflection;
using OpenTelemetry.Logs;

namespace Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

internal static class UpstreamLoggerProviderResolver
{
    private const BindingFlags StaticNonPublicFlags = BindingFlags.Static | BindingFlags.NonPublic;

    public static LoggerProvider? TryGetAlreadyCreatedLoggerProvider()
    {
        var instrumentationType = UpstreamInstrumentationResolver.TryGetInstrumentationType();
        var loggerProviderFactory = instrumentationType
            ?.GetProperty("LoggerProviderFactory", StaticNonPublicFlags)
            ?.GetValue(null);

        if (loggerProviderFactory is not Lazy<LoggerProvider?> lazyLoggerProvider || !lazyLoggerProvider.IsValueCreated)
        {
            // Effective config reporting must not create bridge providers just to inspect them.
            return null;
        }

        return lazyLoggerProvider.Value;
    }
}
