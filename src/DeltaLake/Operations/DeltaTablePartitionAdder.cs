using System.Text.Json;
using DeltaLake.Log;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Exceptions;
using DeltaLake.Operations.Helpers;
using Stowage;
using static DeltaLake.Operations.Helpers.DeltaTableHelpers;

namespace DeltaLake.Operations {
    class DeltaTablePartitionAdder {
        public static async Task AddPartitionAsync(IFileStorage storage, IOPath location, IOPath partition
            ) {
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

            var actions = new List<System.Action>();
            var commitLines = new List<CommitLine>();
            JsonElement commitInfo = DeltaTableHelpers.CreateCommitInfo(OperationEnum.MANUAL_UPDATE);
            commitLines.Add(new CommitLine() { Commit = commitInfo });
            var protocolEvolution = new ProtocolEvolution {
                MinReaderVersion = 1,
                MinWriterVersion = 2
            };

            commitLines.Add(new CommitLine() { Protocol = protocolEvolution });

            ParquetProcessingResult parquetProcessingResult = await DeltaTableHelpers.ProcessParquetFiles(storage, location, parquetFiles);

        }
    }
}
