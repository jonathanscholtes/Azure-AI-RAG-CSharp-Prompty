param name string
param location string

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: name
  location: location
  sku: {
    name: 'B2'
    tier: 'Basic'
    size: 'B2'
   family: 'B'
    capacity: 1
  }
  properties: {
    reserved: true
    isXenon: false
    hyperV: false
  }
}

output appServicePlanName string = appServicePlan.name

