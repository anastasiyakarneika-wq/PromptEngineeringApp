using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace Ai.CvReviewer.Services;

public interface IEmbeddingProvider
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    public float CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
            throw new ArgumentException("Embeddings must have the same dimension");

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            magnitude1 += embedding1[i] * embedding1[i];
            magnitude2 += embedding2[i] * embedding2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }
}

public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public OpenAiEmbeddingProvider(string apiKey, string model = "text-embedding-3-small")
    {
        var kernelBuilder = Kernel.CreateBuilder();
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_URL");
        kernelBuilder.AddOpenAITextEmbeddingGeneration(
            modelId: model,
            apiKey: Environment.GetEnvironmentVariable("AZURE_API_KEY") ?? string.Empty
        );

        _kernel = kernelBuilder.Build();
        _embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    { 
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync([text]);
        return embeddings[0].ToArray();
    }
}

public class AzuredEmbeddingProvider : IEmbeddingProvider
{ 
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public string EmbeddingModel { get; }

    public AzuredEmbeddingProvider(string baseURl)
    {
        EmbeddingModel = Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "text-embedding-3-small-1";

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
            deploymentName: EmbeddingModel,
            endpoint: baseURl!,
            apiKey: Environment.GetEnvironmentVariable("AZURE_API_KEY") ?? string.Empty
        );

        _kernel = kernelBuilder.Build();
        _embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync([text]);
            return embeddings[0].ToArray();
        }
        catch (Exception ex)
        {
            throw new Exception($"Embedding generation failed." +
                $" Model: {EmbeddingModel}, IsAzure: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_API_BASE_URL"))}", ex);
        }
    }
}