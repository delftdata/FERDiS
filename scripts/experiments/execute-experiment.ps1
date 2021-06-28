
#SAS for the azure log blob container
$azureSasUrl = 'https://vertexstore.blob.core.windows.net/logs?sp=radl&st=2021-06-14T10:23:01Z&se=2022-06-15T10:23:00Z&sv=2020-02-10&sr=c&sig=0Z6EJSlBYm4J3jHXLEmrfZVUnccT%2FDTfAqDLL0Dkxyc%3D'#'http://145.100.57.248:10000/devstoreaccount1/logs'#145.100.59.144

#kafka settings
$localKafkaDnsTemplate = 'localhost:3240{0}'
$clusterKafkaDnsTemplate = 'kafka-{0}.kafka.kafka.svc.cluster.local:9092'
$kafkaBrokerCount = 1
$kafkaTopicPartitionCount = 24
$kafkaKustomizationPath = '.\kafka\variants\scale-1'
$kafkaInitSeconds = 30

#generator settings
$generatorShards = 1
$generatorThroughput = 600
$generatorType = 'graph' #possible types: 'text', 'graph', 'nexmark'
$generatorNexmarkGenCalls = 99999999 #...
$generatorSkipList = 'people'#'bids'

#checkpoint settings
$checkpointMode = 2 #0 = uc, 1 = cc, 2 = cic
$checkpointIntervalSec = 10

#job settings
$jobType = 6 #0-6
$jobSize = 1 #0-2

#log settings
$logTargets = 5 # flags (1 = console, 2 = file, 4 = azure blob)
$logLevel = 2 # 0-5 (Verbose-Debug-Information-Warning-Error-Fatal)


#unique identifier for the experiment
$keySuffix = "(0)"
$experimentKey = "job-$($jobType)-cp-$($checkpointMode)-$($checkpointIntervalSec)s-$($generatorShards * ($generatorThroughput / 1000))k-$($keySuffix)"

#experiment execution timing settings
$generatorStartDelayMs = 45000
$preFailureSleepMs = 90000 
$postFailureSleepMs = 150000 
$metricTearDownDelayMs = 20000 #the amount of delay betwean tearing down the workers+generators and the  metric collectors

#-----------------------------------start script-------------------------------------------
Write-Output "====================================="
Write-Output "Starting experiment $($experimentKey)"
Write-Output "====================================="

Write-Output "Setting up environment variables"
.\lib\env\env-checkpoint.ps1 $checkpointMode $checkpointIntervalSec
.\lib\env\env-log $logTargets $logLevel
.\lib\env\env-benchmark.ps1 $jobType $jobSize #job + size
.\lib\env\env-generator.ps1 $generatorThroughput $generatorNexmarkGenCalls #throughput + gencalls
.\lib\env\env-kafka.ps1 $clusterKafkaDnsTemplate $kafkaBrokerCount $kafkaTopicPartitionCount

.\lib\env\env-azure-default.ps1
.\lib\env\env-cra-default.ps1
# Uncomment to print env variables
#dir env:

# First 
#.\lib\env\env-kafka.ps1 $localKafkaDnsTemplate $kafkaBrokerCount
Write-Output "Deploying kafka"
kubectl apply -k $kafkaKustomizationPath
$kafkaStartTime = Get-Date

Write-Output "Deleting remaining log files from blob storage"
azcopy rm $azureSasUrl --recursive

#prepare cra deployment (yields k8s yaml)
Write-Output "Preparing deployment file for BlackSP nodes"
.\lib\blacksp-deployment.ps1
#prepare metric logger deployment
Write-Output "Preparing deployment file for metric nodes"
.\lib\metric-deployment.ps1
#prepare generator deployment
Write-Output "Preparing deployment file for generator nodes"
.\lib\generator-deployment.ps1 $generatorType $generatorShards $generatorSkipList

$kafkaInitSleepMs = $kafkaInitSeconds*1000 - (New-TimeSpan -Start $kafkaStartTime -End (Get-Date)).TotalMilliseconds;
Write-Output "Waiting for $($kafkaInitSleepMs/1000) seconds for kafka to initialise"
Start-Sleep -m $kafkaInitSleepMs

#apply blacksp deployment to kubectl
Write-Output "Deploying BlackSP nodes to kubernetes cluster"
kubectl apply -f .\deployment.yaml

#wait for startup before launching generators
Write-Output "Waiting for $($generatorStartDelayMs/1000) seconds before deploying generator and metric logger nodes"
Start-Sleep -m $generatorStartDelayMs

#apply metric deployment to kubectl
Write-Output "Deploying metric nodes to kubernetes cluster"
kubectl apply -f .\metric-loggers.yaml
#apply generator deployment to kubectl
Write-Output "Deploying generator nodes to kubernetes cluster"
kubectl apply -f .\generators.yaml

$startTime = (Get-Date).ToUniversalTime().ToString("hh:mm:ss:ffffff");
[console]::beep(700,500) # BEEP - experiment has started

#deployment deployed..
Write-Output "Experiment $($experimentKey) deployed, waiting $($preFailureSleepMs/1000) seconds before inserting failure.."
#let the system run
Start-Sleep -m $preFailureSleepMs

#insert failure(s)
Write-Output "Inserting failure"
#used to enrich logs with failure times
$failureTimes = 'timestamp';
$failureTimes += "`n"
$failureTimes += (Get-Date).ToUniversalTime().ToString("hh:mm:ss:ffffff");
$failureTimes += "`n"
#kubectl delete pod crainst03-0
#kubectl scale statefulsets crainst03 --replicas=0
#kubectl scale statefulsets crainst03 --replicas=1
#kubectl rollout restart statefulset crainst13
kubectl exec -it crainst18-0 -c crainst18 -- /bin/sh -c "kill 1"
[console]::beep(700,500) 
[console]::beep(700,500) # BEEP BEEP - failure inserted

#let the system recover and resume
Write-Output "Failure inserted, waiting $($postFailureSleepMs/1000) seconds before tearing the cluster down.."
Start-Sleep -m $postFailureSleepMs

#---------------------------- TEAR DOWN ----------------------------

#delete deployments via kubectl
Write-Output "Tearing down cluster"
kubectl delete -f .\generators.yaml
kubectl delete -f .\deployment.yaml

Write-Output "Waiting $($metricTearDownDelayMs/1000) seconds before tearing the metric loggers down.." 
Start-Sleep -m $metricTearDownDelayMs
kubectl delete -f .\metric-loggers.yaml

Write-Output "Tearing kafka down"
kubectl delete -k $kafkaKustomizationPath

Write-Output "Purging kubernetes DNS cache"
kubectl scale deployment coredns -n kube-system --replicas=0
kubectl scale deployment coredns -n kube-system --replicas=2

#deployment deleted..
Write-Output "Teardown completed." 
Start-Sleep -m 1000 #fixed one second sleep to ensure all log files have indeed been written

Write-Output "Downloading log files" 
#download log files
New-Item -Path './results/' -Name "logs" -ItemType "directory" #ensure folder creation even if azcopy fails
$failureTimes | Out-File ./results/logs/failures.log -Encoding "UTF8"
$startTime | Out-File ./results/logs/init_timestamp.log -Encoding "UTF8"
azcopy copy $azureSasUrl './results/' --recursive --from-to BlobLocal
Rename-Item ./results/logs $experimentKey

Write-Output "Deleting deployment files from disk"
Remove-Item -path .\* -include *.yaml

[console]::beep(700,500)
[console]::beep(700,500)
[console]::beep(700,500) #BEEP BEEP BEEP - experiment completed