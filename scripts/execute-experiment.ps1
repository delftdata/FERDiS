# Start configure stage
$experimentKey = "test-exp-1"

$localKafkaDnsTemplate = 'localhost:3240{0}'
$clusterKafkaDnsTemplate = 'kafka-{0}.kafka.kafka.svc.cluster.local:9092'
$kafkaBrokerCount = 6

$preFailureSleepMs = 60000 #180000
$postFailureSleepMs = 60000 #180000
$metricTearDownDelayMs = 1000 #the amount of delay betwean tearing down the workers+generators and the  metric collectors

Write-Output "Starting experiment $($experimentKey)"

Write-Output "Setting up environment variables"
.\env\env-checkpoint.ps1 2 30
.\env\env-log 5 2
.\env\env-benchmark.ps1 1 0 1
.\env\env-generator.ps1 100 100 #throughput + gencalls

.\env\env-kafka.ps1 $localKafkaDnsTemplate $kafkaBrokerCount

.\env\env-azure-default.ps1
.\env\env-cra-default.ps1

# Uncomment below to print env variables
#dir env:


#first delete any remaining topics from kafka..
Write-Output "Deleting existing kafka topics"
.\util\delete-topics.ps1

.\env\env-kafka.ps1 $clusterKafkaDnsTemplate $kafkaBrokerCount

#prepare cra deployment (yields k8s yaml)
Write-Output "Preparing deployment file for BlackSP nodes"
.\util\prepare-benchmark.ps1
#prepare metric logger deployment
Write-Output "Preparing deployment file for metric nodes"
.\deployments\metric-deployment.ps1
#prepare generator deployment
Write-Output "Preparing deployment file for generator nodes"
.\deployments\generator-deployment.ps1 'text' 1

#apply blacksp deployment to kubectl
Write-Output "Deploying BlackSP nodes to kubernetes cluster"
kubectl apply -f .\deployment.yaml
#apply metric deployment to kubectl
Write-Output "Deploying metric nodes to kubernetes cluster"
kubectl apply -f .\metric-loggers.yaml
#apply generator deployment to kubectl
Write-Output "Deploying generator nodes to kubernetes cluster"
kubectl apply -f .\generators.yaml


#deployment deployed..
Write-Output "Experiment $($experimentKey) deployed, waiting $($preFailureSleepMs/1000) seconds before inserting failure.."

#let the system run
Start-Sleep -m $preFailureSleepMs
#insert failure(s)
Write-Output "Inserting failure"
kubectl delete pod crainst03-0

#let the system recover and resume
Write-Output "Failure inserted, waiting $($preFailureSleepMs/1000) seconds before tearing the cluster down.."
Start-Sleep -m $postFailureSleepMs


#delete deployments via kubectl
Write-Output "Tearing down cluster"
kubectl delete -f .\generators.yaml
kubectl delete -f .\deployment.yaml

Write-Output "Waiting $($metricTearDownDelayMs/1000) seconds before tearing the metric loggers down.." 
kubectl delete -f .\metric-loggers.yaml

#deployment deleted..

Write-Output "Teardown completed." 


Write-Output "Downloading log files" 
#download log files
azcopy copy 'https://vertexstore.blob.core.windows.net/logs?sp=rl&st=2021-05-07T21:11:23Z&se=2021-05-08T21:11:23Z&sv=2020-02-10&sr=c&sig=0XxbEzzjIROsFYTNk8gcG%2BPvZcfGrOtAY992HCN2Rg4%3D' './results/' --recursive
Rename-Item ./results/logs $experimentKey

#delete log container in azureblob