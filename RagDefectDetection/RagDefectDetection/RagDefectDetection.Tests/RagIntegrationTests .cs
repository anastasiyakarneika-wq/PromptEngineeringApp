using DotNetEnv;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using RagDefectDetection.Core.Models;
using RagDefectDetection.Core.Services;
using System.Text.Json;
using Weaviate.Client;
using Xunit;

namespace RagDefectDetection.Tests
{
    public class RagIntegrationTests : IAsyncLifetime
    {
        private VectorDatabaseManager _dbManager;
        private SimilarDefectsRetriever _retriever;
        private DuplicatesDetector _detector;
        private string _bugsDir;
        private string _duplicatesDir;
        private string _newBugsDir;

        // Configuration
        private const string TestCollectionName = "BugzillaDefect_Test"; // Separate collection for testing

        public async Task InitializeAsync()
        {
            Env.Load();

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                         ?? throw new Exception("OPENAI_API_KEY not found");
            var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")
                           ?? throw new Exception("OPENAI_ENDPOINT not found");
            var llmModel = Environment.GetEnvironmentVariable("LLM_DEPLOYMENT_NAME") ?? "gpt-4o";
            var embedModel = Environment.GetEnvironmentVariable("EMBEDDING_DEPLOYMENT_NAME") ?? "text-embedding-3-small-1";
            var weaviateUrl = Environment.GetEnvironmentVariable("WEAVIATE_URL") ?? "http://localhost:8080";

            _bugsDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data/bugs"));
            _duplicatesDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data/duplicates"));
            _newBugsDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data/new_bugs"));

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAITextEmbeddingGeneration(embedModel, endpoint, apiKey);
            builder.AddAzureOpenAIChatCompletion(llmModel, endpoint, apiKey);

            var kernel = builder.Build();
            var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

            // 4. Setup Weaviate
            var weaviateClient = new WeaviateClient(new ClientConfiguration());

            // 5. Initialize Components
            _dbManager = new VectorDatabaseManager(weaviateClient, embeddingService, TestCollectionName);
            _retriever = new SimilarDefectsRetriever(weaviateClient, embeddingService, TestCollectionName, 0.6); // 0.6 threshold
            _detector = new DuplicatesDetector(chatService, _retriever, llmModel);

            // 6. Prepare Database
            await _dbManager.InitializeSchemaAsync();

            // Ingest data if empty (Simple check)
            // Note: In a real CI/CD, you might want to clean and reload every time, 
            // but for local dev, we check if we need to load to save time.
            // For this test, we force load if we suspect it's empty or to ensure consistency.
            Console.WriteLine("Ensuring test data is loaded...");
            await _dbManager.InsertIntoDatabaseAsync(_bugsDir);
        }

        public Task DisposeAsync()
        {
            // Optional: Cleanup after tests
            // await _dbManager.DeleteCollectionAsync();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task T1_CRUD_Operations_ShouldWork()
        {
            var testId = 999999;
            var bug = new Bug
            {
                Id = testId,
                Summary = "Test CRUD Summary",
                Description = "Test CRUD Description",
                Severity = "critical",
                Status = "NEW",
                Component = "TestComponent",
                Priority = "P1"
            };

            // 1. Insert
            await _dbManager.InsertDefectAsync(bug);

            // 2. Get
            var retrievedJson = await _dbManager.GetDefectAsync(testId.ToString());
            Assert.NotNull(retrievedJson);
            Assert.Contains("Test CRUD Summary", retrievedJson);

            // 3. Delete
            await _dbManager.DeleteDefectAsync(testId.ToString());

            // 4. Verify Delete
            // Weaviate eventual consistency might require a small delay, but usually fast enough locally
            await Task.Delay(1000);
            var deletedJson = await _dbManager.GetDefectAsync(testId.ToString());
            Assert.Null(deletedJson);
        }

        [Fact]
        public async Task T2_Retriever_ShouldFindSimilarDefects()
        {
            // Pick a known bug from the dataset (e.g., one about "browser crash")
            // We construct a query that is semantically similar but not identical.
            var query = "The web browser terminates unexpectedly when rendering heavy pages.";

            var context = await _retriever.RetrieveSimilarDefectsContextAsync(query);

            Assert.NotNull(context);
            Assert.NotEmpty(context);
            Assert.Contains("Similar defects found", context);
            // It should find something related to crashes if the dataset is loaded
        }

        [Fact]
        public async Task T3_Detector_ShouldIdentify_Duplicate()
        {
            // Load a specific duplicate file
            var dupFile = Directory.GetFiles(_duplicatesDir)[2];
            if (dupFile == null) Assert.Fail("No duplicate test data found");

            var json = await File.ReadAllTextAsync(dupFile);
            var bug = JsonSerializer.Deserialize<Bug>(json);

            // Act
            var report = await _detector.DetectDuplicateAsync(bug.Summary!, bug.Description!);

            // Assert
            Assert.True(report.IsDuplicate, $"Should be a duplicate. Reason: {report.Reason}");
            Assert.True(report.Confidence > 0.7, $"Confidence {report.Confidence} is too low");
            Assert.NotEmpty(report.Defects);

            // Save report for inspection
            await _detector.SaveReportAsync(report, "test_duplicate_report.json");
        }

        [Fact]
        public async Task T4_Detector_ShouldIdentify_NewBug()
        {
            // Load a specific new bug file
            var newBugFile = Directory.GetFiles(_newBugsDir).FirstOrDefault();
            if (newBugFile == null) Assert.Fail("No new bug test data found");

            var json = await File.ReadAllTextAsync(newBugFile);
            var bug = JsonSerializer.Deserialize<Bug>(json);

            // Act
            var report = await _detector.DetectDuplicateAsync(bug.Summary!, bug.Description!);

            // Assert
            Assert.False(report.IsDuplicate, $"Should NOT be a duplicate. Reason: {report.Reason}");

            // Save report for inspection
            await _detector.SaveReportAsync(report, "test_newbug_report.json");
        }
    }
}