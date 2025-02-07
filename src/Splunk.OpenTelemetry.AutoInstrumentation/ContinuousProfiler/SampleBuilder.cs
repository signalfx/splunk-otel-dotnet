// <copyright file="SampleBuilder.cs" company="Splunk Inc.">
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

#if NET

using Splunk.OpenTelemetry.AutoInstrumentation.Pprof.Proto.Profile;

namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class SampleBuilder
{
    private readonly Sample _sample = new();
    private readonly IList<ulong> _locationIds = new List<ulong>();
    private long? _value;

    public SampleBuilder AddLabel(Label label)
    {
        _sample.Labels.Add(label);
        return this;
    }

    public SampleBuilder SetValue(long val)
    {
        _value = val;
        return this;
    }

    public SampleBuilder AddLocationId(ulong locationId)
    {
        _locationIds.Add(locationId);
        return this;
    }

    public Sample Build()
    {
        _sample.LocationIds = _locationIds.ToArray();
        _sample.Values = _value.HasValue ? [_value.Value] : [];

        return _sample;
    }
}

#endif
