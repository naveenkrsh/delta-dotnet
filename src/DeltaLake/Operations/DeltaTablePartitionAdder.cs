using System.Text.Json;
using DeltaLake.Log;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Exceptions;
using DeltaLake.Operations.Models;
using DeltaLake.Operations.Utils;
using Stowage;

namespace DeltaLake.Operations {
    public class DeltaTablePartitionAdder {
        public static async Task AddPartitionAsync(IFileStorage storage, IOPath location, IOPath partition) {
            var log = new DeltaLog(storage, location);
            IReadOnlyCollection<LogCommit> history = await log.ReadHistoryAsync();
            if(!history.Any())
                throw new TableNotFoundException();

            var partitionPath = new IOPath(location, partition);

            IReadOnlyCollection<IOEntry> files = await storage.Ls(partitionPath + "/", true);
            var parquetFiles = files
                .Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet"))
                .ToList();
            if(parquetFiles.Count == 0)
                throw new ParquetFileNotFoundException();

            var commitLines = new List<CommitLine>();
            JsonElement commitInfo = DeltaTableUtil.CreateCommitInfo(OperationEnum.MANUAL_UPDATE);
            commitLines.Add(new CommitLine() { Commit = commitInfo });

            ParquetProcessingResult parquetProcessingResult = await DeltaTableUtil.ProcessParquetFilesAsync(storage, location, parquetFiles);

            //TODO: Check if schema is changed then add the metadata action

            commitLines.AddRange(parquetProcessingResult.GenerateCommitLinesFromActions());
            Table table = await Table.OpenAsync(storage, location);
            await log.WriteJsonAsCommitAsync(commitLines, table.CurrentVersion + 1);
        }
    }
}
