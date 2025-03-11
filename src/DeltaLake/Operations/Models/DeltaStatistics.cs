using System.Text.Json.Serialization;

namespace DeltaLake.Operations.Models {
    internal class DeltaStatistics {
        [JsonPropertyName("numRecords")]
        public long NumRecords { get; set; } = 0;

        [JsonPropertyName("minValues")]
        public Dictionary<string, object?> MinValues { get; set; } = new Dictionary<string, object?>();

        [JsonPropertyName("maxValues")]
        public Dictionary<string, object?> MaxValues { get; set; } = new Dictionary<string, object?>();

        [JsonPropertyName("nullCount")]
        public Dictionary<string, long?> NullCount { get; set; } = new Dictionary<string, long?>();
    }
}