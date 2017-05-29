$solution = "$project.sln"
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

    #& dotnet restore $test --verbosity Warning
    #& dotnet restore $project --verbosity Warning

    #& nuget restore $test
    #& nuget restore $project

    & nuget restore $solution
	
	# calculate version, only when on a branch
	if ($(git log -n 1 --pretty=%d HEAD).Trim() -ne '(HEAD)')
	{
		Write-Output "Determining version number using gitversion"
        
		& cd $projectFolder 
		& dotnet gitversion  --verbosity Warning
		& cd "..\\.."
    }
    else
    {
		Write-Output "In a detached HEAD mode, unable to determine the version number using gitversion"		
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
