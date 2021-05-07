$result = ./bin/BlackSP.Benchmarks.exe delete-topic *>&1

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