# Replaces machine-specific absolute paths in CMake-generated .vcxproj files
# with MSBuild property references so the project is portable across machines.
#
# $(MSBuildProjectDirectory) = the build/ directory (where each .vcxproj lives)
# $(MSBuildProjectDirectory)\.. = the categorybars/ source directory
#
# Run once after cmake --fresh. If cmake re-runs (e.g. CMakeLists.txt changes),
# re-run this script to restore relative paths.

$buildDir   = Split-Path $PSScriptRoot -Parent | Split-Path -Leaf  # not used - use literal below
$absSource  = 'C:\Users\Vladimir\Documents\SSKA_Analizer_Source-1.3-1.4\WpfApp-2\WpfApp-1\SparEinKasse\categorybars'
$absBuild   = $absSource + '\build'
$absBuildFw = $absSource.Replace('\', '/') + '/build'
$absSourceFw= $absSource.Replace('\', '/')

# Order matters: replace the longer (build) path before the shorter (source) path
$replacements = @(
    @{ Old = $absBuild;   New = '$(MSBuildProjectDirectory)' },
    @{ Old = $absSource;  New = '$(MSBuildProjectDirectory)\..' },
    @{ Old = $absBuildFw; New = '$(MSBuildProjectDirectory)' },
    @{ Old = $absSourceFw;New = '$(MSBuildProjectDirectory)/..' }
)

$scriptDir = $PSScriptRoot
$files = Get-ChildItem $scriptDir -Filter '*.vcxproj'
$files += Get-ChildItem $scriptDir -Filter '*.vcxproj.filters'

foreach ($file in $files) {
    $content  = [System.IO.File]::ReadAllText($file.FullName)
    $modified = $content
    foreach ($r in $replacements) {
        $modified = $modified.Replace($r.Old, $r.New)
    }
    if ($modified -ne $content) {
        [System.IO.File]::WriteAllText($file.FullName, $modified, [System.Text.Encoding]::UTF8)
        Write-Host "Patched: $($file.Name)"
    } else {
        Write-Host "No change: $($file.Name)"
    }
}
Write-Host "Done."
