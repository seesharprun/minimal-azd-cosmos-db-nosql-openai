using './main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azd-cosmos-db-openai-demo')
param location = readEnvironmentVariable('AZURE_LOCATION', 'westus3')
param deploymentUserPrincipalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')
