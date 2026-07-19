$ErrorActionPreference = "Stop"

Set-Location "$PSScriptRoot\.."

$solutionPath = ".\SistemaPlanificacionSNP.sln"
$testProjectName = "SistemaPlanificacionSNP.ControlCalidad.Api.Tests"
$testProjectPath = ".\$testProjectName"
$testCsproj = "$testProjectPath\$testProjectName.csproj"
$apiCsproj = ".\SistemaPlanificacionSNP.ControlCalidad.Api\SistemaPlanificacionSNP.ControlCalidad.Api.csproj"
$infraCsproj = ".\SistemaPlanificacionSNP.Infrastructure\SistemaPlanificacionSNP.Infrastructure.csproj"

if (-not (Test-Path $testProjectPath)) {
    dotnet new xunit -n $testProjectName -f net8.0
}

$alreadyInSolution = dotnet sln $solutionPath list | Select-String -SimpleMatch "$testProjectName\$testProjectName.csproj"
if (-not $alreadyInSolution) {
    dotnet sln $solutionPath add $testCsproj
}

dotnet add $testCsproj reference $apiCsproj
dotnet add $testCsproj reference $infraCsproj

dotnet add $testCsproj package Moq
dotnet add $testCsproj package FluentAssertions
dotnet add $testCsproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
dotnet add $testCsproj package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0
dotnet add $testCsproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add $testCsproj package coverlet.collector --version 6.0.0
dotnet add $testCsproj package xunit --version 2.5.3
dotnet add $testCsproj package xunit.runner.visualstudio --version 2.5.3
dotnet add $testCsproj package Microsoft.NET.Test.Sdk --version 17.8.0

Write-Host "Fase 1 completada: proyecto de pruebas de ControlCalidad configurado."
