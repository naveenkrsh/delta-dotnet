using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Stowage;
using Xunit;

namespace DeltaLake.Test.Operations {
    public class DeltaTableCheckpointCreatorTest {
        private readonly IFileStorage _storage;
        private readonly string _dataPath = Path.GetFullPath("data");
        private readonly IOPath _checkpointPath = new("chinook", "artist.trickle.checkpoint");

        public DeltaTableCheckpointCreatorTest() {
            _storage = Files.Of.LocalDisk(_dataPath);
            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                Path.Combine(_dataPath, "chinook", "artist.trickle"),
                Path.Combine(_dataPath, "chinook", "artist.trickle.checkpoint"));

            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                Path.Combine(_dataPath, "chinook", "artist.simple"),
                Path.Combine(_dataPath, "chinook", "artist.simple.parquet.checkpoint"));
        }

        [Fact]
        public async Task CreateClassicCheckpointAsync_ShouldThrowTableNotFoundException_WhenHistoryIsEmpty() {
            var location = new IOPath("test/location");
            var operations = DeltaTableOperations.Create(_storage, location);
            await Assert.ThrowsAsync<TableNotFoundException>(() => operations.CreateClassicCheckpointAsync());
        }

        [Fact]
        public async Task CreateClassicCheckpointAsync_ShouldCreateCheckpoint_WhenValidData() {
            await _storage.Rm(new IOPath("chinook", "artist.trickle.checkpoint", "_delta_log", "000000000000000000013.checkpoint.parquet"));

            var operations = DeltaTableOperations.Create(_storage, _checkpointPath);
            await operations.CreateClassicCheckpointAsync();

            Table table = await Table.OpenAsync(_storage, _checkpointPath);
            Assert.Single(table.History);
            Assert.Equal(14, table.DataFiles.Count);
            Assert.Single(table.Versions);
            Assert.Equal(table.Versions, [13]);
            Assert.True(await _storage.Exists(new IOPath("chinook", "artist.trickle.checkpoint", "_delta_log", "00000000000000000013.checkpoint.parquet")));
        }

        [Fact]
        public async Task CreateClassicCheckpointAsync_ShouldCreateCheckpoint_WhenHistoryExists() {
            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet.checkpoint", "_delta_log", "00000000000000000000.checkpoint.parquet"));
            var tablePath = new IOPath("chinook", "artist.simple.parquet.checkpoint");
            var operations = DeltaTableOperations.Create(_storage, tablePath);
            await operations.CreateClassicCheckpointAsync();

            Assert.True(await _storage.Exists(new IOPath("chinook", "artist.simple.parquet.checkpoint", "_delta_log", "00000000000000000000.checkpoint.parquet")));
        }
    }
}
