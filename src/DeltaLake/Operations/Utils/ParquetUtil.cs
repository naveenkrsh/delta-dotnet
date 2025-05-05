using DeltaLake.Operations.Extensions;
using DeltaLake.Operations.Models;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations.Utils {
    internal static partial class ParquetUtil {

        public static DeltaStatistics CollectStatisticsAsync(ParquetReader reader) {
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
            foreach(ParquetSchema schema in schemas) {
                foreach(Field field in schema.Fields) {
                    if(field is DataField dataField && !fieldDict.ContainsKey(dataField.Name)) {
                        fieldDict.Add(dataField.Name, dataField);
                    }
                }
            }

            if(partitionSchema != null) {
                // Iterate through the partition schema and add its fields to the dictionary
                foreach(Field field in partitionSchema.Fields) {
                    if(field is DataField dataField && !fieldDict.ContainsKey(dataField.Name)) {
                        fieldDict.Add(dataField.Name, dataField);
                    }
                }
            }

            // Create a new ParquetSchema using the combined fields
            var combinedFields = fieldDict.Values.ToList();
            return new ParquetSchema(combinedFields);
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

            return new RowGroupStatistics(
                rowGroupReader.RowCount,
                rowGroupStats
            );
        }

        private static DeltaStatistics ComputeStatisticsAsync(List<RowGroupStatistics> rowGroupsStats) {
            var statistics = new DeltaStatistics();

            foreach(RowGroupStatistics rowGroupStats in rowGroupsStats) {
                statistics.NumRecords += rowGroupStats.RowCount;

                foreach(KeyValuePair<DataField, DataColumnStatistics> columnStatsEntry in rowGroupStats.ColumnStatistics) {
                    DataField field = columnStatsEntry.Key;
                    DataColumnStatistics columnStats = columnStatsEntry.Value;
                    string fieldName = field.Name;

                    UpdateMinValue(statistics, fieldName, columnStats.MinValue, field.ClrType);
                    UpdateMaxValue(statistics, fieldName, columnStats.MaxValue, field.ClrType);
                    UpdateNullCount(statistics, fieldName, columnStats.NullCount);
                }
            }

            return statistics;
        }
        private static void UpdateMinValue(DeltaStatistics stats, string fieldName, object? newMin, Type clrType) {
            if(newMin == null)
                return;

            if(stats.MinValues.TryGetValue(fieldName, out object? existingMin) && existingMin != null)
                stats.MinValues[fieldName] = ClrTypeExtensions.Min(existingMin, newMin, clrType);
            else
                stats.MinValues[fieldName] = newMin;
        }

        private static void UpdateMaxValue(DeltaStatistics stats, string fieldName, object? newMax, Type clrType) {
            if(newMax == null)
                return;

            if(stats.MaxValues.TryGetValue(fieldName, out object? existingMax) && existingMax != null)
                stats.MaxValues[fieldName] = ClrTypeExtensions.Max(existingMax, newMax, clrType);
            else
                stats.MaxValues[fieldName] = newMax;
        }

        private static void UpdateNullCount(DeltaStatistics stats, string fieldName, long? newNullCount) {
            if(!stats.NullCount.TryGetValue(fieldName, out long? existingNullCount) || existingNullCount == null) {
                stats.NullCount[fieldName] = newNullCount;
            } else if(newNullCount != null) {
                stats.NullCount[fieldName] = existingNullCount + newNullCount;
            }
        }

        private static void EnsureConsistentPartitioning(List<Dictionary<string, string>> partitionValuesList) {
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
            if(partitionSchema == null || !partitionValuesList.Any()) {
                return;
            }

            // Get partition field names from schema
            HashSet<string> schemaFields = new HashSet<string>(
                partitionSchema.Fields.Select(f => f.Name)
            );

            // Validate all partition values match schema fields
            foreach(Dictionary<string, string> partitionValues in partitionValuesList) {
                foreach(string partitionKey in partitionValues.Keys) {
                    if(!schemaFields.Contains(partitionKey)) {
                        throw new InvalidOperationException(
                            $"Partition field '{partitionKey}' in path '{location}' not found in partition schema."
                        );
                    }
                }
            }

            // Validate all schema fields are present in partition values
            if(partitionValuesList.Any()) {
                HashSet<string> partitionFields = new HashSet<string>(
                    partitionValuesList.First().Keys
                );

                foreach(string field in schemaFields) {
                    if(!partitionFields.Contains(field)) {
                        throw new InvalidOperationException(
                            $"Partition schema field '{field}' not found in partition values for path '{location}'."
                        );
                    }
                }
            }
        }
    }
}