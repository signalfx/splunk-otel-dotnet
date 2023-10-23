// <copyright file="AllInOne.cs" company="Splunk Inc.">
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

namespace MatrixHelper;

internal class AllInOne
{
    public AllInOne(string component, string version, Dependency[] dependencies, Setting[] settings, Instrumentation[] instrumentations, ResourceDetector[] resourceDetectors)
    {
        Component = component;
        Version = version;
        Dependencies = dependencies;
        Settings = settings;
        Instrumentations = instrumentations;
        ResourceDetectors = resourceDetectors;
    }

    public string Component { get; set; }

    public string Version { get; set; }

    public Dependency[] Dependencies { get; set; }

    public Setting[] Settings { get; set; }

    public Instrumentation[] Instrumentations { get; set; }

    public ResourceDetector[] ResourceDetectors { get; set; }
}
