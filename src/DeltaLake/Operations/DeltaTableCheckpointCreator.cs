using DeltaLake.Log;
using DeltaLake.Operations.Exceptions;
using Stowage;

namespace DeltaLake.Operations {
    /// <summary>
    /// This class is responsible for creating a classic checkpoint for a Delta table.
    /// It reads the Delta log history and writes the actions to a Parquet file.
    /// </summary>
    public class DeltaTableCheckpointCreator {
        /// <summary>
        /// Creates a classic checkpoint for a Delta table.
        /// This method reads the Delta log history and writes the actions to a Parquet file.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="TableNotFoundException"></exception>
        public static async Task CreateClassisCheckpointAsync(IFileStorage storage,
            IOPath location) {
            var log = new DeltaLog(storage, location);
            IReadOnlyCollection<LogCommit> history = await log.ReadHistoryAsync();
            if(!history.Any())
                throw new TableNotFoundException();

            Table table = await Table.OpenAsync(storage, location);
            var existingActions = table.History.SelectMany(x => x.Actions).ToList();
            await log.WriteParquetAsClassicCheckPointAsync(existingActions, table.CurrentVersion);
        }
    }
}
