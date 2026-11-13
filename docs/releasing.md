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
3. Add the environment secret **`NUGET_USER`** containing the NuGet profile
   username, not an email address or API key.
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

1. Merge the intended release source, including these workflows, into `main`.
2. Choose a new three-component SemVer version, such as `0.0.1-beta.6` or
   `0.1.0`, and tag that commit as exactly `v<version>`.
3. Push the tag and optionally run **Publish dotnet library to NuGet.org**
   manually with that existing tag. Manual runs are **validation-only**:
   they test and pack but never authenticate to or publish on NuGet.
4. Publish the GitHub Release for that same tag, then approve the `nuget.org`
   deployment when requested.

Version components must have no leading zeros and fit .NET assembly version
limits (`0-65534`). Prerelease identifiers must be lowercase; numeric
identifiers must have no leading zeros. Four-component versions, unprefixed
tags, and build metadata (`+...`) are rejected to avoid NuGet normalization
ambiguities.

Local builds continue using `src/common.props`. Release builds override
`Version` and `PackageVersion` from the tag, and set `RepositoryCommit` to
the exact tagged commit. Updating the local development version is optional;
the release tag determines the published package version.

The workflow freshly fetches the tag and `origin/main`, requires the tagged
commit to be contained in `main`, and requires release events to match their
original event SHA. The reusable Tests workflow checks out that exact commit
on Linux, Windows, and macOS and tests all existing target frameworks.
The candidate must contain the expected package ID, version, repository
commit, and libraries for `net11.0`, `net10.0`, `net9.0`, and `net8.0`.
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
