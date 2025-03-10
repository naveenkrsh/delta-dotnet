using System.Reflection;
using System.Text.Json;
using DeltaLake.Log;
using DeltaLake.Log.Actions;
using Parquet;
using Parquet.Schema;
using Stowage;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations {
    public class Operations {
        public static async Task ConvertToDeltaAsync(IFileStorage storage, IOPath location) {
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
            
            string clientVersion = $"DeltaIO-{Assembly.GetExecutingAssembly().GetName().Version}";
            var commitInfoModel = new CommitInfoModel(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                clientVersion,
                Operation.CREATE_TABLE.ToString(),
                new Dictionary<string, string>(),
                true,
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()
            );

            string jsonString = JsonSerializer.Serialize(commitInfoModel, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var doc = JsonDocument.Parse(jsonString);
            var commitInfo = new CommitInfo(doc.RootElement);
            
            commitLines.Add(new  CommitLine() {
                Commit = doc.RootElement,
            });
            
            var protocolEvolution = new ProtocolEvolution {
                MinReaderVersion = 1,
                MinWriterVersion = 2
            };
            
            commitLines.Add(new CommitLine(){Protocol = protocolEvolution});
            //actions.Add(protocolEvolution);
            
            var parquetSchemas = new List<ParquetSchema>();
            foreach(IOEntry parquetFile in parquetFiles) {
                await using Stream? stream = await storage.OpenRead(parquetFile.Path);
                if(stream == null) continue;
                using ParquetReader reader = await ParquetReader.CreateAsync(stream);
                parquetSchemas.Add(reader.Schema);

                actions.Add(new AddFile() {
                    Path = parquetFile.Path.ToString().Substring(location.ToString().Length+1),
                    Size = parquetFile.Size,
                    ModificationTime = parquetFile.LastModificationTime?.ToUnixTimeMilliseconds(),
                    DataChange = true,
                    PartitionValues = new Dictionary<string, string>(),
                });
            }
            string schemaString = ParquetToSparkSchemaConverter.ConvertToSparkJsonSchema(parquetSchemas.First());
            
            var schemaDocument = JsonDocument.Parse(schemaString);
            
            var metadata = new Metadata {
                Id = Guid.NewGuid().ToString(),
                SchemaString = JsonSerializer.Serialize(schemaDocument.RootElement),
                Format = new MetadataFormat(),
                PartitionColumns = Array.Empty<string>(),
                Configuration = new Dictionary<string, string>()
            };
            //actions.Add(metadata);
            
            commitLines.Add(new CommitLine(){MetaData = metadata});

            foreach(Action action in actions) {
                commitLines.Add(new CommitLine(){Add = (AddFile)action});
            }
            await log.WriteJsonAsCommitAsync(commitLines,0);
        }
    }

    public class ParquetFileNotFoundException : Exception {
    }

    public class TableAlreadyExistsException : Exception {
        public TableAlreadyExistsException() : base("A Delta Lake table already exists at that location.") {
        }
    }

    public class CommitInfoModel {
        public long Timestamp { get; set; }

        public string ClientVersion { get; set; }
        public string Operation { get; set; }
        public Dictionary<string, string> OperationParameters { get; set; }
        public bool IsBlindAppend { get; set; }
        public string TxnId { get; set; }
        public Dictionary<string, string> OperationMetrics { get; set; }

        // Constructor
        public CommitInfoModel(
            long timestamp,
            string clientVersion,
            string operation,
            Dictionary<string, string> operationParameters,
            bool isBlindAppend,
            string txnId,
            Dictionary<string, string> operationMetrics) {
            Timestamp = timestamp;
            ClientVersion = clientVersion;
            Operation = operation;
            OperationParameters = operationParameters;
            IsBlindAppend = isBlindAppend;
            TxnId = txnId;
            OperationMetrics = operationMetrics;
        }
    }
    
    public enum Operation
    {
        // Recorded when the table is created.
        CREATE_TABLE,

        // Recorded during batch inserts.
        WRITE,

        // Recorded during streaming inserts.
        STREAMING_UPDATE,

        // For any operation that doesn't fit the above categories.
        MANUAL_UPDATE
        
        
    }

    public static class OperationExtensions
    {
        // Method to get the string description for each operation
        public static string ToString(this Operation operation)
        {
            switch (operation)
            {
                case Operation.CREATE_TABLE:
                    return "CREATE TABLE";
                case Operation.WRITE:
                    return "WRITE";
                case Operation.STREAMING_UPDATE:
                    return "STREAMING UPDATE";
                case Operation.MANUAL_UPDATE:
                    return "Manual Update";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

}