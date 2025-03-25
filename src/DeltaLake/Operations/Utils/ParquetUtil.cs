using System.Collections.Concurrent;
using System.Text.Json;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Extensions;
using DeltaLake.Operations.Models;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations.Utils {
    internal static partial class ParquetUtil {


        /// <summary>
        /// <summary>
        /// Collects statistics for a Parquet file in parallel. However, performance is poor, so this method is not used yet.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="path"></param>
        /// <param name="rowGroupCount"></param>
        /// <returns></returns>
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="path"></param>
        /// <param name="rowGroupCount"></param>
        /// <returns></returns>
#if NET8_0_OR_GREATER
        public static async Task<DeltaStatistics> CollectStatisticsAsync(IFileStorage storage, IOPath path, int rowGroupCount) {

            ConcurrentBag<RowGroupStatistics> rowGroupsStats = new ConcurrentBag<RowGroupStatistics>();
            await Parallel.ForAsync(0, rowGroupCount,
                async (i, cancellationToken) => {
                    await using Stream? stream = await storage.OpenRead(path);
                    {
                        using(ParquetReader reader = await ParquetReader.CreateAsync(stream)) {
                            using(ParquetRowGroupReader rowGroupReader = reader.OpenRowGroupReader(i)) {
                                DataField[] dataFields = reader.Schema.GetDataFields();
                                RowGroupStatistics result = CollectRowGroupStatistics(dataFields, rowGroupReader);
                                rowGroupsStats.Add(result);
                            }
                        }
                    }
                }
            );

            DeltaStatistics statistics = ComputeStatisticsAsync(rowGroupsStats.ToList());
            return statistics;
        }
#else
        public static async Task<DeltaStatistics> CollectStatisticsAsync(IFileStorage storage, IOPath path, int rowGroupCount) {

            ConcurrentBag<RowGroupStatistics> rowGroupsStats = new ConcurrentBag<RowGroupStatistics>();

            await Parallel.ForEachAsync(Enumerable.Range(0, rowGroupCount), async (i, cancellationToken) => {
                await using Stream? stream = await storage.OpenRead(path);
                {
                    using(ParquetReader reader = await ParquetReader.CreateAsync(stream)) {
                        using(ParquetRowGroupReader rowGroupReader = reader.OpenRowGroupReader(i)) {
                            DataField[] dataFields = reader.Schema.GetDataFields();
                            RowGroupStatistics result = CollectRowGroupStatistics(dataFields, rowGroupReader);
                            rowGroupsStats.Add(result);
                        }
                    }
                }
            });

            DeltaStatistics statistics = ComputeStatisticsAsync(rowGroupsStats.ToList());
            return statistics;
        }
#endif

        public static async Task<DeltaStatistics> CollectStatisticsAsync(ParquetReader reader) {
            List<RowGroupStatistics> rowGroupsStats = new List<RowGroupStatistics>();
            for(int i = 0; i < reader.RowGroupCount; i++) {
                using(ParquetRowGroupReader rowGroupReader = reader.OpenRowGroupReader(i)) {
                    DataField[] dataFields = reader.Schema.GetDataFields();
                    RowGroupStatistics result = CollectRowGroupStatistics(dataFields, rowGroupReader);
                    rowGroupsStats.Add(result);
                }
            }
            DeltaStatistics statistics = ComputeStatisticsAsync(rowGroupsStats);
            return statistics;
        }

        public static void EnsureValidPartitionStrategy(ParquetSchema? partitionSchema, IPartitionStrategy? partitionStrategy) {
            if(partitionSchema != null && partitionStrategy == null) {
                throw new InvalidOperationException("Partition strategy must be provided when partition schema is present.");
            }
        }

        public static List<Dictionary<string, string>> ExtractPartitionValues(IPartitionStrategy? partitionStrategy, List<IOEntry> parquetFiles) {
            var partitionValuesList = new List<Dictionary<string, string>>();

            if(partitionStrategy != null) {
                foreach(IOEntry file in parquetFiles) {
                    Dictionary<string, string> partitionKeyValues = partitionStrategy.ExtractPartitionKeyValues(file.Path.ToString());
                    if(partitionKeyValues.Any()) {
                        partitionValuesList.Add(partitionKeyValues);
                    }
                }
            }

            return partitionValuesList;
        }

        public static ParquetSchema MergeSchemas(List<ParquetSchema> schemas, ParquetSchema? partitionSchema) {
            var fieldDict = new Dictionary<string, DataField>();

            // Iterate through each schema and add its fields to the dictionary
            foreach(ParquetSchema schema in schemas)
                foreach(Field field in schema.Fields)
                    if(field is DataField dataField && !fieldDict.ContainsKey(dataField.Name))
                        fieldDict.Add(dataField.Name, dataField);

            if(partitionSchema != null) {
                // Iterate through the partition schema and add its fields to the dictionary
                foreach(Field field in partitionSchema.Fields)
                    if(field is DataField dataField && !fieldDict.ContainsKey(dataField.Name))
                        fieldDict.Add(dataField.Name, dataField);
            }

            // Create a new ParquetSchema using the combined fields
            var combinedFields = fieldDict.Values.ToList();
            return new ParquetSchema(combinedFields);
        }

        public static async Task<ParquetProcessingResult> ProcessParquetFilesAsync(IFileStorage storage,
            IOPath location,
            List<IOEntry> parquetFiles,
            IPartitionStrategy? partitionStrategy) {
            var parquetSchemas = new List<ParquetSchema>();
            var actions = new List<AddFile>();

            foreach(IOEntry parquetFile in parquetFiles) {
                Dictionary<string, string> partitionValues = new Dictionary<string, string>();
                if(partitionStrategy != null)
                    partitionValues = partitionStrategy.ExtractPartitionKeyValues(parquetFile.Path);

                await using Stream? stream = await storage.OpenRead(parquetFile.Path);
                {
                    if(stream == null)
                        continue;

                    using(ParquetReader reader = await ParquetReader.CreateAsync(stream)) {
                        parquetSchemas.Add(reader.Schema);
                        DeltaStatistics statistics = await CollectStatisticsAsync(reader);
                        actions.Add(new AddFile() {
                            Path = parquetFile.Path.ToString().Substring(location.ToString().Length + 1),
                            Size = parquetFile.Size,
                            ModificationTime = parquetFile.LastModificationTime?.ToUnixTimeMilliseconds(),
                            DataChange = true,
                            PartitionValues = partitionValues,
                            Stats = JsonSerializer.Serialize(statistics, new JsonSerializerOptions() {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            }),
                        });
                    }
                }
            }

            return new ParquetProcessingResult(parquetSchemas, actions);
        }

        public static void ValidateAndEnsurePartitioning(IOPath location, ParquetSchema? partitionSchema, List<Dictionary<string, string>> partitionValuesList) {
            EnsurePartitionSchema(partitionSchema, partitionValuesList);
            EnsureConsistentPartitioning(partitionValuesList);
            ValidatePartitionSchema(location, partitionSchema, partitionValuesList);
        }

        private static RowGroupStatistics CollectRowGroupStatistics(DataField[] dataFields, ParquetRowGroupReader rowGroupReader) {
            Dictionary<DataField, DataColumnStatistics> rowGroupStats = new Dictionary<DataField, DataColumnStatistics>();

            foreach(DataField field in dataFields) {
                DataColumnStatistics? columnStatistics = rowGroupReader.GetStatistics(field);
                if(columnStatistics is not null) {
                    rowGroupStats.Add(field, columnStatistics);
                }
            }

            var result = new RowGroupStatistics(
                rowGroupReader.RowCount,
                rowGroupStats
            );

            return result;
        }

        private static DeltaStatistics ComputeStatisticsAsync(List<RowGroupStatistics> rowGroupsStats) {
            var statistics = new DeltaStatistics();

            foreach(RowGroupStatistics rowGroupStats in rowGroupsStats) {
                statistics.NumRecords += rowGroupStats.RowCount;

                foreach(KeyValuePair<DataField, DataColumnStatistics> columnStatisticsEntry in rowGroupStats.ColumnStatistics) {
                    DataField field = columnStatisticsEntry.Key;
                    DataColumnStatistics dataColumnStatistics = columnStatisticsEntry.Value;


                    if(statistics.MinValues.TryGetValue(field.Name, out object minValue))
                        statistics.MinValues[field.Name] = ClrTypeExtensions.Min(minValue, dataColumnStatistics.MinValue, field.ClrType);
                    else
                        statistics.MinValues.Add(field.Name, dataColumnStatistics.MinValue);

                    if(statistics.MaxValues.TryGetValue(field.Name, out object maxValue))
                        statistics.MaxValues[field.Name] = ClrTypeExtensions.Max(maxValue, dataColumnStatistics.MaxValue, field.ClrType);
                    else
                        statistics.MaxValues.Add(field.Name, dataColumnStatistics.MaxValue);

                    if(statistics.NullCount.TryGetValue(field.Name, out long? nullCount)) {
                        if(nullCount is not null && dataColumnStatistics.NullCount is not null)
                            statistics.NullCount[field.Name] = statistics.NullCount[field.Name] + nullCount;

                        if(nullCount is null && dataColumnStatistics.NullCount is not null)
                            statistics.NullCount[field.Name] = nullCount;
                    } else
                        statistics.NullCount.Add(field.Name, dataColumnStatistics.NullCount);
                }
            }
            return statistics;
        }

        private static void EnsureConsistentPartitioning(List<Dictionary<string, string>> partitionValuesList) {
            // Select the keys as arrays
            List<string[]> partitionKeysList = partitionValuesList
            .Select(dict => dict.Keys.ToArray())
            .ToList();
            if(partitionKeysList.Count > 1) {
                string[] firstKeys = partitionKeysList[0];
                for(int i = 1; i < partitionKeysList.Count; i++) {
                    if(!firstKeys.SequenceEqual(partitionKeysList[i])) {
                        throw new InvalidOperationException("All parquet files must have the same partitioning.");
                    }
                }
            }
        }

        private static void EnsurePartitionSchema(ParquetSchema? partitionSchema, List<Dictionary<string, string>> partitionValuesList) {
            if(partitionValuesList.Any() && partitionSchema == null) {
                throw new InvalidOperationException("Partition schema must be provided when partition values are present.");
            }
        }

        private static void ValidatePartitionSchema(IOPath location, ParquetSchema? partitionSchema, List<Dictionary<string, string>> partitionValuesList) {
            if(partitionSchema is not null) {
                Dictionary<string, string> partitionValues = partitionValuesList.First();

                if(partitionValues.Count != partitionSchema.DataFields.Length) {
                    throw new InvalidOperationException(location + " does not contain all partition keys.");
                }

                foreach(string key in partitionSchema.DataFields.Select(f => f.Name)) {
                    if(!partitionValues.ContainsKey(key)) {
                        throw new InvalidOperationException("Partition values must contain all partition keys.");
                    }
                }
            }
        }
    }
}