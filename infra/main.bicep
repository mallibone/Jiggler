// Infrastructure for the keep-awake web page (web/index.html).
// Deploy (resource-group scope):
//   az group create --name rg-mousejiggler --location westeurope
//   az deployment group create --resource-group rg-mousejiggler --template-file infra/main.bicep

@description('Name of the Static Web App.')
param name string = 'mousejiggler-web'

@description('Region. The Free tier is only offered in a limited set of regions.')
@allowed(['westeurope', 'centralus', 'eastus2', 'westus2', 'eastasia'])
param location string = 'westeurope'

resource site 'Microsoft.Web/staticSites@2024-04-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // Content deploys via the GitHub Actions deployment token ("Other" source),
    // so no repositoryUrl/branch is wired here. The token is intentionally NOT
    // exposed as an output (outputs land in deployment history in plaintext) —
    // fetch it with `az staticwebapp secrets list` instead.
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
  }
}

output defaultHostname string = site.properties.defaultHostname
output staticWebAppName string = site.name
