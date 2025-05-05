using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Parquet.Schema;
using Stowage;
using Xunit;

namespace DeltaLake.Test.Operations {
    public class DeltaTableConverterTest {
        private readonly IFileStorage _storage;
        private readonly string _dataPath = Path.GetFullPath("data");
        public DeltaTableConverterTest() {
            _storage = Files.Of.LocalDisk(_dataPath);
            string artistSimpleParquetPath = Path.Combine(_dataPath, "chinook", "artist.simple.parquet");
            string trackPartitionedMediatypeidParquetPath = Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid.parquet");

            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                Path.Combine(_dataPath, "chinook", "artist.simple"),
                artistSimpleParquetPath);

            DeltaOperationTestHelper.DeleteDeltaLogFolder(artistSimpleParquetPath);

            DeltaOperationTestHelper.CopyDirectoryIfNotExists(
                Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid"),
                trackPartitionedMediatypeidParquetPath);
            DeltaOperationTestHelper.DeleteDeltaLogFolder(trackPartitionedMediatypeidParquetPath);
        }



        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldThrowTableAlreadyExistsException_WhenHistoryIsNotEmpty() {
            IOPath location = new IOPath("chinook", "artist.simple.parquet");

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet", "_delta_log"));
            IOPath tablePath = new IOPath("chinook", "artist.simple.parquet");
            var operations = DeltaTableOperations.Create(_storage, tablePath);
            await operations.ConvertParquetToDeltaAsync();

            var operations1 = DeltaTableOperations.Create(_storage, location);
            await Assert.ThrowsAsync<TableAlreadyExistsException>(() => operations1.ConvertParquetToDeltaAsync());
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldThrowParquetFileNotFoundException_WhenNoParquetFilesFound() {
            IOPath location = new IOPath("/test/location");
            var operations = DeltaTableOperations.Create(_storage, location);
            await Assert.ThrowsAsync<ParquetFileNotFoundException>(() => operations.ConvertParquetToDeltaAsync());
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldCreateNewTable_WhenNoHistoryExists() {
            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet", "_delta_log"));
            IOPath tablePath = new IOPath("chinook", "artist.simple.parquet");
            var operations = DeltaTableOperations.Create(_storage, tablePath);
            await operations.ConvertParquetToDeltaAsync();

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Single(table.History);
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldCreateNewTable_WithMediaTypeIdPartition() {
            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet", "_delta_log"));
            IOPath tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet");

            ParquetSchema partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            var operations = DeltaTableOperations.Create(_storage, tablePath);
            await operations.ConvertParquetToDeltaAsync(partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Single(table.History);
        }
    }
}
