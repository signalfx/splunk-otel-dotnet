// <copyright file="Instrumentation.cs" company="Splunk Inc.">
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

using YamlDotNet.Serialization;

namespace MatrixHelper;

internal class Instrumentation
{
    public Instrumentation(string key, InstrumentedComponent instrumentedComponent, string? description, string stability, string support, SignalsList[] signalsList)
    : this([key], [instrumentedComponent], description, stability, support, [], signalsList, [])
    {
    }

    public Instrumentation(string key, InstrumentedComponent instrumentedComponent, string? description, string stability, string support, SignalsList signalsList)
        : this([key], [instrumentedComponent], description, stability, support, [], [signalsList], [])
    {
    }

    public Instrumentation(string key, InstrumentedComponent instrumentedComponent, string? description, string stability, string support, Dependency dependency, SignalsList[] signalsList)
        : this([key], [instrumentedComponent], description, stability, support, [dependency], signalsList, [])
    {
    }

    public Instrumentation(string[] keys, InstrumentedComponent[] instrumentedComponents, string? description, string stability, string support, Dependency[] dependencies, SignalsList[] signalsList, Setting[] settings)
    {
        Keys = keys;
        InstrumentedComponents = instrumentedComponents;
        Description = description;
        Stability = stability;
        Support = support;
        Dependencies = dependencies;
        Signals = signalsList;
        Settings = settings;
    }

    public string[] Keys { get; set; }

    public InstrumentedComponent[] InstrumentedComponents { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Description { get; set; }

    public string Stability { get; set; }

    public string Support { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dependency[] Dependencies { get; set; }

    public SignalsList[] Signals { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Setting[] Settings { get; set; }
}
