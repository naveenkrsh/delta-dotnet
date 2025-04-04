using System.Text.RegularExpressions;

namespace DeltaLake.Operations {
    /// <summary>
    /// Interface for partition strategy.
    /// This interface is used to extract partition key values from a given path.
    /// </summary>
    public interface IPartitionStrategy {
        Dictionary<string, string> ExtractPartitionKeyValues(string path);
    }

    /// <summary>
    /// Implementation of IPartitionStrategy for Hive-style partitioning.
    /// This implementation uses a regular expression to extract partition key values from the path.
    /// </summary>
    public class HivePartitionStrategy : IPartitionStrategy {
        public Dictionary<string, string> ExtractPartitionKeyValues(string path) {
            var partitionKeyValues = new Dictionary<string, string>();
            var regex = new Regex(@"([^/]+)=([^/]+)");
            MatchCollection matches = regex.Matches(path);

            foreach(Match match in matches) {
                if(match.Groups.Count > 2) {
                    partitionKeyValues.Add(match.Groups[1].Value, match.Groups[2].Value);
                }
            }
            return partitionKeyValues;
        }
    }
}