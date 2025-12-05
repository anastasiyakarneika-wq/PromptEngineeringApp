namespace RagDefectDetection.Core.Models
{
    public class BugDuplicateReport
    {
        public bool IsDuplicate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<Bug> Defects { get; set; } = new();
        public float Confidence { get; set; }
    }
}
