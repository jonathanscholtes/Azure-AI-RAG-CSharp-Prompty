targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name which is used to generate a short unique hash for each resource')
param environmentName string

@minLength(1)
@maxLength(64)
@description('Name which is used to generate a short unique hash for each resource')
param projectName string

@minLength(1)
@description('Primary location for all resources')
param location string


var resourceToken = uniqueString(environmentName,location,az.subscription().subscriptionId)
var abbreviations = loadJsonContent('abbreviations.json')

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${projectName}-${environmentName}-${location}-${resourceToken}'
  location: location
}


module managedIdentity 'core/security/managed-identity.bicep' = {
  name: 'managed-identity'
  scope: resourceGroup
  params: {
    name: 'id-${projectName}-${environmentName}'
    location: location
  }
}


module storage 'core/storage/blob-storage-account.bicep' = {
  name: 'storage'
  scope: resourceGroup
  params: {
    accountName: 'sa${projectName}${environmentName}'
    location: location
  }
  dependsOn:[managedIdentity]
}

module openAIService 'core/ai/openai/openai-account.bicep' = {
  name: 'openAIService'
  scope: resourceGroup
  params: {
    name: '${abbreviations.openAI}-${projectName}-${environmentName}-${resourceToken}'
    location: location
    identityName: managedIdentity.outputs.managedIdentityName
    customSubdomain: 'openai-app-${resourceToken}'
  }
  dependsOn: [managedIdentity]
}

module search 'core/search/search-services.bicep' = {
  name: 'search'
  scope: resourceGroup
  params: {
    name: '${abbreviations.aiSearch}-${projectName}-${environmentName}-${resourceToken}'
    location: location
    semanticSearch: 'standard'
    disableLocalAuth: true
  }
  dependsOn:[storage]
}

module database 'app/database.bicep' = {
  name: 'database'
  scope: resourceGroup
  params: {
    accountName: '${abbreviations.cosmosDbAccount}-${projectName}-${environmentName}-${resourceToken}'
    location: 'centralus'
  }
}



module monitoring 'core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: resourceGroup
  params: {
    location: location
    logAnalyticsName: 'log-${projectName}-${environmentName}'
    applicationInsightsName: 'appi-${projectName}-${environmentName}'
    applicationInsightsDashboardName: 'appid-${projectName}-${environmentName}'
  }
}



