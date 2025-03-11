using Stowage;
using Xunit;
using Op = DeltaLake.Operations.DeltaTablePartitionAdder;

namespace DeltaLake.Test.Operations
{
    public class DeltaTablePartitionAdderTest
    {
        private readonly IFileStorage _storage;

        public DeltaTablePartitionAdderTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task TrackPartitionedByMediaTypeId() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition");

            var partitionedPath = new IOPath("MediaTypeId=5");
            
            await Op.AddPartitionAsync(_storage, tablePath, partitionedPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(5, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Equal(table.Versions, [0, 1]);
        }
    }
}
