using System.Reflection;
using System.Text.Json;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Extensions;
using DeltaLake.Operations.Models;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Stowage;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations.Utils {
    internal static class DeltaTableUtil {

        public static ParquetSchema MergeSchemas(List<ParquetSchema> schemas, List<Dictionary<string, string>> partitionValuesList) {

            string[] partitionColumns = partitionValuesList.Any() ? partitionValuesList.First().Keys.ToArray() : Array.Empty<string>();

            var fieldDict = new Dictionary<string, DataField>();

            // Iterate through each schema and add its fields to the dictionary
            foreach(ParquetSchema schema in schemas)
                foreach(Field field in schema.Fields)
                    if(field is DataField dataField && !fieldDict.ContainsKey(dataField.Name))
                        fieldDict.Add(dataField.Name, dataField);

            // Add partitioned columns to the dictionary with inferred data types
            foreach(string partitionColumn in partitionColumns)
                if(!fieldDict.ContainsKey(partitionColumn)) {

                    //TODO: This can be taken as input from the user

                    // Collect all sample values for the partition column
                    IEnumerable<string> sampleValues = partitionValuesList.SelectMany(dict => dict)
                                                          .Where(kv => kv.Key == partitionColumn && kv.Value != null)
                                                          .Select(kv => kv.Value);

                    Type dataType = sampleValues.Any() ? sampleValues.InferClrType() : typeof(string);

                    DataField partitionField = dataType switch {
                        Type t when t == typeof(int) => new DataField<int>(partitionColumn),
                        Type t when t == typeof(double) => new DataField<double>(partitionColumn),
                        Type t when t == typeof(bool) => new DataField<bool>(partitionColumn),
                        Type t when t == typeof(DateTime) => new DataField<DateTime>(partitionColumn),
                        Type t when t == typeof(long) => new DataField<long>(partitionColumn),
                        _ => new DataField<string>(partitionColumn),
                    };

                    fieldDict.Add(partitionColumn, partitionField);
                }

            // Create a new ParquetSchema using the combined fields
            var combinedFields = fieldDict.Values.ToList();
            return new ParquetSchema(combinedFields);
        }

        public static async Task<DeltaStatistics> CollectStatisticsAsync(ParquetReader reader) {
            var statistics = new DeltaStatistics();

            for(int i = 0; i < reader.RowGroupCount; i++) {
                using ParquetRowGroupReader rowGroupReader = reader.OpenRowGroupReader(i);
                statistics.NumRecords += rowGroupReader.RowCount;

                foreach(DataField field in reader.Schema.GetDataFields()) {
                    DataColumn column = await rowGroupReader.ReadColumnAsync(field);
                    DataColumnStatistics dataColumnStatistics = column.Statistics;

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

        public static JsonElement CreateCommitInfo(OperationEnum operation) {
            string clientVersion = $"DeltaIO-{Assembly.GetExecutingAssembly().GetName().Version}";
            var commitInfoModel = new CommitInfoModel(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                clientVersion,
                operation.ToDeltaOperationString(),
                new Dictionary<string, string>(),
                true,
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()
            );

            string jsonString = JsonSerializer.Serialize(commitInfoModel, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement;
        }


        public static async Task<ParquetProcessingResult> ProcessParquetFilesAsync(IFileStorage storage, IOPath location, List<IOEntry> parquetFiles) {
            var parquetSchemas = new List<ParquetSchema>();
            var partitionValuesList = new List<Dictionary<string, string>>();
            var actions = new List<AddFile>();

            foreach(IOEntry parquetFile in parquetFiles) {
                await using Stream? stream = await storage.OpenRead(parquetFile.Path);
                if(stream == null)
                    continue;
                using ParquetReader reader = await ParquetReader.CreateAsync(stream);
                parquetSchemas.Add(reader.Schema);

                Dictionary<string, string> partitionValues = HivePartitionUtil.ExtractPartitionKeyValues(parquetFile.Path);
                partitionValuesList.Add(partitionValues);

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

            return new ParquetProcessingResult(parquetSchemas, partitionValuesList, actions);
        }



        public static void MapActionsToCommitLines(List<Action> actions, List<CommitLine> commitLines) {
            foreach(Action action in actions)                 commitLines.Add(new CommitLine() { Add = (AddFile)action });
        }
    }
}