param accountName string
param location string



resource storageAcct 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: accountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowSharedKeyAccess: true
    publicNetworkAccess: 'Enabled'
   
  }
}


resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAcct
  name: 'default'
}


resource loadContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  parent: blobServices
  name: 'load'
}

resource archiveContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  parent: blobServices
  name: 'archive'
  properties: {
    publicAccess: 'None'
  }
}
