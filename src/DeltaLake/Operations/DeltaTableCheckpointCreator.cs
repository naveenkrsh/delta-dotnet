using DeltaLake.Log;
using DeltaLake.Operations.Exceptions;
using Stowage;

namespace DeltaLake.Operations {
    public class DeltaTableCheckpointCreator {
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
