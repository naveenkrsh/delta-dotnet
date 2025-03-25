using Parquet.Schema;
using Stowage;
using Xunit;
using Op = DeltaLake.Operations.DeltaTableConverter;
namespace DeltaLake.Test.Operations {
    public class DeltaTableConverterTest {
        private readonly IFileStorage _storage;

        public DeltaTableConverterTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task ArtistSimple() {

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet", "_delta_log"));
            string tablePath = new IOPath("chinook", "artist.simple.parquet");
            await  Op.ConvertParquetToDeltaAsync(_storage, tablePath);
            
            Table table = await Table.OpenAsync(_storage,tablePath);
            Assert.Single(table.History);
        }

        [Fact]
        public async Task TrackPartitinedByMediaTypeId() {

            await _storage.Rm(new IOPath("chinook", "track.partitioned.mediatypeid.parquet", "_delta_log"));
            string tablePath = new IOPath("chinook", "track.partitioned.mediatypeid.parquet");

           var partitionSchema = new ParquetSchema(
                new DataField<int>("MediaTypeId")
            );
            await Op.ConvertParquetToDeltaAsync(_storage, tablePath, partitionSchema);

            Table table = await Table.OpenAsync(_storage, tablePath);
            Assert.Single(table.History);
        }
    }
}