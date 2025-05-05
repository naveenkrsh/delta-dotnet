using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Models;
using DeltaLake.Operations.Storage;
using Parquet;
using Parquet.Meta;
using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations.Utils {
    /// <summary>
    /// Provides parallel processing capabilities for Parquet operations.
    /// </summary>
    internal static class ParallelParquetProcessor {
        private static readonly int MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1);

        /// <summary>
        /// Processes Parquet files in parallel to extract stats and create AddFile actions.
        /// </summary>
        public static async Task<ParquetProcessingResult> ProcessFilesParallelAsync(
            IDeltaStorage storage,
            IEnumerable<IOEntry> parquetFiles,
            IList<Dictionary<string, string>> partitionValuesList) {
            var actions = new ConcurrentBag<AddFile>();
            var schemas = new ConcurrentBag<ParquetSchema>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };

            await Parallel.ForEachAsync(
                parquetFiles.Select((file, index) => (file, index)),
                options,
                async (item, ct) => {
                    (IOEntry file, int index) = item;

                    using(Stream? stream = await storage.UnderlyingStorage.OpenRead(file.Path)) {
                        if(stream == null)
                            throw new InvalidOperationException($"Could not open stream for path {file.Path}");
                        using(ParquetReader reader = await ParquetReader.CreateAsync(stream)) {
                            schemas.Add(reader.Schema);
                            Models.DeltaStatistics stats = ParquetUtil.CollectStatisticsAsync(reader);
                            actions.Add(CreateAddFileAction(storage,partitionValuesList, file, index, stats));
                        }
                    }
                });

            return new ParquetProcessingResult(schemas.ToList(), actions.ToList());
        }

        private static AddFile CreateAddFileAction(IDeltaStorage storage, IList<Dictionary<string, string>> partitionValuesList, IOEntry file, int index, DeltaStatistics stats) => new AddFile {
            Path = file.Path.ToString().Substring(storage.Location.ToString().Length + 1),
            Size = file.Size,
            ModificationTime = file.LastModificationTime?.ToUnixTimeMilliseconds(),
            DataChange = true,
            Stats = JsonSerializer.Serialize(stats, new JsonSerializerOptions() {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }),
            PartitionValues = partitionValuesList.Count > 0 ? partitionValuesList[index] : new Dictionary<string, string>(),
        };


        /// <summary>
        /// Validates schema compatibility across multiple Parquet schema in parallel.
        /// </summary>
        public static async Task ValidateSchemasParallelAsync(
            List<ParquetSchema> schemas) {
            var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
            var exceptions = new ConcurrentBag<Exception>();

            ParquetSchema baseSchema = schemas.First();
            await Parallel.ForEachAsync(
                schemas.Skip(1), // Skip first file as it's the base schema
                options,
                async (schema, ct) => {
                    try {
                        if(!schema.Equals(baseSchema)) {
                            throw new InvalidOperationException($"Schema mismatch");
                        }
                    } catch(Exception ex) {
                        exceptions.Add(ex);
                    }
                });

            if(exceptions.Any()) {
                throw new AggregateException("Schema validation failed", exceptions);
            }
        }
    }
}