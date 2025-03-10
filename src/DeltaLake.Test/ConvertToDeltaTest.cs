using Parquet.Serialization;
using Stowage;
using Xunit;
using Op = DeltaLake.Operations.Operations;
namespace DeltaLake.Test {
    public class ConvertToDeltaTest {
        private readonly IFileStorage _storage;

        public ConvertToDeltaTest() {
            _storage = Files.Of.LocalDisk(Path.GetFullPath(Path.Combine("data")));
        }

        [Fact]
        public async Task ArtistSimple() {

            await _storage.Rm(new IOPath("chinook", "artist.simple.parquet", "_delta_log"));
            string tablePath = new IOPath("chinook", "artist.simple.parquet");
            await  Op.ConvertToDeltaAsync(_storage, tablePath);
            
            Table table = await Table.OpenAsync(_storage,tablePath);
            Assert.Single(table.History);
        }
    }
}