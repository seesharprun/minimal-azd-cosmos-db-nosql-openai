namespace Microsoft.Samples.Cosmos.Basic.Web.Settings;

public record Connection
{
    public required AzureCosmosDB AzureCosmosDB { get; init; }

    public required AzureOpenAI AzureOpenAI { get; init; }
}

public record AzureCosmosDB
{
    public required string Endpoint { get; init; }

    public required string DatabaseName { get; init; }

    public required string ContainerName { get; init; }
}

public record AzureOpenAI
{
    public required string Endpoint { get; init; }

    public required string DeploymentName { get; init; }
}