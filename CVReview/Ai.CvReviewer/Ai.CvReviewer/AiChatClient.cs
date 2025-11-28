using DotNetEnv;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

public class AiChatClient
{
    private readonly Kernel _kernel;
    public string ChatModel { get; }

    public AiChatClient()
    {
        Env.Load();
        var azureBase = Environment.GetEnvironmentVariable("AZURE_API_BASE_URl");
        var isAzure = !string.IsNullOrWhiteSpace(azureBase);
        var apiVersion = Environment.GetEnvironmentVariable("API_VERSION") ?? "2024-10-21";
        var apiKey = isAzure
            ? Environment.GetEnvironmentVariable("AZURE_API_KEY") ?? string.Empty
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        ChatModel = Environment.GetEnvironmentVariable("MODEL") ?? "gpt-4o";

        var builder = Kernel.CreateBuilder();

        if (isAzure)
        {
            // Azure OpenAI
            var endpoint = azureBase!.TrimEnd('/');
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: ChatModel,
                endpoint: endpoint,
                apiKey: apiKey,
                apiVersion: apiVersion,
                serviceId: "chat"
            );
        }
        else
        {
            // OpenAI
            var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")?.TrimEnd('/');
            Uri uri = new(endpoint);
            builder.AddOpenAIChatCompletion(
                modelId: ChatModel,
                apiKey: apiKey,
                endpoint: uri,
                serviceId: "chat"
            );
        }

        _kernel = builder.Build();
    }

    public async Task<string> ChatAsync(string system, string user, CancellationToken ct = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);
        var historyInString = JsonSerializer.Serialize(history);
        var chat = _kernel.GetRequiredService<IChatCompletionService>("chat");
        var result = await chat.GetChatMessageContentAsync(historyInString);
        return result?.Content ?? string.Empty;
    }
}