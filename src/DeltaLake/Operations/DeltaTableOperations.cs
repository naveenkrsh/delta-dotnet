using Parquet.Schema;
using Stowage;

namespace DeltaLake.Operations {
    public class DeltaTableOperations {
        /// <summary>
        /// Converts a Parquet table to a Delta table.
        /// </summary>
        /// <param name="storage">The file storage interface.</param>
        /// <param name="location">The location of the Parquet table.</param>
        /// <param name="partitionSchema">Optional partition schema.</param>
        /// <param name="partitionStrategy">Optional partition strategy.</param>
        public static async Task ConvertParquetToDeltaAsync(
            IFileStorage storage,
            IOPath location,
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null) {
            await DeltaTableConverter.ConvertParquetToDeltaAsync(
                storage,
                location,
                partitionSchema,
                partitionStrategy
            );
        }

        /// <summary>
        /// Creates a checkpoint for a Delta table.
        /// </summary>
        /// <param name="storage">The file storage interface.</param>
        /// <param name="location">The location of the Delta table.</param>
        public static async Task CreateClassisCheckpointAsync(
            IFileStorage storage,
            IOPath location) {
            await DeltaTableCheckpointCreator.CreateClassisCheckpointAsync(
                storage,
                location
            );
        }

        /// <summary>
        /// Appends Parquet files to a Delta table.
        /// </summary>
        /// <param name="storage">The file storage interface.</param>
        /// <param name="location">The location of the Delta table.</param>
        /// <param name="path">The path to the Parquet files.</param>
        /// <param name="partitionSchema">Optional partition schema.</param>
        /// <param name="partitionStrategy">Optional partition strategy.</param>
        public static async Task AppendParquetAsync(
            IFileStorage storage,
            IOPath location,
            IOPath path,
            ParquetSchema? partitionSchema = null,
            IPartitionStrategy? partitionStrategy = null) {
            await DeltaTableParquetAppender.AppendParquetAsync(
                storage,
                location,
                path,
                partitionSchema,
                partitionStrategy
            );
        }
    }
}
