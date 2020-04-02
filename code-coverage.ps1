param (
    [string]$format = "opencover",
    [bool]$nohtml = $false
)

if (-Not (Get-Command -Name reportgenerator -ErrorAction SilentlyContinue))
{
    Write-Output "Installing reportgenerator"
    Invoke-Expression "dotnet tool install --global dotnet-reportgenerator-globaltool --version 4.0.9"
}

function Get-BaseCommand {
    Param ([string]$basename, [string]$name)
    return "dotnet test ./$($BaseName)/$($Name) /p:Exclude=[*]**.UnitTest.Utilities.*%2c[*]**.Migrations.* /p:CollectCoverage=true";
}

Write-Output "Getting csproj files"
$reports = [System.Collections.ArrayList]@()
$count = Get-ChildItem -Path .\ -Recurse -Filter *.UnitTests.csproj -File | Measure-Object | ForEach-Object{$_.Count}
Get-ChildItem -Path .\ -Recurse -Filter *.UnitTests.csproj -File | ForEach-Object {
    Write-Output "Running tests of project $($_.BaseName)/$($_.Name)"
    if ($reports.Count -eq $count-1) {
        Write-Output "Generating unified report using $($format) format"
        Invoke-Expression "$(Get-BaseCommand -basename $_.BaseName -name $_.Name) /p:MergeWith='..\result.json' /p:CoverletOutput='..\coverage.$($format).xml' /p:CoverletOutputFormat='$($format)'"
    }
    elseif ($reports.Count -gt 0) {
        Write-Output "Merging results coverage with project $($reports[$reports.Count-1])"
        Invoke-Expression "$(Get-BaseCommand -basename $_.BaseName -name $_.Name) /p:MergeWith='..\result.json' /p:CoverletOutput='..\result.json'"
    } else {
        Invoke-Expression "$(Get-BaseCommand -basename $_.BaseName -name $_.Name) /p:CoverletOutput='..\result.json'"
    }

    $reports.Add(".\$($_.BaseName)")
}

$reportsText = $reports -join "`n"

Write-Output "Run tests finished. Tested projects: `n$($reportsText)"

if ($nohtml -eq $false) {
    Invoke-Expression "reportgenerator '-reports:./coverage.$($format).xml' '-targetdir:./test-coverage-report'"
    
	Remove-Item ./result.json
	Remove-Item ./coverage.opencover.xml
}

Read-Host -Prompt "Press Enter to view report and exit"
Invoke-Item ./test-coverage-report/index.htm