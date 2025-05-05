namespace DeltaLake.Operations.Exceptions {
    public class ParquetFileNotFoundException : Exception {
        public ParquetFileNotFoundException() {
        }

        public ParquetFileNotFoundException(string? message) : base(message) {
        }
    }
}