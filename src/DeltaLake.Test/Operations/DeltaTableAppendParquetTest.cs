using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Parquet.Schema;
using Stowage;
using Xunit;

namespace DeltaLake.Test.Operations {
    public class DeltaTableAppendParquetTest {
        private readonly IFileStorage _storage;
        private readonly string _dataPath = Path.GetFullPath("data");

        public DeltaTableAppendParquetTest() {
            _storage = Files.Of.LocalDisk(_dataPath);
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldThrowTableNotFoundException_WhenHistoryIsEmpty() {
            var location = new IOPath("test/location");
            var path = new IOPath("test/path");
            var operations = DeltaTableOperations.Create(_storage, location);
            await Assert.ThrowsAsync<TableNotFoundException>(() => operations.AppendParquetAsync(path));
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldThrowParquetFileNotFoundException_WhenNoParquetFilesFound() {
            var location = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition");
            var path = new IOPath("test/path");
            var operations = DeltaTableOperations.Create(_storage, location);
            await Assert.ThrowsAsync<ParquetFileNotFoundException>(() => operations.AppendParquetAsync(path));
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataTrackPartitionedByMediaTypeId() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartition");
            PrepareTestTable("track.partitioned.mediatypeid.parquet.addpartition");
            var partitionedPath = new IOPath("MediaTypeId=6/");

            var operations = DeltaTableOperations.Create(_storage, tablePath);
            var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await operations.AppendParquetAsync(partitionedPath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(6, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Collection(table.Versions,
                v => Assert.Equal(0, v),
                v => Assert.Equal(1, v));
        }
        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataTrackPartitionedByMediaTypeIdFile() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartitionfile", "_delta_log", "00000000000000000001.json"));
            PrepareTestTable("track.partitioned.mediatypeid.parquet.addpartitionfile");

            var tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet.addpartitionfile");

            var partitionedPath = new IOPath("MediaTypeId=6/part-00000-22275672-2cd9-47f8-aba4-a02e8a445fb7.c000.snappy.parquet");

            var operations = DeltaTableOperations.Create(_storage, tablePath);
            var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await operations.AppendParquetAsync(partitionedPath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(6, table.DataFiles.Count);
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

            string partition6 = Path.Combine(trackPartitionedMediatypeidParquetPath, "MediaTypeId=6");

            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                    Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid"),
                    trackPartitionedMediatypeidParquetPath);

            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid", "MediaTypeId=5"),
                partition6);
        }

        [Fact]
        public async Task AppendParquetAsync_ShouldProcessParquetFiles_WhenValidDataArtistSimple() {

            IOPath simpleTablePath = new("chinook", "artist.simple.parquet.addparquet");
            string artistSimpleParquetPath = Path.Combine(_dataPath, "chinook", "artist.simple.parquet.addparquet");

            if(!Directory.Exists(artistSimpleParquetPath)) {
                Directory.CreateDirectory(artistSimpleParquetPath);
            }

            if(!Directory.EnumerateFileSystemEntries(artistSimpleParquetPath).Any()) {
                DeltaOperationTestHelper.CopyDirectory(
                    Path.Combine(_dataPath, "chinook", "artist.simple"),
                    artistSimpleParquetPath
                );
            }

            string sourceFileName = "part-00000-df960eb7-f439-480a-b59b-c145d2da0a1d-c000.snappy.parquet";
            string destinationFileName = "part-00000-81e36bbb-cde4-4611-844d-5ed98850bd36-c000.snappy.parquet";

            string sourceFilePath = Path.Combine(artistSimpleParquetPath, sourceFileName);
            string destinationFilePath = Path.Combine(artistSimpleParquetPath, destinationFileName);

            if(File.Exists(sourceFilePath) && !File.Exists(destinationFilePath)) {
                File.Copy(sourceFilePath, destinationFilePath);
            }

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet.addparquet", "_delta_log", "00000000000000000001.json"));
            var tablePath = new IOPath("chinook", "artist.simple.parquet.addparquet");
            var parquetPath = new IOPath(destinationFileName);
            var operations = DeltaTableOperations.Create(_storage, tablePath);
            await operations.AppendParquetAsync(parquetPath);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Equal(2, table.History.Count);
            Assert.Equal(2, table.DataFiles.Count);
            Assert.Equal(2, table.Versions.Count);
            Assert.Collection(table.Versions,
                v => Assert.Equal(0, v),
                v => Assert.Equal(1, v));
        }
    }
}
