// <copyright file="LifetimeObserver.cs" company="Splunk Inc.">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TestApplication.HttpServer;

public class LifetimeObserver : IDisposable, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
{
    private readonly WebApplication _app;
    private readonly string _terminatorPath;
    private readonly Timer _timer;
    private IDisposable _subscriber;
    private int _shutDown;

    public LifetimeObserver(WebApplication app, string terminatorPath)
    {
        _app = app;
        _terminatorPath = terminatorPath;
        _timer = new Timer((state) => Shutdown());

        // Trigger application shutdown in 2 minutes
        _timer.Change(TimeSpan.FromMinutes(2), Timeout.InfiniteTimeSpan);

        _subscriber = DiagnosticListener.AllListeners.Subscribe(this);
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener value)
    {
        value.Subscribe(this);
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        const string key = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

        if (value.Key == key &&
            value.Value is HttpContext httpContext &&
            httpContext.Request.Path == _terminatorPath)
        {
            Shutdown();
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _subscriber?.Dispose();
    }

    private void Shutdown()
    {
        if (Interlocked.Exchange(ref _shutDown, 1) != 0)
        {
            // Allready called
            return;
        }

        _app.StopAsync().Wait();
    }
}
