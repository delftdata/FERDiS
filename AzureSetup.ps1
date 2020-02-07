$resourceGroup="Marc_CRA_Test"
$aksName="CRA-Test-Cluster"
$location="westeurope"
$vmSize="Standard_DS1_v2"#"Standard_DS11-1_v2" #"Standard_B2s"
#IMPORTANT: vm-count should be 3 in production!
$vmCount=3

#if cluster autoscaler is enabled.. 
$minScale=1
$maxScale=3

#create the resource group
az group create `
	--location $location `
	--name $resourceGroup

# Create the actual azure kubernetes cluster
az aks create `
	--resource-group $resourceGroup `
	--name $aksName `
	--node-count $vmCount `
	--location $location `
	--node-vm-size $vmSize `
	--generate-ssh-keys

Write-Output "Done.. Exiting in 60 seconds"
# Wait for azure to propagate the service principal creation
Start-Sleep -Seconds 60
#	--enable-cluster-autoscaler `
#	--min-count $minScale `
#	--max-count $maxScale `