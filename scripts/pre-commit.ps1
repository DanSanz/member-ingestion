# Layout check on the staged C# files. The build, the banned API rule and the tests run in
# scripts/check.ps1 and in CI.
$code = @(git diff --cached --name-only --diff-filter=ACM -- '*.cs')
if ($code.Count -eq 0) { exit 0 }

& dotnet format whitespace --folder --verify-no-changes --include @code
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Layout does not match .editorconfig. Fix it with: dotnet format whitespace --folder --include <files>'
    exit 1
}
exit 0
