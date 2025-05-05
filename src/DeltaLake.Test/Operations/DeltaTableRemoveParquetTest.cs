using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Stowage;
using Xunit;

namespace DeltaLake.Test.Operations {
    public class DeltaTableRemoveParquetTest {

        private readonly IFileStorage _storage;
        private readonly string _dataPath = Path.GetFullPath("data");

        public DeltaTableRemoveParquetTest() {
            _storage = Files.Of.LocalDisk(_dataPath);
        }

        [Fact]
        public async Task RemoveParquetAsync_ShouldThrowTableNotFoundException_WhenHistoryIsEmpty() {
            var location = new IOPath("test/location");
            var path = new IOPath("test/path");
            var operations = DeltaTableOperations.Create(_storage, location);
            await Assert.ThrowsAsync<TableNotFoundException>(() => operations.RemoveParquetAsync(path));
        }


        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataTrackPartitionedByMediaTypeId() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.removepartition", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.removepartition");
            PrepareTestTable("track.partitioned.mediatypeid.parquet.removepartition");
            var partitionedPath = new IOPath("MediaTypeId=5/");

            var operations = DeltaTableOperations.Create(_storage, tablePath);

            await operations.RemoveParquetAsync(partitionedPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(4, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Collection(table.Versions,
                v => Assert.Equal(0, v),
                v => Assert.Equal(1, v));
        }
        [Fact]
        public async Task RemoveParquetAsync_ShouldProcessParquetFiles_WhenValidDataTrackPartitionedByMediaTypeIdFile() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.removepartitionfile", "_delta_log", "00000000000000000001.json"));
            PrepareTestTable("track.partitioned.mediatypeid.parquet.removepartitionfile");

            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.removepartitionfile");

            var partitionedPath = new IOPath("MediaTypeId=5/part-00000-22275672-2cd9-47f8-aba4-a02e8a445fb7.c000.snappy.parquet");

            var operations = DeltaTableOperations.Create(_storage, tablePath);
       
            await operations.RemoveParquetAsync(partitionedPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(4, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Collection(table.Versions,
                v => Assert.Equal(0, v),
                v => Assert.Equal(1, v));
        }

        private void PrepareTestTable(string table) {
            string trackPartitionedMediatypeidParquetPath = Path.Combine(_dataPath, "chinook", table);
            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid"),
                trackPartitionedMediatypeidParquetPath);
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataArtistSimple() {
            Setup();

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet.remove", "_delta_log", "000000000000000000014.json"));
            var tablePath = new IOPath("chinook", "artist.simple.parquet.remove");
            var operations = DeltaTableOperations.Create(_storage, tablePath);

            string parquetPath = "part-00000-fa663253-2f5d-4700-994e-58d11ce37882-c000.snappy";
            await operations.RemoveParquetAsync(parquetPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(5, table.History.Count);
            Assert.Equal(13, table.DataFiles.Count);
            Assert.Equal(5, table.Versions.Count);
            Assert.Collection(table.Versions,
                v => Assert.Equal(10, v),
                v => Assert.Equal(11, v),
                v => Assert.Equal(12, v),
                v => Assert.Equal(13, v),
                v => Assert.Equal(14, v));
        }

        private void Setup() {
            IOPath simpleTablePath = new("chinook", "artist.simple.parquet.remove");
            string artistTrickleParquetPath = Path.Combine(_dataPath, "chinook", "artist.simple.parquet.remove");

            if(!Directory.Exists(artistTrickleParquetPath)) {
                Directory.CreateDirectory(artistTrickleParquetPath);
            }

            if(!Directory.EnumerateFileSystemEntries(artistTrickleParquetPath).Any()) {
                DeltaOperationTestHelper.CopyDirectory(
                    Path.Combine(_dataPath, "chinook", "artist.trickle"),
                    artistTrickleParquetPath
                );
            }
        }
    }
}
