using System.Text.Json;
using DeltaLake.Log;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Exceptions;
using DeltaLake.Operations.Models;
using DeltaLake.Operations.Utils;
using Parquet.Schema;
using Stowage;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations {
    public class DeltaTableConverter {
        /// <summary>
        /// Converts a Parquet table to a Delta table.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="TableAlreadyExistsException"></exception>
        /// <exception cref="ParquetFileNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task ConvertParquetToDeltaAsync(
    IFileStorage storage,
    IOPath location,
    ParquetSchema? partitionSchema = null,
    IPartitionStrategy? partitionStrategy = null

    ) {
            if(partitionStrategy == null)
                partitionStrategy = new HivePartitionStrategy();

            var log = new DeltaLog(storage, location);
            IReadOnlyCollection<LogCommit> history = await log.ReadHistoryAsync();
            if(history.Any()) {
                throw new TableAlreadyExistsException();
            }

            IReadOnlyCollection<IOEntry> files = await storage.Ls(location + "/", true);

            var parquetFiles = files
                .Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet"))
                .ToList();

            if(parquetFiles.Count == 0)
                throw new ParquetFileNotFoundException();


            List<Dictionary<string, string>> partitionValuesList = ParquetUtil.ExtractPartitionValues(partitionStrategy, parquetFiles);

            ParquetUtil.ValidateAndEnsurePartitioning(location, partitionSchema, partitionValuesList);

            var actions = new List<Action>();

            JsonElement commitInfo = DeltaTableUtil.CreateCommitInfo(OperationEnum.CREATE_TABLE);
            actions.Add(new CommitInfo(commitInfo));

            var protocolEvolution = new ProtocolEvolution {
                MinReaderVersion = 1,
                MinWriterVersion = 2
            };
            actions.Add(protocolEvolution);

            ParquetProcessingResult parquetProcessingResult = await ParquetUtil.ProcessParquetFilesAsync(storage, location, parquetFiles, partitionStrategy);

            ParquetSchema mergedSchema = ParquetUtil.MergeSchemas(parquetProcessingResult.ParquetSchemas, partitionSchema);
            string schemaString = ParquetToSparkSchemaConverter.ConvertToSparkJsonSchema(mergedSchema);
            var schemaDocument = JsonDocument.Parse(schemaString);

            var metadata = new Metadata {
                Id = Guid.NewGuid().ToString(),
                SchemaString = JsonSerializer.Serialize(schemaDocument.RootElement),
                Format = new MetadataFormat(),
                PartitionColumns = partitionValuesList.Any() ? partitionValuesList.First().Keys.ToArray() : Array.Empty<string>(),
                Configuration = new Dictionary<string, string>()
            };
            actions.Add(metadata);
            actions.AddRange(parquetProcessingResult.Actions);
            await log.WriteJsonAsCommitAsync(actions, 0);
        }
    }

 
}