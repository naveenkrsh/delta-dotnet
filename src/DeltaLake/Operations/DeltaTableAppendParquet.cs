using System.Text.Json;
using DeltaLake.Log;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Exceptions;
using DeltaLake.Operations.Models;
using DeltaLake.Operations.Utils;
using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations {
    public class DeltaTableAppendParquet {
        public static async Task AppendParquetAsync(IFileStorage storage,
            IOPath location,
            IOPath path,
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null ) {

            if(partitionStrategy == null)
                partitionStrategy = new HivePartitionStrategy();

            var log = new DeltaLog(storage, location);
            IReadOnlyCollection<LogCommit> history = await log.ReadHistoryAsync();
            if(!history.Any())
                throw new TableNotFoundException();

            List<IOEntry> parquetFiles = new List<IOEntry>();
            IOPath fullPath = new IOPath(location, path);
            if( !fullPath.ToString().EndsWith(".parquet")) {
                IReadOnlyCollection<IOEntry> files = files = await storage.Ls(fullPath + "/", true);
                parquetFiles = files
                    .Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet"))
                    .ToList();
            } else {
                IOEntry? parquetIOEntry = await storage.Stat(fullPath);
                parquetFiles.Add(parquetIOEntry);
            }

            if(parquetFiles.Count == 0)
                throw new ParquetFileNotFoundException();

            List<Dictionary<string, string>> partitionValuesList = ParquetUtil.ExtractPartitionValues(partitionStrategy, parquetFiles);

            ParquetUtil.ValidateAndEnsurePartitioning(location, partitionSchema, partitionValuesList);

            var commitLines = new List<CommitLine>();
            JsonElement commitInfo = DeltaTableUtil.CreateCommitInfo(OperationEnum.MANUAL_UPDATE);
            commitLines.Add(new CommitLine() { Commit = commitInfo });

            ParquetProcessingResult parquetProcessingResult = await ParquetUtil.ProcessParquetFilesAsync(storage, location, parquetFiles,partitionStrategy);

            //TODO: Check if schema is changed then add the metadata action

            commitLines.AddRange(parquetProcessingResult.GenerateCommitLinesFromActions());
            Table table = await Table.OpenAsync(storage, location);
            await log.WriteJsonAsCommitAsync(commitLines, table.CurrentVersion + 1);
        }
    }
}
