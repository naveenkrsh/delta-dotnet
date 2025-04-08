using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Parquet.Schema;
using Stowage;
using Xunit;

namespace DeltaLake.Test.Operations {
    public class DeltaTableConverterTest
    {
        private readonly IFileStorage _storage;
        private readonly IOPath _simpleTablePath = new("chinook", "artist.simple.parquet");
        private readonly IOPath _partitionedMediatypeidTablePath = new("chinook", "track.partitioned.mediatypeid.parquet");
        private readonly string _dataPath = Path.GetFullPath("data");

        public DeltaTableConverterTest()
        {
            _storage = Files.Of.LocalDisk(_dataPath);

            string artistSimpleParquetPath = Path.Combine(_dataPath, "chinook", "artist.simple.parquet");
            string trackPartitionedMediatypeidParquetPath = Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid.parquet");

            if (!Directory.Exists(artistSimpleParquetPath))
            {
                Directory.CreateDirectory(artistSimpleParquetPath);
            }

            if (!Directory.EnumerateFileSystemEntries(artistSimpleParquetPath).Any())
            {
                DeltaOperationTestHelper.CopyDirectory(
                    Path.Combine(_dataPath, "chinook", "artist.simple"),
                    artistSimpleParquetPath
                );
            }
            if (Directory.Exists(Path.Combine(artistSimpleParquetPath, "_delta_log")))
                Directory.Delete(Path.Combine(artistSimpleParquetPath, "_delta_log"), recursive: true);

            if (!Directory.Exists(trackPartitionedMediatypeidParquetPath))
            {
                Directory.CreateDirectory(trackPartitionedMediatypeidParquetPath);
            }

            if (!Directory.Exists(trackPartitionedMediatypeidParquetPath) ||
                !Directory.EnumerateFileSystemEntries(trackPartitionedMediatypeidParquetPath).Any())
            {

                DeltaOperationTestHelper.CopyDirectory(
                    Path.Combine(_dataPath, "chinook", "track.partitioned.mediatypeid"),
                    trackPartitionedMediatypeidParquetPath
                );
            }
            if (Directory.Exists(Path.Combine(trackPartitionedMediatypeidParquetPath, "_delta_log")))
                Directory.Delete(Path.Combine(trackPartitionedMediatypeidParquetPath, "_delta_log"), recursive: true);
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldThrowTableAlreadyExistsException_WhenHistoryIsNotEmpty()
        {
            await DeltaTableOperations.ConvertParquetToDeltaAsync(_storage, _simpleTablePath);
            await Assert.ThrowsAsync<TableAlreadyExistsException>(() => DeltaTableOperations.ConvertParquetToDeltaAsync(_storage, _simpleTablePath));
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldThrowParquetFileNotFoundException_WhenNoParquetFilesFound()
        {
            var location = new IOPath("/test/location");
            await Assert.ThrowsAsync<ParquetFileNotFoundException>(() => DeltaTableOperations.ConvertParquetToDeltaAsync(_storage, location));
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldCreateNewTable_WhenNoHistoryExists()
        {
            await DeltaTableOperations.ConvertParquetToDeltaAsync(_storage, _simpleTablePath);

            Table table = await Table.OpenAsync(_storage, _simpleTablePath);
            Assert.Single(table.History);
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldCreateNewTable_WithMediaTypeIdPartition()
        {
            var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await DeltaTableOperations.ConvertParquetToDeltaAsync(_storage, _partitionedMediatypeidTablePath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, _partitionedMediatypeidTablePath);
            Assert.Single(table.History);
        }
    }
}
