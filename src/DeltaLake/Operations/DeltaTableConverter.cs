using System.Text.Json;
using DeltaLake.Log;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Exceptions;
using DeltaLake.Operations.Helpers;
using Parquet.Schema;
using Stowage;
using static DeltaLake.Operations.Helpers.DeltaTableHelpers;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations {
    public class DeltaTableConverter {
        public static async Task ConvertParquetToDeltaAsync(IFileStorage storage, IOPath location) {
            var log = new DeltaLog(storage, location);
            IReadOnlyCollection<LogCommit> history = await log.ReadHistoryAsync();

            if(history.Any())
                throw new TableAlreadyExistsException();

            IReadOnlyCollection<IOEntry> files = await storage.Ls(location + "/", true);

            var parquetFiles = files
                .Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet"))
                .ToList();

            if(parquetFiles.Count == 0)
                throw new ParquetFileNotFoundException();

            var actions = new List<Action>();
            var commitLines = new List<CommitLine>();

            JsonElement commitInfo = DeltaTableHelpers.CreateCommitInfo(OperationEnum.CREATE_TABLE);
            commitLines.Add(new CommitLine() { Commit = commitInfo });

            var protocolEvolution = new ProtocolEvolution {
                MinReaderVersion = 1,
                MinWriterVersion = 2
            };
            commitLines.Add(new CommitLine() { Protocol = protocolEvolution });

            ParquetProcessingResult parquetProcessingResult = await DeltaTableHelpers.ProcessParquetFiles(storage, location, parquetFiles);
            actions.AddRange(parquetProcessingResult.Actions);

            // Select the keys as arrays
            List<string[]> partitionKeysList = parquetProcessingResult.PartitionValuesList
            .Select(dict => dict.Keys.ToArray())
            .ToList();

            if(partitionKeysList.Count > 1 && partitionKeysList.Skip(1).Any(keys => !partitionKeysList[0].SequenceEqual(keys))) {
                throw new InvalidOperationException("All parquet files must have the same partitioning.");
            }

            ParquetSchema mergedSchema = DeltaTableHelpers.MergeSchemas(parquetProcessingResult.ParquetSchemas, parquetProcessingResult.PartitionValuesList);
            string schemaString = ParquetToSparkSchemaConverter.ConvertToSparkJsonSchema(mergedSchema);
            var schemaDocument = JsonDocument.Parse(schemaString);

            var metadata = new Metadata {
                Id = Guid.NewGuid().ToString(),
                SchemaString = JsonSerializer.Serialize(schemaDocument.RootElement),
                Format = new MetadataFormat(),
                PartitionColumns = parquetProcessingResult.PartitionValuesList.Any() ? parquetProcessingResult.PartitionValuesList.First().Keys.ToArray() : Array.Empty<string>(),
                Configuration = new Dictionary<string, string>()
            };
            commitLines.Add(new CommitLine() { MetaData = metadata });

            foreach(Action action in actions) {
                commitLines.Add(new CommitLine() { Add = (AddFile)action });
            }
            await log.WriteJsonAsCommitAsync(commitLines, 0);
        }
    }
}