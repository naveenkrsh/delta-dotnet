using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Parquet.Schema;
using Stowage;
using Xunit;

namespace DeltaLake.Test.Operations
{
    public class DeltaTableAppendParquetTest
    {
        private readonly IFileStorage _storage;

        public DeltaTableAppendParquetTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldThrowTableNotFoundException_WhenHistoryIsEmpty() {
            var location = new IOPath("test/location");
            var path = new IOPath("test/path");
            await Assert.ThrowsAsync<TableNotFoundException>(() => DeltaTableAppendParquet.AppendParquetAsync(_storage, location, path));
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldThrowParquetFileNotFoundException_WhenNoParquetFilesFound() {
            var location = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition");
            var path = new IOPath("test/path");

            await Assert.ThrowsAsync<ParquetFileNotFoundException>(() => DeltaTableAppendParquet.AppendParquetAsync(_storage, location, path));
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataTrackPartitionedByMediaTypeId() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition");

            var partitionedPath = new IOPath("MediaTypeId=5");
            
            var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await DeltaTableAppendParquet.AppendParquetAsync(_storage, tablePath, partitionedPath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(5, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Equal(table.Versions, [0, 1]);
        }
        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataTrackPartitionedByMediaTypeIdFile() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartitionfile", "_delta_log", "00000000000000000001.json"));

            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartitionfile");

            var partitionedPath = new IOPath("MediaTypeId=5/part-00000-22275672-2cd9-47f8-aba4-a02e8a445fb7.c000.snappy.parquet");

            var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await DeltaTableAppendParquet.AppendParquetAsync(_storage, tablePath, partitionedPath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(5, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Equal(table.Versions, [0, 1]);
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataArtistSimple() {

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet.addparquet", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "artist.simple.parquet.addparquet");
            var parquetPath = new IOPath("part-00000-df960eb7-f439-480a-b59b-c145d2da0a1d-c001.snappy.parquet");
            await DeltaTableAppendParquet.AppendParquetAsync(_storage, tablePath, parquetPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(2, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Equal(table.Versions, [0, 1]);
        }
    }
}
