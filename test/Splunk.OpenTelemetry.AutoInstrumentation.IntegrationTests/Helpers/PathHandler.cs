// <copyright file="PathHandler.cs" company="Splunk Inc.">
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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using Microsoft.AspNetCore.Http;

namespace Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests.Helpers;

public class PathHandler
{
    public PathHandler(RequestDelegate @delegate, string path)
    {
        Delegate = @delegate;
        Path = path;
    }

    public RequestDelegate Delegate { get; }

    public string Path { get; }
}
#endif
