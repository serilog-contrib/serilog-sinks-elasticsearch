echo "In directory: $PSScriptRoot"

$solution = "serilog-sinks-elasticsearch.sln"
$test = "test\\Serilog.Sinks.Elasticsearch.Tests\\Serilog.Sinks.Elasticsearch.Tests.csproj"
$projectFolder = "src\\Serilog.Sinks.Elasticsearch"
$project = $projectFolder + "\\Serilog.Sinks.Elasticsearch.csproj"

function Invoke-Build()
{
    Write-Output "Building"

	if(Test-Path .\artifacts) {
		echo "build: Cleaning .\artifacts"
		Remove-Item .\artifacts -Force -Recurse
	}

    & nuget restore $solution
    
    & dotnet test $test -c Release
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "The tests failed"
        exit 1 
    }
  
    & dotnet pack $project -c Release -o ..\..\artifacts 
  
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "Packing the sink failed"
        exit 1 
    }
    Write-Output "Building done"
}

$ErrorActionPreference = "Stop"
Invoke-Build 
