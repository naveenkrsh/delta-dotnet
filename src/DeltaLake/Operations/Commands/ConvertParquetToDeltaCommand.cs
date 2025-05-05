using System.Text.Json;
using DeltaLake.Log;
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
    /// Command to convert a Parquet table to a Delta table.
    /// </summary>
    public class ConvertParquetToDeltaCommand : DeltaTableOperationBase {
        private readonly ParquetSchema? _partitionSchema;
        private readonly IPartitionStrategy _partitionStrategy;

        public ConvertParquetToDeltaCommand(
            IDeltaStorage storage,
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null)
            : base(storage) {
            _partitionSchema = partitionSchema;
            _partitionStrategy = partitionStrategy ?? new HivePartitionStrategy();
        }

        protected override async Task ValidateCoreAsync() {
            await DeltaOperationValidator.ValidateTableDoesNotExist(Storage);
            await DeltaOperationValidator.ValidateParquetFilesExist(Storage, Storage.Location + "/");
        }

        protected override async Task ExecuteCoreAsync() {
            IReadOnlyCollection<IOEntry> files = await Storage.CachedStorage.Ls(Storage.Location + "/", true);
            List<IOEntry> parquetFiles = files.Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet")).ToList();

            List<Dictionary<string, string>> partitionValuesList = ParquetUtil.ExtractPartitionValues(_partitionStrategy, parquetFiles);
            ParquetUtil.ValidateAndEnsurePartitioning(Storage.Location, _partitionSchema, partitionValuesList);

            List<Action> actions = new List<Action>
            {
                new CommitInfo(DeltaTableUtil.CreateCommitInfo(OperationEnum.CREATE_TABLE))
            };

            var protocolEvolution = new ProtocolEvolution {
                MinReaderVersion = 1,
                MinWriterVersion = 2
            };
            actions.Add(protocolEvolution);

            // Process files in parallel and get schema
            ParquetProcessingResult parquetProcessingResult = await ParallelParquetProcessor.ProcessFilesParallelAsync(
                Storage,
                parquetFiles,
                partitionValuesList
            );

            ParquetSchema schema = parquetProcessingResult.ParquetSchemas[0];
            await ParallelParquetProcessor.ValidateSchemasParallelAsync(parquetProcessingResult.ParquetSchemas);

            ParquetSchema mergedSchema = ParquetUtil.MergeSchemas(parquetProcessingResult.ParquetSchemas, _partitionSchema);
            string schemaString = ParquetToSparkSchemaConverter.ConvertToSparkJsonSchema(mergedSchema);
            var schemaDocument = JsonDocument.Parse(schemaString);

            // Add metadata action
            string[]? partitionColumns = _partitionSchema?.Fields.Select(f => f.Name).ToArray();

            var metadata = new Metadata {
                Id = Guid.NewGuid().ToString(),
                SchemaString = JsonSerializer.Serialize(schemaDocument.RootElement),
                Format = new MetadataFormat(),
                PartitionColumns = partitionColumns ?? Array.Empty<string>(),
                Configuration = new Dictionary<string, string>()
            };

            actions.Add(metadata);

            // Add the AddFile actions
            actions.AddRange(parquetProcessingResult.Actions);
            DeltaLog log = new DeltaLog(Storage.UnderlyingStorage, Storage.Location);
            await log.WriteJsonAsCommitAsync(actions, 0);
        }
    }
}