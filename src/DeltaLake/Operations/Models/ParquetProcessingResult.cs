using DeltaLake.Log.Actions;
using Parquet.Schema;

namespace DeltaLake.Operations.Models {
    internal class ParquetProcessingResult {
        public List<ParquetSchema> ParquetSchemas { get; set; }
        public List<Dictionary<string, string>> PartitionValuesList { get; set; }
        public List<AddFile> Actions { get; set; }

        public ParquetProcessingResult(List<ParquetSchema> parquetSchemas, List<Dictionary<string, string>> partitionValuesList, List<AddFile> actions) {
            ParquetSchemas = parquetSchemas;
            PartitionValuesList = partitionValuesList;
            Actions = actions;
        }

        public List<CommitLine> GenerateCommitLinesFromActions() {
            var commitLines = new List<CommitLine>();
            foreach(AddFile action in Actions)                 commitLines.Add(new CommitLine() { Add = action });
            return commitLines;
        }
    }
}