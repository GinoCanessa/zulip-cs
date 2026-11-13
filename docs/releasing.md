# Release Process

`zulip-cs-lib` is published by `.github/workflows/nuget-library.yaml` only
when a GitHub Release for a `v<version>` tag is **published**. Merging to
`main`, pushing a tag, and creating a draft release do not publish to NuGet.
Both stable releases and prereleases use this process.

This follows the release style in `dotnet-fhir-packages` and
`CSharpLightDbGen`: validate the tagged source, run the cross-platform source
tests, build one immutable package candidate, and publish that exact package
using NuGet trusted publishing.

## Administrator Setup

Before the first release with this workflow:

1. Create the GitHub environment **`nuget.org`**, configure required reviewers
   or equivalent deployment protection, and allow **`v*` tags** in its
   deployment rules.
2. Set the environment variable **`NUGET_PUBLISH_ENVIRONMENT_READY=true`**
   after protection is configured.
3. Provide the secret **`NUGET_USER`** containing the NuGet profile username,
   not an email address or API key. An environment or repository secret works;
   prefer the environment scope for release-only configuration.
4. Create a [NuGet trusted-publishing policy](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
   with repository owner **`GinoCanessa`**, repository **`zulip-cs`**, workflow
   filename **`nuget-library.yaml`** (filename only), and environment
   **`nuget.org`**. Select the user or organization that owns
   **`zulip-cs-lib`** and grant publication of new versions of that package.

Committing the workflow does not configure GitHub or NuGet settings.
Only the publishing job has `id-token: write`. It fails without the readiness
variable and exchanges its GitHub OIDC token via `NuGet/login@v1` immediately
before pushing. No long-lived NuGet API key is used by the new workflow.

## Prepare and Publish

1. Complete the administrator setup above. The `nuget.org` environment must
   have deployment protection and allow `v*` tags; setting the readiness
   variable alone is not sufficient.
2. Choose a new three-component SemVer version, such as `1.0.0` or
   `1.0.1-beta.1`. Update `src/common.props`, the
   [changelog](../CHANGELOG.md), and version references in documentation.
   Verify a Release build, all target-framework tests, and the NuGet candidate
   before committing.
3. Merge the intended release source, including these workflows, into `main`,
   and tag that merged commit as exactly `v<version>` (`v1.0.0` for this release).
   Do not tag an unmerged development-branch commit.
4. Push the tag and optionally run **Publish dotnet library to NuGet.org**
   manually with that existing tag. Manual runs are **validation-only**:
   they test and pack but never authenticate to or publish on NuGet.
5. Publish the GitHub Release for that same tag using its changelog entry as
   release notes, then approve the `nuget.org` deployment when requested.
   Mark `v1.0.0` as a stable release, not a prerelease.

Version components must have no leading zeros and fit .NET assembly version
limits (`0-65534`). Prerelease identifiers must be lowercase; numeric
identifiers must have no leading zeros. Four-component versions, unprefixed
tags, and build metadata (`+...`) are rejected to avoid NuGet normalization
ambiguities.

Local builds use `src/common.props`. Release builds override
`Version` and `PackageVersion` from the tag, and set `RepositoryCommit` to
the exact tagged commit. Keep the source version aligned when preparing a
release; the release tag remains authoritative for the published package version.

The workflow freshly fetches the tag and `origin/main`, requires the tagged
commit to be contained in `main`, and requires release events to match their
original event SHA. The reusable Tests workflow checks out that exact commit
on Linux, Windows, and macOS and tests all existing target frameworks.
The candidate must contain the expected package ID, version, repository
commit, and libraries for `net11.0`, `net10.0`, `net9.0`, and `net8.0`.
It also includes the license, README, and changelog, with a version-specific
release-notes link in its NuGet metadata.
Build and publishing jobs use the .NET 11 SDK.

The candidate is retained as a GitHub Actions artifact for 90 days.
The publisher downloads it by artifact ID, checks its digest and recorded
SHA-512, and revalidates the tag and published GitHub Release before
authentication. It never rebuilds the package in the publishing job.

## Failed Releases and Migration

Investigate failures before rerunning. If the validated candidate still
exists and no package was published, rerun only the failed publishing job;
it downloads the same candidate. If a NuGet push succeeded, do not rerun it
or overwrite the version. Choose a new version for any changed source.
Duplicate-version pushes fail explicitly rather than reporting success.
Never move a published release tag to different source.

Old release reruns use the workflow from their original tag. Retain
`GINOC_NUGET` only if an outstanding old workflow still requires it; otherwise
remove the obsolete secret and revoke its API key after migration. New tags
must include the trusted-publishing workflow.
