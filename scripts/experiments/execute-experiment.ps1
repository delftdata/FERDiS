
#unique identifier for the experiment
$experimentKey = "projection-xxlarge-4x8cores-4x5k-gen-6-9-kafka"

#SAS for the azure log blob container
$azureSasUrl = 'http://145.100.59.144:10000/devstoreaccount1/logs'

#kafka settings
$localKafkaDnsTemplate = 'localhost:3240{0}'
$clusterKafkaDnsTemplate = 'kafka-{0}.kafka.kafka.svc.cluster.local:9092'
$kafkaBrokerCount = 6
$kafkaTopicPartitionCount = 12
$kafkaKustomizationPath = '.\kafka\variants\scale-6-9'
$kafkaInitSeconds = 30

#generator settings
$generatorShards = 4
$generatorThroughput = 5000
$generatorType = 'text' #possible types: 'text', 'graph', 'nexmark'
$generatorNexmarkGenCalls = 9999999 #...

#checkpoint settings
$checkpointMode = 1 #0 = uc, 1 = cc, 2 = cic
$checkpointIntervalSec = 30

#job settings
$jobType = 1 #0-6
$jobSize = 2 #0-2

#log settings
$logTargets = 5 #flags (1 = console, 2 = file, 4 = azure blob)
$logLevel = 2 #0-5 (Verbose-Debug-Information-Warning-Error-Fatal)


#experiment execution timing settings
$generatorStartDelayMs = 30000#90000
$preFailureSleepMs = 180000#120000 
$postFailureSleepMs = 0#210000 
$metricTearDownDelayMs = 10000 #the amount of delay betwean tearing down the workers+generators and the  metric collectors

#-----------------------------------start script-------------------------------------------
Write-Output "Starting experiment $($experimentKey)"

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
kubectl create namespace kafka
kubectl apply -k $kafkaKustomizationPath
$kafkaStartTime = Get-Date

Write-Output "Deploying Azurite"
kubectl apply -f .\azurite\azurite.yaml

#Write-Output "Deleting remaining log files from blob storage"
#azcopy rm $azureSasUrl --recursive

#prepare cra deployment (yields k8s yaml)
Write-Output "Preparing deployment file for BlackSP nodes"
.\lib\blacksp-deployment.ps1
#prepare metric logger deployment
Write-Output "Preparing deployment file for metric nodes"
.\lib\metric-deployment.ps1
#prepare generator deployment
Write-Output "Preparing deployment file for generator nodes"
.\lib\generator-deployment.ps1 $generatorType $generatorShards

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
kubectl delete pod crainst03-0
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
kubectl delete namespace kafka

#deployment deleted..
Write-Output "Teardown completed." 
Start-Sleep -m 1000 #fixed one second sleep to ensure all log files have indeed been written

Write-Output "Downloading log files" 
#download log files
New-Item -Path './results/' -Name "logs" -ItemType "directory" #ensure folder creation even if azcopy fails
azcopy copy $azureSasUrl './results/' --recursive --from-to BlobLocal
$failureTimes | Out-File ./results/logs/failures.log -Encoding "UTF8"
$startTime | Out-File ./results/logs/init_timestamp.log -Encoding "UTF8"
Rename-Item ./results/logs $experimentKey

Write-Output "Deleting deployment files from disk"
Remove-Item -path .\* -include *.yaml

[console]::beep(700,500)
[console]::beep(700,500)
[console]::beep(700,500) #BEEP BEEP BEEP - experiment completed