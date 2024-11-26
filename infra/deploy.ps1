# Params
param (
    [string]$Subscription,
    [string]$Location = "southcentralus"
)


# Variables
$projectName = "chatapi"
$environmentName = "demo"
$templateFile = "main.bicep"
$deploymentName = "chatapidemodeployment"

# Clear account context and configure Azure CLI settings
az account clear
az config set core.enable_broker_on_windows=false
az config set core.login_experience_v2=off

# Login to Azure
az login 
az account set --subscription $Subscription

# Start the deployment
$deploymentOutput = az deployment sub create `
    --name $deploymentName `
    --location $Location `
    --template-file $templateFile `
    --parameters `
        environmentName=$environmentName `
        projectName=$projectName `
        location=$Location `
    --query "properties.outputs"


Write-Output "Deployment Complete"