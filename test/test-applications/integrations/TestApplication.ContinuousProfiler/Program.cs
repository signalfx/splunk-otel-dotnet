// <copyright file="Program.cs" company="Splunk Inc.">
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

namespace TestApplication.ContinuousProfiler
{
    public static class Program
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("TestApplication.ContinuousProfiler");

        public static void Main()
        {
            using (var activity = ActivitySource.StartActivity("Main"))
            {
                // Run for ~6 seconds to allow profiler to collect samples
                var endTime = DateTime.UtcNow.AddSeconds(6);
                while (DateTime.UtcNow < endTime)
                {
                    My.Custom.Test.Namespace.ClassA.MethodA();
                }
            }

            Console.WriteLine("Test application completed.");
        }
    }
}
