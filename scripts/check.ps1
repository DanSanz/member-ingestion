# One command check: builds the job and runs the fast test suites.
# Stops at the first failure and exits with its code. Exits 0 when everything passes.
# The acceptance project is left out. It does not build until its task is done.
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$steps = @(
    @('build', 'src/MemberIngestion.Job'),
    @('test', 'tests/MemberIngestion.UnitTests'),
    @('test', 'tests/MemberIngestion.ModuleTests')
)

foreach ($step in $steps) {
    Write-Host "> dotnet $($step -join ' ')"
    & dotnet @step --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Check failed at: dotnet $($step -join ' ')"
        exit $LASTEXITCODE
    }
}

Write-Host 'Check passed.'
exit 0
