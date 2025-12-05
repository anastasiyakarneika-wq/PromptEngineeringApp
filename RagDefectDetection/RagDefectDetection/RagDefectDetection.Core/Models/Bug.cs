using System.Text.Json.Serialization;


namespace RagDefectDetection.Core.Models
{
    public class Bug
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = string.Empty;

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        [JsonPropertyName("component")]
        public string Component { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("comments")]
        public List<string> Comments { get; set; } = new();
    }
}
