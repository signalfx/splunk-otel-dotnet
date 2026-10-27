// <copyright file="UpstreamProfilerStateResolverTests.cs" company="Splunk Inc.">
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

using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig;
using Splunk.OpenTelemetry.AutoInstrumentation.EffectiveConfig.Resolvers;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Tests;

public class UpstreamProfilerStateResolverTests
{
    private enum QueryResult
    {
        Succeeded,
        UnsupportedLayout
    }

    [Fact]
    public void QueryDecodesCommittedConfiguration()
    {
        var binding = new UpstreamProfilerStateResolver.RuntimeStateBinding(typeof(EnabledNativeMethods));
        var state = binding.Query();
        var expected = EffectiveProfilerFeatures.Cpu | EffectiveProfilerFeatures.Snapshot;
#if NET
        expected |= EffectiveProfilerFeatures.Memory;
#endif
        Assert.Equal(expected, state.Features);
        Assert.Equal(2000U, state.CpuSamplingInterval);
        Assert.Equal(40U, state.SnapshotSamplingInterval);
    }

    [Fact]
    public void AllDisabledCommittedConfigurationDoesNotReportPreparedExportersAsEnabled()
    {
        var state = new UpstreamProfilerStateResolver.RuntimeStateBinding(typeof(DisabledNativeMethods)).Query();
        Assert.Equal(EffectiveProfilerFeatures.None, state.Features);
        Assert.Equal(0U, state.CpuSamplingInterval);
        Assert.Equal(0U, state.SnapshotSamplingInterval);
    }

    [Fact]
    public void FailedQueryIsUnavailableRatherThanAnEmptyConfiguration()
    {
        var binding = new UpstreamProfilerStateResolver.RuntimeStateBinding(typeof(FailedNativeMethods));
        Assert.Throws<InvalidOperationException>(() => binding.Query());
    }

    [Fact]
    public void MissingOrChangedQueryContractIsRejected()
    {
        Assert.Throws<MissingMethodException>(() => new UpstreamProfilerStateResolver.RuntimeStateBinding(typeof(object)));
        Assert.Throws<MissingMethodException>(() => new UpstreamProfilerStateResolver.RuntimeStateBinding(typeof(InvalidNativeMethods)));
    }

#pragma warning disable SA1401 // Fields model the upstream reflection contract.
    private struct Configuration
    {
        public uint CpuSamplingIntervalMilliseconds;
        public uint SelectiveThreadSamplingIntervalMilliseconds;
        public uint MaxAllocationSamplesPerMinute;
    }

    private struct State
    {
        public Configuration CommittedConfiguration;
    }
#pragma warning restore SA1401

    private static class EnabledNativeMethods
    {
        public static QueryResult GetContinuousProfilerState(out State state)
        {
            state = new State
            {
                CommittedConfiguration = new Configuration
                {
                    CpuSamplingIntervalMilliseconds = 2000,
                    SelectiveThreadSamplingIntervalMilliseconds = 40,
                    MaxAllocationSamplesPerMinute = 100
                }
            };
            return QueryResult.Succeeded;
        }
    }

    private static class DisabledNativeMethods
    {
        public static QueryResult GetContinuousProfilerState(out State state)
        {
            state = default;
            return QueryResult.Succeeded;
        }
    }

    private static class FailedNativeMethods
    {
        public static QueryResult GetContinuousProfilerState(out State state)
        {
            state = default;
            return QueryResult.UnsupportedLayout;
        }
    }

    private static class InvalidNativeMethods
    {
        public static QueryResult GetContinuousProfilerState()
        {
            return QueryResult.Succeeded;
        }
    }
}
