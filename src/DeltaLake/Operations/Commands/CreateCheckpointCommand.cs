using DeltaLake.Operations.Storage;
using DeltaLake.Operations.Validation;

namespace DeltaLake.Operations.Commands
{
    /// <summary>
    /// Command to create a checkpoint for a Delta table.
    /// </summary>
    public class CreateCheckpointCommand : DeltaTableOperationBase
    {
        public CreateCheckpointCommand(IDeltaStorage storage) : base(storage) { }

        protected override async Task ValidateCoreAsync()
        {
            await DeltaOperationValidator.ValidateTableExists(Storage);
        }

        protected override async Task ExecuteCoreAsync()
        {
            Table table = await Table.OpenAsync(Storage.UnderlyingStorage, Storage.Location);
            List<DeltaLake.Log.Actions.Action> existingActions = table.History.SelectMany(x => x.Actions).ToList();
            await table.Log.WriteParquetAsClassicCheckPointAsync(existingActions, table.CurrentVersion);
        }
    }
}