# Release Process

Versioning is using [MinVer](https://github.com/adamralph/minver).
No changes in the code are needed to correctly version this package.

1. Update the version in the following files:
   - [`splunk-otel-dotnet-install.sh`](splunk-otel-dotnet-install.sh#L34)
   - [`Splunk.OTel.DotNet.psm1`](Splunk.OTel.DotNet.psm1#L246)
   - [`docs/README.md`](docs/README.md)

1. Update the [CHANGELOG.md](CHANGELOG.md) with the new release.

1. Create a pull request on GitHub with the changes described in the changelog.
   - `*scripts*` and `validate-documentation` jobs will fail
     because the release is not published yet.

1. Once the pull request has been merged, create a signed tag for the merged commit.
   You can do this using the following Bash snippet:

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push upstream $TAG
   ```

   After you've pushed the git tag, a `ci` GitHub workflow starts.

1. Publish a release in GitHub:

   - Use a draft created by `create-release` GitHub job in `ci` workflow.
   - Use the [CHANGELOG.md](CHANGELOG.md) content in the description.
   - Ensure that following flags under the are correctly set
      - `Set as a pre-release`
      - `Set as the latest release`

1. Ask [o11y-docs team](https://github.com/orgs/splunk/teams/o11y-docs)
to publish necessary updates to the [documentation](https://github.com/splunk/public-o11y-docs).
