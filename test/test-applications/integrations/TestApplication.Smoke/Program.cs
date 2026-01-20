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

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using TestApplication.Shared;

namespace TestApplication.Smoke;

public class Program
{
    public const string SourceName = "MyCompany.MyProduct.MyLibrary";

    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        if (args.Length != 2)
        {
            throw new InvalidOperationException("Missing arguments. Provide server port with --test-server-port <test-server-port>.");
        }

        var testServerPort = int.Parse(args[1], CultureInfo.InvariantCulture);

        EmitTraces(testServerPort);
        EmitMetrics();
        EmitLogs();

        // The "LONG_RUNNING" environment variable is used by tests that access/receive
        // data that takes time to be produced.
        var longRunning = Environment.GetEnvironmentVariable("LONG_RUNNING");

        while (longRunning == "true")
        {
            // In this case it is necessary to ensure that the test has a chance to read the
            // expected data, only by keeping the application alive for some time that can
            // be ensured. Anyway, tests that set "LONG_RUNNING" env var to true are expected
            // to kill the process directly.
            Console.WriteLine("LONG_RUNNING is true, waiting for process to be killed...");
            Console.ReadLine();
        }
    }

    private static void EmitTraces(int testServerPort)
    {
        var myActivitySource = new ActivitySource(SourceName, "1.0.0");

        using (var activity = myActivitySource.StartActivity("SayHello"))
        {
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new[] { 1, 2, 3 });

            activity?.SetTag("long", new string('*', 13000));
        }

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(1)
        };

        try
        {
            client.GetStringAsync(new Uri($"http://localhost:{testServerPort}/test")).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static void EmitMetrics()
    {
        var myMeter = new Meter(SourceName, "1.0");
        var myFruitCounter = myMeter.CreateCounter<int>("MyFruitCounter");

        myFruitCounter.Add(1, new KeyValuePair<string, object?>("name", "apple"));
    }

    private static void EmitLogs()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("SmokeTest app log");
    }
}
