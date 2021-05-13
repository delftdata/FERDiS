
$experimentKey = "projection-1-cic-30-100(1)"

$localKafkaDnsTemplate = 'localhost:3240{0}'
$clusterKafkaDnsTemplate = 'kafka-{0}.kafka.kafka.svc.cluster.local:9092'
$kafkaBrokerCount = 6

$generatorStartDelayMs = 45000
$preFailureSleepMs = 120000 #180000
$postFailureSleepMs = 150000 #180000
$metricTearDownDelayMs = 10000 #the amount of delay betwean tearing down the workers+generators and the  metric collectors

$azureSasUrl = 'https://vertexstore.blob.core.windows.net/logs?sp=rdl&st=2021-05-12T20:01:24Z&se=2021-05-13T20:01:24Z&sv=2020-02-10&sr=c&sig=DgptVHg2v9h84zO%2BiOIXzfqmIqzL78hC0bRT8R205mY%3D'

$failureTimes = 'timestamp';
$failureTimes += "`n"

Write-Output "Starting experiment $($experimentKey)"

Write-Output "Setting up environment variables"
.\env\env-checkpoint.ps1 2 30
.\env\env-log 5 2
.\env\env-benchmark.ps1 1 1 1 #infra + job + size
.\env\env-generator.ps1 100 100 #throughput + gencalls

.\env\env-kafka.ps1 $localKafkaDnsTemplate $kafkaBrokerCount

.\env\env-azure-default.ps1
.\env\env-cra-default.ps1

# Uncomment to print env variables
#dir env:


#first delete any remaining topics from kafka..
Write-Output "Deploying kafka"
#.\util\delete-topics.ps1
kubectl apply -k C:\Projects\kafka-k8s\variants\scale-6-9
$kafkaStartTime = Get-Date

Write-Output "Deleting remaining log files from blob storage"
azcopy rm $azureSasUrl --recursive

.\env\env-kafka.ps1 $clusterKafkaDnsTemplate $kafkaBrokerCount

#prepare cra deployment (yields k8s yaml)
Write-Output "Preparing deployment file for BlackSP nodes"
.\util\prepare-benchmark.ps1
#prepare metric logger deployment
Write-Output "Preparing deployment file for metric nodes"
.\deployments\metric-deployment.ps1
#prepare generator deployment
Write-Output "Preparing deployment file for generator nodes"
.\deployments\generator-deployment.ps1 'text' 1 #BIG NOTE: CURRENTLY HARDCODED TO TEXT DATA GENERATION!!

$kafkaInitSleepMs = 60*1000 - (New-TimeSpan -Start $kafkaStartTime -End (Get-Date)).TotalMilliseconds;
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

#Start-Sleep -m 5000
$startTime = (Get-Date).ToUniversalTime().ToString("hh:mm:ss:ffffff");

#deployment deployed..
Write-Output "Experiment $($experimentKey) deployed, waiting $($preFailureSleepMs/1000) seconds before inserting failure.."

#let the system run
Start-Sleep -m $preFailureSleepMs
#insert failure(s)
Write-Output "Inserting failure"
$failureTimes += (Get-Date).ToUniversalTime().ToString("hh:mm:ss:ffffff");
$failureTimes += "`n"
kubectl delete pod crainst03-0


#let the system recover and resume
Write-Output "Failure inserted, waiting $($postFailureSleepMs/1000) seconds before tearing the cluster down.."
Start-Sleep -m $postFailureSleepMs


#delete deployments via kubectl
Write-Output "Tearing down cluster"
kubectl delete -f .\generators.yaml
kubectl delete -f .\deployment.yaml

Write-Output "Waiting $($metricTearDownDelayMs/1000) seconds before tearing the metric loggers down.." 
Start-Sleep -m $metricTearDownDelayMs
kubectl delete -f .\metric-loggers.yaml

Write-Output "Tearing kafka down"
kubectl delete -k C:\Projects\kafka-k8s\variants\scale-6-9

#deployment deleted..
Write-Output "Teardown completed." 
Start-Sleep -m 1000 #fixed one second sleep to ensure all log files have indeed been written

Write-Output "Downloading log files" 
#download log files
azcopy copy $azureSasUrl './results/' --recursive
$failureTimes | Out-File ./results/logs/failures.log -Encoding "UTF8"
$startTime | Out-File ./results/logs/init_timestamp.log -Encoding "UTF8"
Rename-Item ./results/logs $experimentKey
