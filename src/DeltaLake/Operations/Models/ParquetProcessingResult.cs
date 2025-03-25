using DeltaLake.Log.Actions;
using Parquet.Schema;

namespace DeltaLake.Operations.Models {
    internal class ParquetProcessingResult {
        public List<ParquetSchema> ParquetSchemas { get; set; }
        public List<AddFile> Actions { get; set; }

        public ParquetProcessingResult(List<ParquetSchema> parquetSchemas, List<AddFile> actions) {
            ParquetSchemas = parquetSchemas;
            Actions = actions;
        }

        public List<CommitLine> GenerateCommitLinesFromActions() {
            var commitLines = new List<CommitLine>();
            foreach(AddFile action in Actions) {
                commitLines.Add(new CommitLine() { Add = action });
            }
            return commitLines;
        }
    }
}