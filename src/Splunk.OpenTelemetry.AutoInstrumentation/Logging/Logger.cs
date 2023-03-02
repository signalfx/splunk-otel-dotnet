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

using System.Reflection;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Logging;

internal class Logger : ILogger
{
    private static readonly object? Log;
    private static readonly MethodInfo? WarningMethod;
    private static readonly MethodInfo? ErrorMethod;
    private static readonly MethodInfo? ErrorWithExceptionMethod;

    static Logger()
    {
        try
        {
            var otelLoggingType = Type.GetType("OpenTelemetry.AutoInstrumentation.Logging.OtelLogging, OpenTelemetry.AutoInstrumentation")!;
            // Call the constructor to initialize (this method guarantees that the static constructor is only called once, regardless how many times the method is called)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(otelLoggingType.TypeHandle);

            var method = otelLoggingType.GetMethod("GetLogger", new[] { typeof(string) })!;

            Log = method.Invoke(null, new object[] { "Splunk" })!;

            WarningMethod = GetMethod("Warning");
            ErrorMethod = GetMethod("Error");
            ErrorWithExceptionMethod = GetMethodWithException("Error");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not initialize Logger. {ex}");
        }
    }

    public void Warning(string message)
    {
        WarningMethod?.Invoke(Log, new object[] { message, true });
    }

    public void Error(string message)
    {
        ErrorMethod?.Invoke(Log, new object[] { message, true });
    }

    public void Error(Exception ex, string message)
    {
        ErrorWithExceptionMethod?.Invoke(Log, new object[] { ex, message, true });
    }

    private static MethodInfo? GetMethod(string method)
    {
        // First type is 'string messageTemplate'
        // Second type is 'bool writeToEventLog = true'

        return Log?
            .GetType()
            .GetMethod(method, types: new[] { typeof(string), typeof(bool) });
    }

    private static MethodInfo? GetMethodWithException(string method)
    {
        // First type is 'Exception exception'
        // Second type is 'string messageTemplate'
        // Third type is 'bool writeToEventLog = true'

        return Log?
            .GetType()
            .GetMethod(method, types: new[] { typeof(Exception), typeof(string), typeof(bool) });
    }
}
