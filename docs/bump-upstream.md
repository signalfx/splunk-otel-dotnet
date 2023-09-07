# Upstream bump process

1. Update the OpenTelemetry .NET AutoInstrumentation version in the following files:

   - [`build/Build.cs`](../build/Build.cs)
   - [`docs/advanced-config.md`](./advanced-config.md)
   - [`docs/README.md`](./README.md)
   - [`src/Splunk.OpenTelemetry.AutoInstrumentation/Splunk.OpenTelemetry.AutoInstrumentation.csproj`](../src/Splunk.OpenTelemetry.AutoInstrumentation/Splunk.OpenTelemetry.AutoInstrumentation.csproj)
   - [`test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/SmokeTests.cs`](../test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/Helpers/ResourceExpectorExtensions.cs)

1. Update the `test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/BuildTests.DistributionStructure_*.verified.txt`
   files.

1. Update the [required env vars table](./advanced-config.md#manual-instrumentation).

1. Update the scripts based on changes in upstream:
   - [`splunk-otel-dotnet-install.sh`](../splunk-otel-dotnet-install.sh)
   - [`Splunk.OTel.DotNet.psm1`](../Splunk.OTel.DotNet.psm1)

1. Update the [GitHub workflows](../.github/workflows) on changes in upstream.
