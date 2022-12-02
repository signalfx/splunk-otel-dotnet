﻿// <copyright file="Logger.cs" company="Splunk Inc.">
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
using System.Reflection;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Logging;

internal static class Logger
{
    private static readonly object? Log;
    private static readonly MethodInfo? WarningMethod;

    static Logger()
    {
        try
        {
            var otelLoggingtype = Type.GetType("OpenTelemetry.AutoInstrumentation.Logging.OtelLogging, OpenTelemetry.AutoInstrumentation");
            // Call the constructor to initialize (this method guarantees that the static constructor is only called once, regardless how many times the method is called)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(otelLoggingtype.TypeHandle);

            var method = otelLoggingtype.GetMethod("GetLogger", BindingFlags.Static | BindingFlags.NonPublic);

            Log = method.Invoke(null, null);

            WarningMethod = Log
                .GetType()
                .GetMethod("Warning", types: new[] { typeof(string), typeof(int), typeof(string) });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not initialize Logger. {ex}");
        }
    }

    internal static void Warning(string message)
    {
        WarningMethod?.Invoke(Log, new object[] { message, 0, string.Empty });
    }
}
