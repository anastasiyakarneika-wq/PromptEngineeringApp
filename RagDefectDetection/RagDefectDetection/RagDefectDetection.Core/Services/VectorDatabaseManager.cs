using Azure;
using Microsoft.SemanticKernel.Embeddings;
using RagDefectDetection.Core.Models;
using System.Text.Json;
using Weaviate.Client;
using Weaviate.Client.Models;
using Weaviate.Client.Rest;

namespace RagDefectDetection.Core.Services
{
    public class VectorDatabaseManager
    {
        private readonly WeaviateClient _weaviateClient;
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly string _collectionName;

        public VectorDatabaseManager(
            WeaviateClient weaviateClient,
            ITextEmbeddingGenerationService embeddingService,
            string collectionName)
        {
            _weaviateClient = weaviateClient;
            _embeddingService = embeddingService;
            _collectionName = collectionName;
        }

        /// <summary>
        /// Ensures the Weaviate schema exists. If not, it creates it.
        /// </summary>
        public async Task InitializeSchemaAsync()
        {
            // Check if collection exists
            var exists = await _weaviateClient.Collections.Exists(_collectionName);

            if (exists)
            {
                Console.WriteLine($"Collection '{_collectionName}' already exists.");
                return;
            }

            Console.WriteLine($"Creating collection '{_collectionName}'...");

            // Create collection with explicit properties
            // We do NOT set a Vectorizer because we are providing vectors manually from Semantic Kernel
            await _weaviateClient.Collections.Create(new Collection
            {
                Name = _collectionName,
                Description = "Mozilla Bugzilla defect data",
                VectorConfig = new VectorConfig("default", new Vectorizer.Text2VecWeaviate()),
                Properties = 
                [
                    Property.Text("defectId"),
                    Property.Text("summary"),
                    Property.Text("description"),
                    Property.Text("fullText"),
                    Property.Text("severity"),
                    Property.Text("status")     ,
                    Property.Text("component"),
                    Property.Text("priority")
                ],
            });

            Console.WriteLine("Collection created successfully.");
        }

        /// <summary>
        /// Reads JSON files from a directory and inserts them into the database.
        /// </summary>
        public async Task InsertIntoDatabaseAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            var files = Directory.GetFiles(directoryPath, "*.json");
            Console.WriteLine($"Found {files.Length} files to process.");

            // Get the collection reference
            var collection = _weaviateClient.Collections.Use(_collectionName) ??
                throw new Exception($"{_collectionName} is not found");
            foreach (var file in files)
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(file);
                    var bug = JsonSerializer.Deserialize<Bug>(jsonContent);

                    if (bug != null)
                    {
                        await InsertDefectAsync(bug, collection);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Inserts a single defect into Weaviate.
        /// </summary>
        public async Task InsertDefectAsync(Bug bug, CollectionClient<dynamic>? collection = null)
        {
            // Use provided collection ref or get a new one
            var col = collection ?? _weaviateClient.Collections.Use(_collectionName);

            // Prepare text for embedding (Summary + Description)
            var textToEmbed = $"{bug.Summary}. {bug.Description}";

            // Generate Embedding using Semantic Kernel
            // Weaviate expects float[], Semantic Kernel returns ReadOnlyMemory<float>
            var embeddingMemory = await _embeddingService.GenerateEmbeddingAsync(textToEmbed);
            // Convert to standard float array for Weaviate
            float[] embeddingArray = embeddingMemory.ToArray();
            
            // Prepare properties object
            var properties = new Dictionary<string, object>
            {
                ["defectId"] = bug.Id,
                ["summary"] = bug.Summary ?? "",
                ["description"] = bug.Description ?? "",
                ["fullText"] = textToEmbed,
                ["severity"] = bug.Severity ?? "unknown",
                ["status"] = bug.Status ?? "unknown",
                ["component"] = bug.Component ?? "unknown",
                ["priority"] = bug.Priority ?? "unknown"
            };

            var data = new
            {
                Properties = properties,
                Vectors = new Dictionary<string, float[]> { { "default", embeddingArray } }
            };
            // Insert into Weaviate with the explicit vector
            // Note: We don't need to specify ID (UUID) unless we want deterministic IDs. 
            // Weaviate generates one automatically.
            await col.Data.Insert(data);

            Console.WriteLine($"Inserted defect ID: {bug.Id}");
        }

        /// <summary>
        /// Retrieves a defect by its Bug ID (using Filter).
        /// </summary>
        public async Task<string?> GetDefectAsync(string defectId)
        {
            var collection = _weaviateClient.Collections.Use(_collectionName);

            var response = await collection.Aggregate.NearText(
                [defectId],
                limit: 1,
                metrics: Metrics.ForProperty("points").Number(sum: true)
            );


            if (response.TotalCount > 0)
            {
                var obj = response.Properties.First();
                // Convert properties back to string for display
                return JsonSerializer.Serialize(obj);
            }

            return null;
        }

        /// <summary>
        /// Updates a defect. 
        /// </summary>
        public async Task UpdateDefectAsync(int defectId, Bug updatedBug)
        {
            // Since we don't store the UUID map, the easiest way is Delete + Insert
            await DeleteDefectAsync(defectId.ToString());
            updatedBug.Id = defectId;
            var bugToInsert = updatedBug;
            await InsertDefectAsync(bugToInsert);
        }

        /// <summary>
        /// Deletes a defect by its Bug ID.
        /// </summary>
        public async Task DeleteDefectAsync(string defectId)
        {
            var collection = _weaviateClient.Collections.Use(_collectionName);

            // Batch delete allows deleting by filter (Property: defectId)
            var response = await collection.Data.DeleteMany( where:
                Filter.Property("defectId").Like(defectId)
            );

            if (response.Matches > 0)
            {
                Console.WriteLine($"Deleted defect {defectId} ({response.Matches} objects removed).");
            }
            else
            {
                Console.WriteLine($"Defect {defectId} not found.");
            }
        }

        /// <summary>
        /// Deletes the entire collection.
        /// </summary>
        public async Task DeleteCollectionAsync()
        {
            await _weaviateClient.Collections.Delete(_collectionName);
            Console.WriteLine($"Collection '{_collectionName}' deleted.");
        }
    }
}