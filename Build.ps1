
$branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$env:APPVEYOR_REPO_BRANCH -ne $NULL];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $env:APPVEYOR_BUILD_NUMBER, 10); $false = "local" }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]

$solution = "$project.sln"
$test = "test\\Serilog.Sinks.Elasticsearch.Tests\\project.json"
$project = "src\\Serilog.Sinks.Elasticsearch\\project.json"

function Invoke-Build()
{
    Write-Output "Building $suffix"

    & dotnet restore $test --verbosity Warning
    & dotnet restore $project --verbosity Warning


    & dotnet test $test -c Release
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "The tests failed"
        exit 1 
    }
    if ($suffix -ne "")
    {
        & dotnet pack $project -c Release -o .\artifacts  --version-suffix=$suffix
    }
    else 
    {
        & dotnet pack $project -c Release -o .\artifacts 
    }
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "Packing the sink failed"
        exit 1 
    }
    Write-Output "Building $suffix done"
}

$ErrorActionPreference = "Stop"
Invoke-Build 
