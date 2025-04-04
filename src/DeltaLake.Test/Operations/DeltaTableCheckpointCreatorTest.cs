using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Stowage;
using Xunit;
using Op = DeltaLake.Operations;
namespace DeltaLake.Test.Operations
{
    public class DeltaTableCheckpointCreatorTest {
        private readonly IFileStorage _storage;

        public DeltaTableCheckpointCreatorTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task CreateClassisCheckpointAsync_ShouldThrowTableNotFoundException_WhenHistoryIsEmpty() {

            var location = new IOPath("test/location");
            await Assert.ThrowsAsync<TableNotFoundException>(() => DeltaTableCheckpointCreator.CreateClassisCheckpointAsync(_storage, location));
        }

        [Fact]
        public async Task CreateClassisCheckpointAsync_ShouldCreateCheckpoint_WhenValidData() {

     
            await _storage.Rm(new IOPath("chinook", "artist.trickle.checkpoint", "_delta_log", "000000000000000000013.checkpoint.parquet"));
            var tablePath = new IOPath("chinook", "artist.trickle.checkpoint");
            await Op.DeltaTableCheckpointCreator.CreateClassisCheckpointAsync(_storage, tablePath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Single(table.History);
            Assert.Equal(14, table.DataFiles.Count);
            Assert.Single(table.Versions);
            Assert.Equal(table.Versions, [13]);
        }
    }
}
