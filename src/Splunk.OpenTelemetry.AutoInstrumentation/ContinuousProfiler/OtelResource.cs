// <copyright file="OtelResource.cs" company="Splunk Inc.">
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

#if NET6_0_OR_GREATER
namespace Splunk.OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class OtelResource
{
    /// <summary>
    /// Returns attributes commonly added by Otel-based instrumentations to Otel resource.
    /// </summary>
    public static IList<KeyValuePair<string, string>> GetCommonAttributes()
    {
        // TODO CommonAttributes
        var attributes = new List<KeyValuePair<string, string>>();
        /*{
            new(CorrelationIdentifier.EnvKey, tracerSettings.Environment),
            new(CorrelationIdentifier.ServiceKey, serviceName),
            new("telemetry.sdk.name", "signalfx-" + TracerConstants.Library),
            new("telemetry.sdk.language", TracerConstants.Language),
            new("telemetry.sdk.version", TracerConstants.AssemblyVersion),
            new("splunk.distro.version", TracerConstants.AssemblyVersion)
        };

        attributes.AddRange(tracerSettings.GlobalTags);

        // TODO Splunk: ensure works if cgroupv2 is used
        var containerId = ContainerMetadata.GetContainerId();
        if (containerId != null)
        {
            attributes.Add(new KeyValuePair<string, string>("container.id", containerId));
        }

        var hostName = HostMetadata.Instance.Hostname;
        if (hostName != null)
        {
            attributes.Add(new KeyValuePair<string, string>("host.name", hostName));
        }

        // theoretically can return -1 for partial trust callers on .NET Framework
        var processId = DomainMetadata.Instance.ProcessId;

        attributes.Add(new KeyValuePair<string, string>("process.pid", processId.ToString()));*/
        return attributes;
    }
}
#endif
