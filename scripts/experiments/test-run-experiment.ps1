#generator settings
$generatorShards = 1
$generatorThroughput = 100
$generatorType = 'graph' #possible types: 'text', 'graph', 'nexmark'
$generatorSkipList = 'people'#'bids'

#checkpoint settings
$checkpointMode = 2 #0 = uc, 1 = cc, 2 = cic
$checkpointIntervalSec = 10

#job settings
$jobType = 6 #0-6
$repCount = 1

$instanceToKill = "crainst14"

For ($i=0; $i -lt $repCount; $i++) {
    Write-Output ">>>> Starting experiment execution"
    .\execute-experiment.ps1 $jobType $i $generatorShards $generatorThroughput $generatorType $generatorSkipList $checkpointMode $checkpointIntervalSec $instanceToKill
    Write-Output ">>>> Waiting for next experiment execution"
    Start-Sleep -s 30 #ensure pods have a chance to terminate or next run cant start due to insufficient available resources
}

