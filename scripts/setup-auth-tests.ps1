$ErrorActionPreference = "Stop"

Set-Location "$PSScriptRoot\.."

$solutionPath = ".\SistemaPlanificacionSNP.sln"
$testProjectName = "SistemaPlanificacionSNP.Auth.Api.Tests"
$testProjectPath = ".\$testProjectName"
$testCsproj = "$testProjectPath\$testProjectName.csproj"
$authApiCsproj = ".\SistemaPlanificacionSNP.Auth.Api\SistemaPlanificacionSNP.Auth.Api.csproj"
$infraCsproj = ".\SistemaPlanificacionSNP.Infrastructure\SistemaPlanificacionSNP.Infrastructure.csproj"

if (-not (Test-Path $testProjectPath)) {
    dotnet new xunit -n $testProjectName -f net8.0
}

$alreadyInSolution = dotnet sln $solutionPath list | Select-String -SimpleMatch "$testProjectName\$testProjectName.csproj"
if (-not $alreadyInSolution) {
    dotnet sln $solutionPath add $testCsproj
}

dotnet add $testCsproj reference $authApiCsproj
dotnet add $testCsproj reference $infraCsproj

dotnet add $testCsproj package Moq
dotnet add $testCsproj package FluentAssertions
dotnet add $testCsproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0

Write-Host "Fase 1 completada: proyecto de pruebas configurado."
