$solution = "$project.sln"
$test = "test\\Serilog.Sinks.Elasticsearch.Tests\\project.json"
$projectFolder = "src\\Serilog.Sinks.Elasticsearch"
$project = $projectFolder + "\\project.json"

function Invoke-Build()
{
    Write-Output "Building"

    & dotnet restore $test --verbosity Warning
    & dotnet restore $project --verbosity Warning
	
	# calculate version, only when on a branch
	if ($(git symbolic-ref HEAD) -ne 'fatal: ref HEAD is not a symbolic ref')
	{
		Write-Output "Determining version number using gitversion"
        
		& cd $projectFolder 
		& dotnet gitversion $project --verbosity Warning
		& cd "..\\.."
    }
    else
    {
		Write-Output "In a detached HEAD mode, unable to determine version number using gitversion"		
    }
  

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
