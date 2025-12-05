namespace RagDefectDetection.Core.Models
{
    public class VectorStoreOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string CollectionName { get; set; } = "bugzilla_defects";
        public string EmbeddingModel { get; set; } = "text-embedding-3-small-1";
        public float SimilarityThreshold { get; set; } = 0.2f;
    }
}
