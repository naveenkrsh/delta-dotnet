using DeltaLake.Operations;
using Parquet.Schema;
using Stowage;
using Xunit;
using Op = DeltaLake.Operations.DeltaTableAppendParquet;

namespace DeltaLake.Test.Operations
{
    public class DeltaTableAppendParquetTest
    {
        private readonly IFileStorage _storage;

        public DeltaTableAppendParquetTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task TrackPartitionedByMediaTypeId() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition");

            var partitionedPath = new IOPath("MediaTypeId=5");
            
            var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await Op.AppendParquetAsync(_storage, tablePath, partitionedPath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(5, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Equal(table.Versions, [0, 1]);
        }

        [Fact]
        public async Task ArtistSimple() {

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet.addparquet", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "artist.simple.parquet.addparquet");
            var parquetPath = new IOPath("part-00000-df960eb7-f439-480a-b59b-c145d2da0a1d-c001.snappy.parquet");
            await Op.AppendParquetAsync(_storage, tablePath, parquetPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(2, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Equal(table.Versions, [0, 1]);
        }
    }
}
