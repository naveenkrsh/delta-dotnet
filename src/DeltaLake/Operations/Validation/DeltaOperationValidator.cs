using System.Threading.Tasks;
using DeltaLake.Log;
using DeltaLake.Operations.Exceptions;
using DeltaLake.Operations.Extensions;
using DeltaLake.Operations.Storage;
using Stowage;

namespace DeltaLake.Operations.Validation {
    /// <summary>
    /// Provides validation methods for Delta table operations.
    /// </summary>
    internal static class DeltaOperationValidator {
        /// <summary>
        /// Validates that a Delta table exists at the specified location.
        /// </summary>
        public static async Task ValidateTableExists(IDeltaStorage storage) {
            DeltaLog log = new DeltaLog(storage.UnderlyingStorage, storage.Location);
            Log.Actions.Metadata? metadata = await log.GetLatestMetadataAsync();

            if(metadata == null) {
                throw new TableNotFoundException("No table metadata found at the specified location.");
            }
        }

        /// <summary>
        /// Validates that a Delta table does not exist at the specified location.
        /// </summary>
        public static async Task ValidateTableDoesNotExist(IDeltaStorage storage) {
            DeltaLog log = new DeltaLog(storage.UnderlyingStorage, storage.Location);
            Log.Actions.Metadata? metadata = await log.GetLatestMetadataAsync();

            if(metadata != null) {
                throw new TableAlreadyExistsException("A Delta table already exists at the specified location.");
            }
        }

        /// <summary>
        /// Validates that Parquet files exist at the specified location.
        /// </summary>
        public static async Task ValidateParquetFilesExist(IDeltaStorage storage, IOPath path) {

            List<IOEntry> parquetFiles = new List<IOEntry>();
            if(path.IsFolder) {
                IReadOnlyCollection<IOEntry> files = await storage.CachedStorage.Ls(path + "/", true);
                parquetFiles = files.Where(e => e.Path.IsFile && e.Name.EndsWith(".parquet")).ToList();
            } 
            else if(path.Name.EndsWith(".parquet")) {
                IOEntry? parquetIOEntry = await storage.CachedStorage.Stat(path);
                if(parquetIOEntry != null) {
                    parquetFiles.Add(parquetIOEntry);
                }
            }

            if(!parquetFiles.Any()) {
                throw new ParquetFileNotFoundException($"No Parquet files found at location: {path}");
            }
        }

        /// <summary>
        /// Validates partition compatibility between existing table and new files.
        /// </summary>
        public static async Task ValidatePartitionCompatibility(
            IDeltaStorage storage,
            IReadOnlyList<string> newPartitionColumns) {
            var log = new DeltaLog(storage.UnderlyingStorage, storage.Location);
            Log.Actions.Metadata? metadata = await log.GetLatestMetadataAsync();

            if(metadata == null) {
                return; // New table, no compatibility check needed
            }

            string[]? existingPartitions = metadata.PartitionColumns;
            if(!existingPartitions.SequenceEqual(newPartitionColumns)) {
                throw new InvalidOperationException(
                    "Partition schema mismatch between existing table and new files.");
            }
        }
    }
}