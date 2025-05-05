using DeltaLake.Log.Actions;
using DeltaLake.Operations.Storage;
using DeltaLake.Operations.Utils;
using DeltaLake.Operations.Validation;
using Stowage;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations.Commands {
    /// <summary>
    /// Command to append Parquet files to a Delta table.
    /// </summary>
    public class RemoveParquetCommand : DeltaTableOperationBase {
        private readonly IOPath _sourcePath;
        private readonly IOPath _parquetPath;

        public RemoveParquetCommand(
            IDeltaStorage storage,
            IOPath sourcePath)
            : base(storage) {
            _sourcePath = sourcePath;
            _parquetPath = new IOPath(Storage.Location, _sourcePath);
        }

        protected override async Task ValidateCoreAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            await DeltaOperationValidator.ValidateTableExists(Storage);
        }

        protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            Table table = await Table.OpenAsync(Storage.UnderlyingStorage, Storage.Location);

            List<DataFile> dataFiles = table.DataFiles.Where(x => x.Path.ToString().Contains(_parquetPath)).ToList();

            if(!dataFiles.Any())
                return;

            List<Action> actions = new List<Action>
            {
                new CommitInfo(DeltaTableUtil.CreateCommitInfo(OperationEnum.MANUAL_UPDATE))
            };

            long deletionTimestamp=  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach(DataFile dataFile in dataFiles) {
                actions.Add(new RemoveFile() {
                    Path = dataFile.Path.ToString().Substring(Storage.Location.ToString().Length + 1),
                    DeletionTimestamp = deletionTimestamp,
                    DataChange = true,
                    //ExtendedFileMetadata = true,
                    PartitionValues = (Dictionary<string, string>)dataFile.PartitionValues,
                    Size = dataFile.Size,
                    Stats = dataFile.BaseAction.Stats,
                    //Tags = dataFile.BaseAction.Tags,

                });
            }

            await table.Log.WriteJsonAsCommitAsync(actions, table.CurrentVersion + 1);
        }
    }
}