using DeltaLake.Log.Actions;
using DeltaLake.Operations.Models;
using DeltaLake.Operations.Storage;
using DeltaLake.Operations.Utils;
using DeltaLake.Operations.Validation;
using Parquet.Schema;
using Stowage;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations.Commands {
    /// <summary>
    /// Command to append Parquet files to a Delta table.
    /// </summary>
    public class AppendParquetCommand : DeltaTableOperationBase {
        private readonly IOPath _sourcePath;
        private readonly ParquetSchema? _partitionSchema;
        private readonly IPartitionStrategy _partitionStrategy;
        private readonly IOPath _parquetPath;

        public AppendParquetCommand(
            IDeltaStorage storage,
            IOPath sourcePath,
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null)
            : base(storage) {
            _sourcePath = sourcePath;
            _partitionSchema = partitionSchema;
            _partitionStrategy = partitionStrategy ?? new HivePartitionStrategy();
            _parquetPath = new IOPath(Storage.Location, _sourcePath);
        }

        protected override async Task ValidateCoreAsync() {
            await DeltaOperationValidator.ValidateTableExists(Storage);
            await DeltaOperationValidator.ValidateParquetFilesExist(Storage, _parquetPath);
        }

        protected override async Task ExecuteCoreAsync() {
            Table table = await Table.OpenAsync(Storage.UnderlyingStorage, Storage.Location);

            List<IOEntry> parquetFiles = new List<IOEntry>();

            if(_parquetPath.IsFolder) {
                IReadOnlyCollection<IOEntry> files = await Storage.CachedStorage.Ls(new IOPath(_parquetPath, "/"));
                parquetFiles = files.Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet")).ToList();
            } else {
                IOEntry? parquetIOEntry = await Storage.CachedStorage.Stat(_parquetPath);
                if(parquetIOEntry != null) {
                    parquetFiles.Add(parquetIOEntry);
                }
            }

            List<Dictionary<string, string>> partitionValuesList = ParquetUtil.ExtractPartitionValues(_partitionStrategy, parquetFiles);
            ParquetUtil.ValidateAndEnsurePartitioning(_sourcePath, _partitionSchema, partitionValuesList);

            //TODO: Check if partition is changed then throw partitioned not matched exception

            List<Action> actions = new List<Action>
            {
                new CommitInfo(DeltaTableUtil.CreateCommitInfo(OperationEnum.MANUAL_UPDATE))
            };

            // Process files in parallel and get schema
            ParquetProcessingResult parquetProcessingResult = await ParallelParquetProcessor.ProcessFilesParallelAsync(
                Storage,
                parquetFiles,
                partitionValuesList
                );

            await ParallelParquetProcessor.ValidateSchemasParallelAsync(parquetProcessingResult.ParquetSchemas);

            //TODO: Check if schema is changed then add the metadata action / throw schema not matched exception

            // Add the AddFile actions
            actions.AddRange(parquetProcessingResult.Actions);
            await table.Log.WriteJsonAsCommitAsync(actions, table.CurrentVersion + 1);
        }
    }
}