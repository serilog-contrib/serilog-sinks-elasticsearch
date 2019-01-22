echo "In directory: $PSScriptRoot"

$solution = "serilog-sinks-elasticsearch.sln"
$test = "test\\Serilog.Sinks.Elasticsearch.Tests\\Serilog.Sinks.Elasticsearch.Tests.csproj"
[string[]]$projects = @(
    ("src\\Serilog.Sinks.Elasticsearch\\Serilog.Sinks.Elasticsearch.csproj"),
    ("src\\Serilog.Formatting.Elasticsearch\\Serilog.Formatting.Elasticsearch.csproj")
)

function Invoke-Build()
{
    Write-Output "Building"

	if(Test-Path .\artifacts) {
		echo "build: Cleaning .\artifacts"
		Remove-Item .\artifacts -Force -Recurse
	}

    & dotnet test $test -c Release
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "The tests failed"
        exit 1 
    }
  
    Write-Output "Creating packages"
    foreach ($project in $projects)
    {
        & dotnet pack $project -c Release -o ..\..\artifacts  --include-symbols --include-source /p:PackageVersion=$env:GitVersion_NuGetVersionV2    
    }
  
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "Packing the sink failed"
        exit 1 
    }
    Write-Output "Building done"
}

$ErrorActionPreference = "Stop"
Invoke-Build 
