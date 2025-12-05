namespace RagDefectDetection.Core.Models
{
    public class SimilarDefect
    {
        public string Id { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
