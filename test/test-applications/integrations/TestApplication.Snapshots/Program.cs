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

using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace TestApplication.Snapshots;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        using var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseAuthorization();

        app.MapControllers();

        app.Start();

        var server = (IServer?)app.Services.GetService(typeof(IServer));
        var addressFeature = server?.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.First();
        using var httpClient = new HttpClient();

        var requestUri = $"{address}/weatherforecast";
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        httpRequestMessage.Headers.Add("baggage", "splunk.trace.snapshot.volume=highest");
        httpClient.Send(httpRequestMessage);

        // Upstream buffer processor doesn't guarantee outstanding buffers are flushed on shutdown.
        // Wait a bit to allow snapshots to be sent.
        // Additionally, first export to local collector can take ~2s.
        // Might need adjustments based on environment.
        Thread.Sleep(3000);
    }
}
