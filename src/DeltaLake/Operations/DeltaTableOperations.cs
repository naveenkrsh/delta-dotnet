using DeltaLake.Operations.Commands;
using DeltaLake.Operations.Factories;
using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations {
    /// <summary>
    /// Facade for Delta table operations with enhanced performance through caching.
    /// </summary>
    public class DeltaTableOperations {
        private readonly DeltaTableOperationFactory _factory;

        private DeltaTableOperations(IFileStorage storage, IOPath location) {
            _factory = new DeltaTableOperationFactory(storage, location);
        }

        /// <summary>
        /// Creates a new instance of DeltaTableOperations.
        /// </summary>
        public static DeltaTableOperations Create(IFileStorage storage, IOPath location) {
            return new DeltaTableOperations(storage, location);
        }

        /// <summary>
        /// Converts a Parquet table to a Delta table.
        /// </summary>
        public async Task ConvertParquetToDeltaAsync(
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null) {
            OperationParameters parameters = new OperationParameters {
                PartitionSchema = partitionSchema,
                PartitionStrategy = partitionStrategy
            };

            IDeltaTableOperation operation = _factory.CreateOperation(OperationType.ConvertParquetToDelta, parameters);
            await operation.ExecuteAsync();
        }

        /// <summary>
        /// Creates a checkpoint for a Delta table.
        /// </summary>
        public async Task CreateClassicCheckpointAsync() {
            IDeltaTableOperation operation = _factory.CreateOperation(OperationType.CreateCheckpoint, new OperationParameters());
            await operation.ExecuteAsync();
        }

        /// <summary>
        /// Appends Parquet files to a Delta table.
        /// </summary>
        public async Task AppendParquetAsync(
            IOPath sourcePath,
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null) {
            OperationParameters parameters = new OperationParameters {
                SourcePath = sourcePath,
                PartitionSchema = partitionSchema,
                PartitionStrategy = partitionStrategy
            };

            IDeltaTableOperation operation = _factory.CreateOperation(OperationType.AppendParquet, parameters);
            await operation.ExecuteAsync();
        }

        /// <summary>
        /// Removes Parquet files from a Delta table.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public async Task RemoveParquetAsync(IOPath sourcePath) {
            OperationParameters parameters = new OperationParameters {
                SourcePath = sourcePath
            };

            IDeltaTableOperation operation = _factory.CreateOperation(OperationType.RemoveParquet, parameters);
            await operation.ExecuteAsync();
        }
    }
}
