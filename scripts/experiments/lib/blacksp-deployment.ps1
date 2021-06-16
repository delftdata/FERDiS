$result = C:\Projects\BlackSP\scripts\experiments\docker\bin\BlackSP.Benchmarks.exe benchmark -a *>&1

# Evaluate success/failure
if(!($LASTEXITCODE -eq 0))
{
    # Failed, reconstruct stderr
    Write-Error ($result -join [System.Environment]::NewLine)
} 
else
{
    Write-Output $result
}
