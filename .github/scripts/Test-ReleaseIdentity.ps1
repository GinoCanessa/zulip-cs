param(
    [Parameter(Mandatory)][string] $Tag,
    [string] $ExpectedCommit
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Tag -cnotmatch '^v(?<version>(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9a-z-]+(?:\.[0-9a-z-]+)*)?)\z')
{
    throw 'Tag must be v<major>.<minor>.<patch> with an optional lowercase SemVer prerelease suffix and no leading zeros or build metadata.'
}
$version = $Matches.version
$parts = $version -split '-', 2
foreach ($component in $parts[0].Split('.'))
{
    [int] $number = 0
    if (![int]::TryParse($component, [ref] $number) -or $number -gt 65534)
    {
        throw 'Version components must fit the .NET assembly version limits (0-65534).'
    }
}
if ($parts.Count -eq 2)
{
    foreach ($identifier in $parts[1].Split('.'))
    {
        if ($identifier -cmatch '^0[0-9]+\z')
        {
            throw 'Numeric prerelease identifiers must not have leading zeros.'
        }
    }
}
if ($ExpectedCommit -and $ExpectedCommit -cnotmatch '^[0-9a-f]{40}\z')
{
    throw 'ExpectedCommit must be a full lowercase commit SHA.'
}

git fetch --quiet --no-tags origin '+refs/heads/main:refs/remotes/origin/main' "+refs/tags/${Tag}:refs/tags/${Tag}" | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Cannot fetch the release tag and main branch.' }
$commit = git rev-parse --verify "refs/tags/${Tag}^{commit}"
if ($LASTEXITCODE -ne 0) { throw 'Release tag must resolve to a commit.' }
$commit = $commit.Trim()
if ($ExpectedCommit)
{
    $headCommit = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $headCommit -cne $ExpectedCommit -or $commit -cne $ExpectedCommit)
    {
        throw 'The checkout or release tag no longer matches the validated commit.'
    }
}
git merge-base --is-ancestor $commit refs/remotes/origin/main
if ($LASTEXITCODE -ne 0) { throw 'Release commit must be contained in origin/main.' }

[pscustomobject]@{ Version = $version; Tag = $Tag; Commit = $commit }
