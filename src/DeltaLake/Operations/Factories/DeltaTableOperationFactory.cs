using DeltaLake.Operations.Commands;
using DeltaLake.Operations.Storage;
using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations.Factories {
    /// <summary>
    /// Factory for creating Delta table operations with integrated caching.
    /// </summary>
    public class DeltaTableOperationFactory {
        private readonly IDeltaStorage _storage;

        public DeltaTableOperationFactory(IFileStorage storage, IOPath location, bool enableCaching = true) {
            _storage = new DeltaStorage(storage, location);
            //_storage = enableCaching ? new CachedDeltaStorage(baseStorage) : baseStorage;
        }

        public IDeltaTableOperation CreateOperation(OperationType operationType, OperationParameters parameters) {
            return operationType switch {
                OperationType.ConvertParquetToDelta => new ConvertParquetToDeltaCommand(
                    _storage,
                    parameters.PartitionSchema,
                    parameters.PartitionStrategy),

                OperationType.CreateCheckpoint => new CreateCheckpointCommand(_storage),

                OperationType.AppendParquet => new AppendParquetCommand(
                    _storage,
                    parameters.SourcePath ?? throw new ArgumentNullException(nameof(parameters.SourcePath)),
                    parameters.PartitionSchema,
                    parameters.PartitionStrategy),

                OperationType.RemoveParquet => new RemoveParquetCommand(
                        _storage,
                        parameters.SourcePath ?? throw new ArgumentNullException(nameof(parameters.SourcePath))),

                _ => throw new ArgumentException($"Unknown operation type: {operationType}")
            };
        }
    }

    /// <summary>
    /// Represents the type of Delta table operation to create.
    /// </summary>
    public enum OperationType {
        ConvertParquetToDelta,
        CreateCheckpoint,
        AppendParquet,
        RemoveParquet
    }

    /// <summary>
    /// Parameters for Delta table operations.
    /// </summary>
    public class OperationParameters {
        public IOPath? SourcePath { get; set; }
        public ParquetSchema? PartitionSchema { get; set; }
        public IPartitionStrategy? PartitionStrategy { get; set; }

        public static OperationParameters Empty => new OperationParameters();
    }
}