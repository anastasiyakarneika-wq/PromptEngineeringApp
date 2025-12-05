namespace RagDefectDetection.Core.Models
{
    public class DefectQueryResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public string Text { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
