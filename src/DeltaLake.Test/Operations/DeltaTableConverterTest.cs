using DeltaLake.Operations;
using DeltaLake.Operations.Exceptions;
using Parquet.Schema;
using Stowage;
using Xunit;
namespace DeltaLake.Test.Operations {
    public class DeltaTableConverterTest {
        private readonly IFileStorage _storage;

        public DeltaTableConverterTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldThrowTableAlreadyExistsException_WhenHistoryIsNotEmpty() {
            var location = new IOPath("chinook", "artist.simple.parquet");
            await Assert.ThrowsAsync<TableAlreadyExistsException>(() => DeltaTableConverter.ConvertParquetToDeltaAsync(_storage, location));
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldThrowParquetFileNotFoundException_WhenNoParquetFilesFound() {
            var location = new IOPath("/test/location");
            await Assert.ThrowsAsync<ParquetFileNotFoundException>(() => DeltaTableConverter.ConvertParquetToDeltaAsync(_storage, location));
        }
        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldCreateNewTable_WhenNoHistoryExists() {

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet", "_delta_log"));
            string tablePath = new IOPath("chinook", "artist.simple.parquet");
            await DeltaTableConverter.ConvertParquetToDeltaAsync(_storage, tablePath);
            
            Table table = await Table.OpenAsync(_storage,tablePath);
            Assert.Single(table.History);
        }

        [Fact]
        public async Task ConvertParquetToDeltaAsync_ShouldCreateNewTable_WithMediaTypeIdPartition() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet", "_delta_log"));
            string tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet");

           var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await DeltaTableConverter.ConvertParquetToDeltaAsync(_storage, tablePath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Single(table.History);
        }
    }
}