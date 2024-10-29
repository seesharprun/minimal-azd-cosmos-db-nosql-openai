targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Id of the principal that is deploying the template')
param deploymentUserPrincipalId string = ''

// serviceName is used as value for the tag (azd-service-name) azd uses to identify deployment host
param serviceName string = 'web'

var resourceToken = toLower(uniqueString(resourceGroup().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
  repo: 'https://github.com/azurecosmosdb'
}

module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.0' = {
  name: 'user-assigned-identity'
  params: {
    name: 'managed-identity-${resourceToken}'
    location: location
    tags: tags
  }
}

module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.8.0' = {
  name: 'cosmos-db-account'
  params: {
    name: 'cosmos-db-nosql-${resourceToken}'
    location: location
    locations: [
      {
        failoverPriority: 0
        locationName: location
        isZoneRedundant: false
      }
    ]
    tags: tags
    disableKeyBasedMetadataWriteAccess: true
    disableLocalAuth: true
    networkRestrictions: {
      publicNetworkAccess: 'Enabled'
      ipRules: []
      virtualNetworkRules: []
    }
    capabilitiesToAdd: [
      'EnableServerless'
    ]
    sqlRoleDefinitions: [
      {
        name: 'nosql-data-plane-contributor'
        dataAction: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
        ]
      }
    ]
    sqlRoleAssignmentsPrincipalIds: union(
      [
        managedIdentity.outputs.principalId
      ],
      !empty(deploymentUserPrincipalId) ? [deploymentUserPrincipalId] : []
    )
    sqlDatabases: [
      {
        name: 'cosmicworks'
        containers: [
          {
            name: 'products'
            paths: [
              '/category'
            ]
          }
        ]
      }
    ]
  }
}

module openAiAccount 'br/public:avm/res/cognitive-services/account:0.8.1' = {
  name: 'open-ai-account'
  params: {
    name: 'open-ai-${resourceToken}'
    location: location
    kind: 'OpenAI'
    tags: tags
    sku: 'S0'
    customSubDomainName: 'open-ai-${resourceToken}'
    publicNetworkAccess: 'Enabled'
    deployments: [
      {
        name: 'chatbot'
        model: {
          format: 'OpenAI'
          name: 'gpt-4'
          version: 'turbo-2024-04-09'
        }
        sku: {
          name: 'Standard'
          capacity: 2
        }
      }
    ]
  }
}

module appServicePlan 'br/public:avm/res/web/serverfarm:0.3.0' = {
  name: 'app-service-plan'
  params: {
    name: 'app-service-plan-${resourceToken}'
    location: location
    tags: tags
    kind: 'Linux'
    skuName: 'B1'
    skuCapacity: 1
  }
}

module appServiceWebApp 'br/public:avm/res/web/site:0.9.0' = {
  name: 'app-service-web-app'
  params: {
    kind: 'app,linux'
    name: 'app-service-web-app-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    serverFarmResourceId: appServicePlan.outputs.resourceId
    publicNetworkAccess: 'Enabled'
    managedIdentities: {
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
    siteConfig: {
      appSettings: [
        {
          name: 'CONNECTION__AZURECOSMOSDB__ENDPOINT'
          value: cosmosDbAccount.outputs.endpoint
        }
        {
          name: 'CONNECTION__AZUREOPENAI__ENDPOINT'
          value: openAiAccount.outputs.endpoint
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.outputs.clientId
        }
      ]
      linuxFxVersion: 'DOTNETCORE|9.0'
    }
  }
}

var cognitiveServicesRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '64702f94-c441-49e6-a78b-ef80e0188fee'
) // Azure AI Developer built-in role

module openAiRoleAssignmentManagedIdentity 'br/public:avm/ptn/authorization/resource-role-assignment:0.1.1' = {
  name: 'open-ai-role-assignment-managed-identity'
  params: {
    principalId: managedIdentity.outputs.principalId
    resourceId: openAiAccount.outputs.resourceId
    roleDefinitionId: cognitiveServicesRole
  }
}

module openAiRoleAssignmentDeploymentUserIdentity 'br/public:avm/ptn/authorization/resource-role-assignment:0.1.1' = {
  name: 'open-ai-role-assignment-deployment-user'
  params: {
    principalId: deploymentUserPrincipalId
    resourceId: openAiAccount.outputs.resourceId
    roleDefinitionId: cognitiveServicesRole
  }
}

output CONNECTION__AZURECOSMOSDB__ENDPOINT string = cosmosDbAccount.outputs.endpoint
output CONNECTION__AZUREOPENAI__ENDPOINT = openAiAccount.outputs.endpoint
