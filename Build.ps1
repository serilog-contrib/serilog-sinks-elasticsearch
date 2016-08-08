$solution = "$project.sln"
$test = "test\\Serilog.Sinks.Elasticsearch.Tests\\project.json"
$projectFolder = "src\\Serilog.Sinks.Elasticsearch"
$project = $projectFolder + "\\project.json"

function Invoke-Build()
{
    Write-Output "Building"

    & dotnet restore $test --verbosity Warning
    & dotnet restore $project --verbosity Warning
	
	# calculate version
	& cd $projectFolder 
	& dotnet gitversion $project --verbosity Warning
	& cd "..\\.."

    & dotnet test $test -c Release
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "The tests failed"
        exit 1 
    }
  
    & dotnet pack $project -c Release -o .\artifacts 
  
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "Packing the sink failed"
        exit 1 
    }
    Write-Output "Building done"
}

$ErrorActionPreference = "Stop"
Invoke-Build 
