param(
    [Parameter(Mandatory)][string] $CandidateDirectory,
    [Parameter(Mandatory)][string] $Version,
    [Parameter(Mandatory)][string] $Commit,
    [string] $ExpectedSha512
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$packages = @(Get-ChildItem -LiteralPath $CandidateDirectory -Filter '*.nupkg' -File)
if ($packages.Count -ne 1 -or $packages[0].Name -cne "zulip-cs-lib.$Version.nupkg")
{
    throw 'Candidate must contain exactly the expected zulip-cs-lib package.'
}
$package = $packages[0]
$hash = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA512).Hash
if ($PSBoundParameters.ContainsKey('ExpectedSha512') -and
    ($ExpectedSha512 -cnotmatch '^[0-9A-F]{128}\z' -or $hash -cne $ExpectedSha512))
{
    throw 'Candidate SHA-512 does not match the validated package.'
}

$archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
try
{
    $manifests = @($archive.Entries | Where-Object { $_.FullName -clike '*.nuspec' })
    if ($manifests.Count -ne 1) { throw 'Candidate must contain exactly one NuGet manifest.' }
    $reader = [IO.StreamReader]::new($manifests[0].Open())
    try { [xml] $manifest = $reader.ReadToEnd() }
    finally { $reader.Dispose() }
    $metadata = $manifest.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    if (!$metadata) { throw 'Candidate has no NuGet metadata.' }
    $id = $metadata.SelectSingleNode("*[local-name()='id']")
    $packageVersion = $metadata.SelectSingleNode("*[local-name()='version']")
    $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
    if (!$id -or $id.InnerText -cne 'zulip-cs-lib' -or
        !$packageVersion -or $packageVersion.InnerText -cne $Version -or
        !$repository -or $repository.GetAttribute('commit') -cne $Commit)
    {
        throw 'Candidate metadata does not match the release package, version, and commit.'
    }
    foreach ($framework in @('net8.0', 'net9.0', 'net10.0', 'net11.0'))
    {
        if (!$archive.GetEntry("lib/$framework/zulip-cs-lib.dll"))
        {
            throw "Candidate is missing the $framework library."
        }
    }
}
finally { $archive.Dispose() }

$hash
