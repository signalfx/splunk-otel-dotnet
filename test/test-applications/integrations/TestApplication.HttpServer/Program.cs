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

using TestApplication.HttpServer;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var builder = WebApplication.CreateBuilder(args);

const string requestPath = "/request";
const string shutdownPath = "/shutdown";
var app = builder.Build();
using var observer = new LifetimeObserver(app, shutdownPath);

app.UseWelcomePage("/alive-check");
app.MapGet(requestPath, () => "TestApplication.HttpServer");
app.UseWelcomePage(shutdownPath);

await app.RunAsync();
