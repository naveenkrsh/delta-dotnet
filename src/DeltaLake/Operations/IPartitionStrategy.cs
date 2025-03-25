using System.Text.RegularExpressions;

namespace DeltaLake.Operations {
    public interface IPartitionStrategy {

        Dictionary<string, string> ExtractPartitionKeyValues(string path);
    }

    public class HivePartitionStrategy : IPartitionStrategy {


        public Dictionary<string, string> ExtractPartitionKeyValues(string path) {
            var partitionKeyValues = new Dictionary<string, string>();
            var regex = new Regex(@"([^/]+)=([^/]+)");
            MatchCollection matches = regex.Matches(path);

            foreach(Match match in matches)
                if(match.Groups.Count > 2)
                    partitionKeyValues.Add(match.Groups[1].Value, match.Groups[2].Value);

            return partitionKeyValues;
        }
    }
}