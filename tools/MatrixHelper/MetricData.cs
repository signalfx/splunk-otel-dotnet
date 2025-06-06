﻿// <copyright file="MetricData.cs" company="Splunk Inc.">
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

internal class MetricData
{
    private const string Custom = "Custom metric.";

    public MetricData(string metricName, string instrument, string description, string categoryNotes = Custom)
    {
        MetricName = metricName;
        Instrument = instrument;
        Description = description;
        CategoryNotes = categoryNotes;
    }

    public string MetricName { get; set; }

    public string Instrument { get; set; }

    public string Description { get; set; }

    public string CategoryNotes { get; set; }
}
