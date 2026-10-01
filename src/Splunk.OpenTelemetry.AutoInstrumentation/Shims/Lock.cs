// <copyright file="Lock.cs" company="Splunk Inc.">
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

#if !NET9_0_OR_GREATER
namespace Splunk.OpenTelemetry.AutoInstrumentation;

// .NET 9 added System.Threading.Lock. On earlier targets, this type keeps lock
// statements monitor-based while allowing call sites to use Lock uniformly.
// Based on: https://github.com/open-telemetry/opentelemetry-dotnet/blob/038da5d8f361ae59622eb1e98488bfc4732e863d/src/Shared/Shims/Lock.cs
internal sealed class Lock;
#endif
