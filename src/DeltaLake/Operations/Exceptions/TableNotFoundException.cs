namespace DeltaLake.Operations.Exceptions {
    [Serializable]
    internal class TableNotFoundException : Exception {
        public TableNotFoundException() {
        }

        public TableNotFoundException(string? message) : base(message) {
        }

        public TableNotFoundException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}