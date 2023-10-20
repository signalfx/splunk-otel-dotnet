// <copyright file="Dependency.cs" company="Splunk Inc.">
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

internal class Dependency
{
    public Dependency(string name, string sourceHref, string? packageHref, string version, string stability)
    {
        Name = name;
        SourceHref = sourceHref;
        PackageHref = packageHref;
        Version = version;
        Stability = stability;
    }

    public string Name { get; set; }

    public string SourceHref { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? PackageHref { get; set; }

    public string Version { get; set; }

    public string Stability { get; set; }
}
