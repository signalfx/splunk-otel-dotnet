// <copyright file="WinApi.cs" company="Splunk Inc.">
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

#if NET

using System.Runtime.InteropServices;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Helpers;

internal static class WinApi
{
    // Corresponds to TIMERR_NOERROR
    // https://learn.microsoft.com/en-us/windows/win32/api/timeapi/nf-timeapi-timebeginperiod
    private const uint SuccessResult = 0;
    // Set period to 1 ms, from default 15.625ms
    private const uint HighResolutionTimerPeriodMs = 1;

    private static readonly ILogger Log = new Logger();

    public static bool TryEnableHighResolutionTimer()
    {
        var timeBeginPeriodResult = Windows.TimeBeginPeriod(HighResolutionTimerPeriodMs);
        var enableHighResolutionTimerSucceeded = timeBeginPeriodResult == SuccessResult;
        if (!enableHighResolutionTimerSucceeded)
        {
            Log.Warning($"Enabling high resolution timer failed with error: {timeBeginPeriodResult}");
        }

        return enableHighResolutionTimerSucceeded;
    }

    public static bool TryDisableHighResolutionTimer()
    {
        var timeEndPeriodResult = Windows.TimeEndPeriod(HighResolutionTimerPeriodMs);
        var disableHighResolutionTimerSucceeded = timeEndPeriodResult == SuccessResult;
        if (!disableHighResolutionTimerSucceeded)
        {
            Log.Warning($"Disabling high resolution timer failed with error: {timeEndPeriodResult}");
        }

        return disableHighResolutionTimerSucceeded;
    }

    private static class Windows
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint TimeBeginPeriod(uint milliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint TimeEndPeriod(uint milliseconds);
    }
}
#endif
