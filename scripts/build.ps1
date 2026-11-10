#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DalamudPath = $env:DALAMUD_HOME,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug'
)

# Builds against an inspected Dalamud reference directory (default: KR XIVLauncher dev hooks).
# Never copies Dalamud runtime or game files. Register the printed DLL path in Dev Plugin Locations.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'package-files.ps1')

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed (exit $LASTEXITCODE)." }
}

if (!$DalamudPath -and $env:APPDATA) { $DalamudPath = Join-Path $env:APPDATA 'XIVLauncherKR/addon/Hooks/dev' }
if (!$DalamudPath -or !(Test-Path -LiteralPath (Join-Path $DalamudPath 'Dalamud.dll') -PathType Leaf)) {
    throw 'Specify -DalamudPath or DALAMUD_HOME pointing to the directory containing Dalamud.dll.'
}
$lib = (Resolve-Path -LiteralPath $DalamudPath).Path
$version = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $lib 'Dalamud.dll')).Version
if ($version.Major -ne 15) { throw "Expected Dalamud 15, found $version. Review SDK compatibility before upgrading." }

Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'check-localization.ps1')
    Write-Output "Building against Dalamud $version at $lib"
    $plugin = 'src/DuckbillRelic.Plugin/DuckbillRelic.Plugin.csproj'
    Invoke-Dotnet restore $plugin --locked-mode "-p:DalamudLibPath=$lib/"
    Invoke-Dotnet build $plugin --no-restore -c $Configuration "-p:DalamudLibPath=$lib/"
    $output = Join-Path $root "src/DuckbillRelic.Plugin/bin/$Configuration"
    $manifest = Get-Content (Join-Path $output 'DuckbillRelic.json') -Raw | ConvertFrom-Json
    if ($manifest.InternalName -ne 'DuckbillRelic' -or $manifest.DalamudApiLevel -ne 15) { throw 'Unexpected generated manifest.' }
    foreach ($file in @(Get-RequiredPluginFiles)) {
        if (!(Test-Path -LiteralPath (Join-Path $output $file) -PathType Leaf)) { throw "Missing output: $file" }
    }
    Write-Output "Dev plugin DLL: $(Join-Path $output 'DuckbillRelic.dll')"
} finally { Pop-Location }
