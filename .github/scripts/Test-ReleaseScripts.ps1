$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$identityScript = Join-Path $PSScriptRoot 'Test-ReleaseIdentity.ps1'
$packageScript = Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1'
$temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "zulip-release-tests-$([guid]::NewGuid())"
$origin = Join-Path $temporaryDirectory 'origin.git'
$source = Join-Path $temporaryDirectory 'source'
$checkout = Join-Path $temporaryDirectory 'checkout'
$candidate = Join-Path $temporaryDirectory 'candidate'
$version = '0.0.1-beta.6'
$commit = ''

function Invoke-Git
{
    & git @args | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Git fixture command failed: $args" }
}

function Assert-Throws([scriptblock] $Action, [string] $Message)
{
    try { & $Action | Out-Null }
    catch
    {
        if (!$_.Exception.Message.Contains($Message))
        {
            throw "Expected '$Message', got '$($_.Exception.Message)'."
        }
        return
    }
    throw "Expected failure containing '$Message'."
}

function New-Candidate(
    [string] $PackageVersion = $version,
    [string] $PackageCommit = $commit,
    [string[]] $Frameworks = @('net8.0', 'net9.0', 'net10.0', 'net11.0')
)
{
    $path = Join-Path $candidate "zulip-cs-lib.$version.nupkg"
    $stream = [IO.File]::Open($path, [IO.FileMode]::Create)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try
    {
        $writer = [IO.StreamWriter]::new($archive.CreateEntry('zulip-cs-lib.nuspec').Open())
        try
        {
            $writer.Write("<package xmlns='http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd'><metadata><id>zulip-cs-lib</id><version>$PackageVersion</version><repository type='git' commit='$PackageCommit' /></metadata></package>")
        }
        finally { $writer.Dispose() }
        foreach ($framework in $Frameworks)
        {
            $archive.CreateEntry("lib/$framework/zulip-cs-lib.dll") | Out-Null
        }
    }
    finally
    {
        $archive.Dispose()
        $stream.Dispose()
    }
}

try
{
    New-Item -ItemType Directory -Path $temporaryDirectory, $candidate | Out-Null
    Invoke-Git init --quiet --bare --initial-branch=main $origin
    Invoke-Git init --quiet --initial-branch=main $source
    Invoke-Git -C $source -c user.name=ReleaseTests -c user.email=release-tests@example.invalid commit --quiet --allow-empty -m 'Release fixture'
    $commit = (git -C $source rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve fixture commit.' }
    Invoke-Git -C $source tag v0.0.1
    Invoke-Git -C $source -c user.name=ReleaseTests -c user.email=release-tests@example.invalid tag -a "v$version" -m 'Prerelease fixture'
    Invoke-Git -C $source remote add origin $origin
    Invoke-Git -C $source push --quiet origin main --tags
    Invoke-Git clone --quiet $origin $checkout

    Push-Location $checkout
    try
    {
        foreach ($tag in @('v0.0.1', "v$version"))
        {
            $release = & $identityScript -Tag $tag -ExpectedCommit $commit
            if ($release.Commit -cne $commit -or $release.Version -cne $tag.Substring(1))
            {
                throw 'Lightweight or annotated release tag resolved incorrectly.'
            }
        }
        foreach ($tag in @('0.0.1', 'v00.0.1', 'v0.0.1.0', 'v0.0.1+build', 'v0.0.1-BETA'))
        {
            Assert-Throws { & $identityScript -Tag $tag } 'Tag must be'
        }
        Assert-Throws { & $identityScript -Tag 'v0.0.1-beta.01' } 'Numeric prerelease'
        Assert-Throws { & $identityScript -Tag 'v65535.0.0' } 'assembly version limits'
        Assert-Throws { & $identityScript -Tag "v$version" -ExpectedCommit 'main' } 'full lowercase commit SHA'
        Assert-Throws { & $identityScript -Tag "v$version" -ExpectedCommit ('a' * 40) } 'no longer matches'

        Invoke-Git -C $source -c user.name=ReleaseTests -c user.email=release-tests@example.invalid commit --quiet --allow-empty -m 'Unmerged fixture'
        Invoke-Git -C $source tag v0.0.2
        Invoke-Git -C $source push --quiet origin refs/tags/v0.0.2
        Assert-Throws { & $identityScript -Tag v0.0.2 } 'contained in origin/main'
        Invoke-Git -C $source tag --force "v$version"
        Invoke-Git -C $source push --quiet --force origin "refs/tags/v$version"
        Assert-Throws { & $identityScript -Tag "v$version" -ExpectedCommit $commit } 'no longer matches'
    }
    finally { Pop-Location }

    New-Candidate
    $hash = & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit
    if ($hash -cnotmatch '^[0-9A-F]{128}\z') { throw 'Candidate did not return a SHA-512 hash.' }
    & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit -ExpectedSha512 $hash | Out-Null
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit -ExpectedSha512 ('0' * 128) } 'SHA-512'
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit -ExpectedSha512 '' } 'SHA-512'
    New-Candidate -PackageVersion '0.0.1'
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit } 'metadata does not match'
    New-Candidate -PackageCommit ('a' * 40)
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit } 'metadata does not match'
    New-Candidate -Frameworks @('net8.0', 'net9.0', 'net10.0')
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit } 'missing the net11.0 library'
    New-Candidate -Frameworks @('net8.0', 'net10.0', 'net11.0')
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit } 'missing the net9.0 library'
    New-Candidate
    Copy-Item -LiteralPath (Join-Path $candidate "zulip-cs-lib.$version.nupkg") -Destination (Join-Path $candidate 'unexpected.nupkg')
    Assert-Throws { & $packageScript -CandidateDirectory $candidate -Version $version -Commit $commit } 'exactly the expected'

    Write-Host 'Release identity and package validation tests passed.'
}
finally
{
    if (Test-Path -LiteralPath $temporaryDirectory)
    {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
