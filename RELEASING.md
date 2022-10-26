# Release Process

Versioning is using [MinVer](https://github.com/adamralph/minver).
No changes in the code are needed to correctly version this package.

1. Update the version in the following files:
   - [`Splunk.OTel.DotNet.psm1`](../Splunk.OTel.DotNet.psm1#L182)
   - [`docs/README.md`](./README.md)

1. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

1. Create a pull request on GitHub with the changes described in the changelog.

1. Test the described [examples](../examples/README.md).

1. Once the pull request has been merged, create a signed tag for the merged commit.
   You can do this using the following Bash snippet:

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push {remote-to-the-main-repo} $TAG
   ```

   After you've pushed the git tag, a `ci` GitHub workflow starts.

1. Publish a release in GitHub:

   - Use the [CHANGELOG.md](../CHANGELOG.md) content in the description.
   - Add the artifacts from [the `ci` GitHub workflow](https://github.com/signalfx/splunk-otel-dotnet/actions/workflows/ci.yml)
     related to the created tag.
