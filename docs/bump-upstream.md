# Upstream bump process

1. Update the OpenTelemetry .NET AutoInstrumentation version in the following files:

   - [`build/Build.cs`](../build/Build.cs)
   - [`docs/advanced-config.md`](./advanced-config.md)
   - [`src/Splunk.OpenTelemetry.AutoInstrumentation/Splunk.OpenTelemetry.AutoInstrumentation.csproj`](../src/Splunk.OpenTelemetry.AutoInstrumentation/Splunk.OpenTelemetry.AutoInstrumentation.csproj)
   - [`test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/Helpers/ResourceExpectorExtensions.cs`](../test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/Helpers/ResourceExpectorExtensions.cs)

1. Update the `test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/BuildTests.DistributionStructure_*.verified.txt`
   files.

1. Update the [required env vars table](./advanced-config.md#manual-instrumentation).

1. Update the script templates based on changes in upstream:
   - [`splunk-otel-dotnet-install.sh.template`](../script-templates/splunk-otel-dotnet-install.sh.template)
   - [`Splunk.OTel.DotNet.psm1.template`](../script-templates/Splunk.OTel.DotNet.psm1.template)

1. Update the [GitHub workflows](../.github/workflows) on changes in upstream.
