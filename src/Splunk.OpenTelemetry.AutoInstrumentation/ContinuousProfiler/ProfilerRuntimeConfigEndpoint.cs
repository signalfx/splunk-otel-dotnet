// <copyright file="ProfilerRuntimeConfigEndpoint.cs" company="Splunk Inc.">
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

using System.Net;
using System.Text;
using Splunk.OpenTelemetry.AutoInstrumentation.Logging;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal sealed class ProfilerRuntimeConfigEndpoint : IDisposable
{
    private const string Localhost = "localhost";
    private static readonly ILogger Log = new Logger();

    private readonly bool _enabled;
    private readonly int _port;
    private readonly object _lock = new();
    private HttpListener? _listener;
    private Thread? _listenerThread;
    private bool _disposed;

    public ProfilerRuntimeConfigEndpoint(PluginSettings settings)
    {
        _enabled = settings.ProfilerRuntimeConfigEndpointEnabled && settings.ProfilerRuntimeConfigEndpointPort > 0;
        _port = settings.ProfilerRuntimeConfigEndpointPort;
    }

    public void Start()
    {
        if (!_enabled)
        {
            return;
        }

        lock (_lock)
        {
            if (_listener != null)
            {
                return;
            }

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://{Localhost}:{_port}/");
            listener.Start();

            _listener = listener;
            _listenerThread = new Thread(Listen)
            {
                IsBackground = true,
                Name = "Splunk Profiler Runtime Config Endpoint"
            };
            _listenerThread.Start();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnProcessExit;
            Log.Debug($"Profiler runtime configuration endpoint started on http://{Localhost}:{_port}/.");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        Stop();
    }

    private static void WriteJson(HttpListenerContext context, int statusCode, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentEncoding = Encoding.UTF8;
        context.Response.ContentLength64 = body.Length;
        context.Response.OutputStream.Write(body, 0, body.Length);
        context.Response.Close();
    }

    private static void WriteText(HttpListenerContext context, int statusCode, string text)
    {
        var body = Encoding.UTF8.GetBytes(text);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain";
        context.Response.ContentEncoding = Encoding.UTF8;
        context.Response.ContentLength64 = body.Length;
        context.Response.OutputStream.Write(body, 0, body.Length);
        context.Response.Close();
    }

    private static IReadOnlyDictionary<string, string?> ReadSettings(HttpListenerRequest request)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal);
        var url = request.Url;
        AddSettings(settings, url == null ? null : url.Query.TrimStart('?'));

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
        AddSettings(settings, reader.ReadToEnd());
        return settings;
    }

    private static void AddSettings(IDictionary<string, string?> settings, string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        foreach (var entry in payload!.Split(['&', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = Decode(entry.Substring(0, separatorIndex));
            var value = Decode(entry.Substring(separatorIndex + 1));
            settings[key] = value;
        }
    }

    private static string Decode(string value)
    {
        return Uri.UnescapeDataString(value.Replace("+", " "));
    }

    private static string ToJson(ProfilerRuntimeUpdateResult result)
    {
        return "{" +
               $"\"applied\":{ToJsonArray(result.Applied)}," +
               $"\"unsupported\":{ToJsonArray(result.Unsupported)}," +
               $"\"unknown\":{ToJsonArray(result.Unknown)}," +
               $"\"invalid\":{ToJsonArray(result.Invalid)}" +
               "}";
    }

    private static string ToJsonArray(IEnumerable<string> values)
    {
        return "[" + string.Join(",", values.Select(value => $"\"{EscapeJson(value)}\"")) + "]";
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private void Listen()
    {
        while (true)
        {
            HttpListenerContext context;
            try
            {
                context = _listener!.GetContext();
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => Handle(context));
        }
    }

    private void Handle(HttpListenerContext context)
    {
        try
        {
            var url = context.Request.Url;
            var path = url == null ? string.Empty : url.AbsolutePath.TrimEnd('/');
            if (context.Request.HttpMethod == "GET" && path == "/healthz")
            {
                WriteText(context, 200, "OK");
                return;
            }

            if (context.Request.HttpMethod != "POST" || path != "/configure")
            {
                WriteText(context, 404, "Not found");
                return;
            }

            var result = ProfilerRuntimeConfiguration.Apply(ReadSettings(context.Request));
            WriteJson(context, 200, ToJson(result));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process profiler runtime configuration request.");
            WriteJson(context, 500, "{\"error\":\"failed\"}");
        }
    }

    private void Stop()
    {
        HttpListener? listener;
        lock (_lock)
        {
            listener = _listener;
            _listener = null;
        }

        if (listener == null)
        {
            return;
        }

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.DomainUnload -= OnProcessExit;
        listener.Stop();
        listener.Close();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        Stop();
    }
}
