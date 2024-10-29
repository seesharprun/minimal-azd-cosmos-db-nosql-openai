using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.Samples.Cosmos.Basic.Web.Models;
using Microsoft.Samples.Cosmos.Basic.Web.Settings;
using OpenAI.Chat;

namespace Microsoft.Samples.Cosmos.Basic.Web.Components.Pages;

public partial class Home
{
    private readonly Connection connection;

    public Home(IOptions<Connection> connectionOptions)
    {
        this.connection = connectionOptions.Value;
    }

    public List<(string content, bool highlight)> ConsoleOutput { get; private set; } = new();

    public bool Loading { get; private set; } = false;

    private async Task RunDemoAsync()
    {
        await SetLoadingAsync(true);
        await ClearConsoleAsync();

        await WriteConsoleAsync("Welcome to the Azure Cosmos DB for NoSQL demo!", highlight: true);

        TokenCredential credential = new DefaultAzureCredential();

        string azureCosmosDBEndpoint = connection.AzureCosmosDB?.Endpoint ?? throw new ArgumentNullException("Azure Cosmos DB for NoSQL endpoint is not configured. Please configure the \"Connection:AzureCosmosDB:Endpoint\" configuration setting.");
        string databaseName = connection.AzureCosmosDB?.DatabaseName ?? throw new ArgumentNullException("Azure Cosmos DB for NoSQL database name is not configured. Please configure the \"Connection:AzureCosmosDB:DatabaseName\" configuration setting.");
        string containerName = connection.AzureCosmosDB?.ContainerName ?? throw new ArgumentNullException("Azure Cosmos DB for NoSQL container name is not configured. Please configure the \"Connection:AzureCosmosDB:ContainerName\" configuration setting.");

        await WriteConsoleAsync("Connecting to Azure Cosmos DB for NoSQL client...");
        await WriteConsoleAsync($"Azure Cosmos DB for NoSQL Endpoint: {azureCosmosDBEndpoint}");

        CosmosClient azureCosmosDBClient = new(azureCosmosDBEndpoint, credential);
        Container container = azureCosmosDBClient.GetContainer(databaseName, containerName);

        await WriteConsoleAsync($"Associated with {containerName} container of {databaseName} database.");

        string id = "0000-0000";
        string partitionKey = "Azure Cosmos DB for NoSQL";
        Item item = new(
            id,
            category: partitionKey,
            name: "Distributed NoSQL database service"
        );

        await WriteConsoleAsync($"Creating item: {item}");

        Response<Item> writeResponse = await container.UpsertItemAsync(item);

        await WriteConsoleAsync("Upsert opertaion done", highlight: true);
        await WriteConsoleAsync($"Request charge of the operation: {writeResponse.RequestCharge:0.00}");
        await WriteConsoleAsync($"Activity ID of the operation: {writeResponse.ActivityId}");

        await WriteConsoleAsync($"Point reading item id \"{id}\" and partition key \"{partitionKey}\"");

        Response<Item> readResponse = await container.ReadItemAsync<Item>(id, new PartitionKey(partitionKey));

        await WriteConsoleAsync("Read operation done", highlight: true);
        await WriteConsoleAsync($"Request charge of the operation: {readResponse.RequestCharge:0.00}");
        await WriteConsoleAsync($"Activity ID of the operation: {readResponse.ActivityId}");

        string azureOpenAIEndpoint = connection.AzureOpenAI?.Endpoint ?? throw new ArgumentNullException("Azure OpenAI endpoint is not configured. Please configure the \"Connection:AzureOpenAI:Endpoint\" configuration setting.");
        string deploymentName = connection.AzureOpenAI?.DeploymentName ?? throw new ArgumentNullException("Azure OpenAI deployment name is not configured. Please configure the \"Connection:AzureOpenAI:DeploymentName\" configuration setting.");

        await WriteConsoleAsync("Connecting to Azure OpenAI client...");
        await WriteConsoleAsync($"Azure OpenAI Endpoint: {azureOpenAIEndpoint}");

        AzureOpenAIClient azureOpenAIClient = new(new Uri(azureOpenAIEndpoint), credential);

        ChatClient chat = azureOpenAIClient.GetChatClient(deploymentName);

        await WriteConsoleAsync($"Associated with {deploymentName} deployment.");

        string prompt = "Translate \"Hello, how are you?\" to German and Spanish.";
        await WriteConsoleAsync($"Prompt sent: {prompt}");

        ClientResult<ChatCompletion> result = await chat.CompleteChatAsync(prompt);
        ChatCompletion completion = result.Value;

        await WriteConsoleAsync("Chat completion done", highlight: true);
        await WriteConsoleAsync($"Chat completion ID: {completion.Id}");
        await WriteConsoleAsync($"Chat completion content count: {completion.Content.Count}");
        await WriteConsoleAsync($"Input token usage: {completion.Usage.InputTokenCount:000}");
        await WriteConsoleAsync($"Output token usage: {completion.Usage.OutputTokenCount:000}");

        string response = string.Join(Environment.NewLine, completion.Content.Select(c => c.Text)).Trim();

        await WriteConsoleAsync($"Chat completion response: {response}");

        await WriteConsoleAsync("Demo finished!", highlight: true);
        await SetLoadingAsync(false);
    }

    private async Task SetLoadingAsync(bool loading)
    {
        Loading = loading;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearConsoleAsync()
    {
        ConsoleOutput.Clear();
        await InvokeAsync(StateHasChanged);
    }

    private async Task WriteConsoleAsync(string message, bool highlight = false)
    {
        ConsoleOutput.Add(($"{message}", highlight));
        await InvokeAsync(StateHasChanged);
    }
}