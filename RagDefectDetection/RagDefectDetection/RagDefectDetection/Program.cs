using DotNetEnv;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using RagDefectDetection.Core.Services;
using Weaviate.Client;

// 1. Load Environment Variables
Env.Load();

string weaviateUrl = Environment.GetEnvironmentVariable("WEAVIATE_URL") ?? "http://localhost:8080";
string collectionName = Environment.GetEnvironmentVariable("COLLECTION_NAME") ?? "BugzillaDefect";
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
string endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")!;
string embeddingDeployment = Environment.GetEnvironmentVariable("EMBEDDING_MODEL")!;

// 2. Setup Semantic Kernel Builder
var builder = Kernel.CreateBuilder();

// Add Azure OpenAI Embeddings (Works for EPAM DIAL)
builder.AddAzureOpenAITextEmbeddingGeneration(
    deploymentName: embeddingDeployment,
    endpoint: endpoint,
    apiKey: apiKey
);

var kernel = builder.Build();
var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

var weaviateClient = new WeaviateClient(new ClientConfiguration());

// 4. Instantiate Manager
var dbManager = new VectorDatabaseManager(weaviateClient, embeddingService, collectionName);

Console.WriteLine("VectorDatabaseManager initialized successfully.");
Console.WriteLine("Ready to implement retrieval...");