# Upstream bump process

1. Confirm that the target OpenTelemetry .NET Auto-Instrumentation release is
   immutable and that its distribution archives have GitHub artifact
   attestations. The expected signer is
   `open-telemetry/opentelemetry-dotnet-instrumentation/.github/workflows/release.yml`
   and the attestation source ref must be the exact release tag. If the upstream
   signer workflow changes, review the change before updating the corresponding
   constant in [`Build.cs`](../build/Build.cs).

1. Update the OpenTelemetry .NET AutoInstrumentation version in the following files:

   - [`build/Build.cs`](../build/Build.cs)
   - [`docs/advanced-config.md`](./advanced-config.md)
   - [`src/Splunk.OpenTelemetry.AutoInstrumentation/Splunk.OpenTelemetry.AutoInstrumentation.csproj`](../src/Splunk.OpenTelemetry.AutoInstrumentation/Splunk.OpenTelemetry.AutoInstrumentation.csproj)

1. Update the `test/Splunk.OpenTelemetry.AutoInstrumentation.IntegrationTests/BuildTests.DistributionStructure_*.verified.txt`
   files.

1. Update the [required env vars table](./advanced-config.md#manual-instrumentation).

1. Update the script templates based on changes in upstream:
   - [`splunk-otel-dotnet-install.sh.template`](../script-templates/splunk-otel-dotnet-install.sh.template)
   - [`Splunk.OTel.DotNet.psm1.template`](../script-templates/Splunk.OTel.DotNet.psm1.template)

1. Update [`dotnet-install.sh`](../scripts/dotnet-install.sh) and
   [`dotnet-install.ps1`](../scripts/dotnet-install.ps1) based on the upstream
   versions. Update [`dotnet-install.MIT.txt`](../scripts/dotnet-install.MIT.txt)
   with the source revision and checksums. Upstream has a script updating these
   scripts monthly.

1. Update [compatibility Matrix data](../tools/MatrixHelper) for

    - dependency:

        - version,
        - stability

    - settings changes,
    - instrumentation changes:

        - supported versions,
        - metrics,
        - stability,

    - resource detectors

        - attributes,
        - stability.

1. Update the [GitHub workflows](../.github/workflows) on changes in upstream.

1. Run `Workflow` without the verification opt-out on every supported platform.
   The build must pass both immutable-release and artifact-attestation
   verification before any upstream archive is extracted.
