using Microsoft.SemanticKernel.Embeddings;
using RagDefectDetection.Core.Models;
using System.Text;
using Weaviate.Client;
using Weaviate.Client.Models;
using Weaviate.Client.Rest.Dto;

namespace RagDefectDetection
{
    public class SimilarDefectsRetriever
    {
        private readonly WeaviateClient _weaviateClient;
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly string _collectionName;
        private readonly double _similarityThreshold;

        public SimilarDefectsRetriever(
            WeaviateClient weaviateClient,
            ITextEmbeddingGenerationService embeddingService,
            string collectionName,
            double similarityThreshold)
        {
            _weaviateClient = weaviateClient;
            _embeddingService = embeddingService;
            _collectionName = collectionName;
            _similarityThreshold = similarityThreshold;
        }

        /// <summary>
        /// Retrieves similar defects and formats them into a context string for the LLM.
        /// </summary>
        public async Task<string> RetrieveSimilarDefectsContextAsync(string query)
        {
            var similarDefects = await RetrieveSimilarDefectsAsync(query);
            return FormatContext(similarDefects);
        }

        /// <summary>
        /// Performs the vector search.
        /// </summary>
        private async Task<List<SimilarDefect>> RetrieveSimilarDefectsAsync(string query)
        {
            // 1. Generate embedding for the query
            var queryEmbeddingMemory = await _embeddingService.GenerateEmbeddingAsync(query);
            float[] queryVector = queryEmbeddingMemory.ToArray();

            // 2. Query Weaviate
            var collection = _weaviateClient.Collections.Use(_collectionName);

            // We use 'NearVector' to search by our externally generated embedding
            var results = await collection.Query.NearVector(
                vector: queryVector,
                distance: (float)_similarityThreshold, // Filter by distance/certainty
                limit: 5
            );

            var defects = new List<SimilarDefect>();

            foreach (var obj in results.Objects)
            {
                // Weaviate returns 'Distance' (0 = identical, 1 = opposite). 
                // Similarity usually = 1 - Distance.
                // Depending on the metric (Cosine), this might vary. 
                // Let's assume Cosine distance where smaller is better.
                double distance = obj.Metadata?.Distance ?? 1.0;
                double similarity = 1.0 - distance;

                // Extract properties safely
                obj.Properties.TryGetValue("defectId", out var idObj);
                obj.Properties.TryGetValue("summary", out var summaryObj);
                obj.Properties.TryGetValue("description", out var descObj);

                defects.Add(new SimilarDefect
                {
                    Id = idObj?.ToString() ?? "unknown",
                    Summary = summaryObj?.ToString() ?? "",
                    Description = descObj?.ToString() ?? "",
                    Score = (float)similarity                   
                });
            }

            return defects;
        }

        private string FormatContext(List<SimilarDefect> defects)
        {
            if (defects.Count == 0)
                return "No similar defects found.";

            var sb = new StringBuilder();
            sb.AppendLine("Similar defects found in database:\n");

            for (int i = 0; i < defects.Count; i++)
            {
                var defect = defects[i];
                sb.AppendLine($"Defect #{i + 1}:");
                sb.AppendLine($"ID: {defect.Id}");
                sb.AppendLine($"Score: {defect.Score:F4}");
                sb.AppendLine($"Summary: {defect.Summary}");
                sb.AppendLine($"Description: {defect.Description}");
                sb.AppendLine("---");
            }

            return sb.ToString();
        }
    }
}