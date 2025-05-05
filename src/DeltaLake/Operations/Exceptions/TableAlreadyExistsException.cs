namespace DeltaLake.Operations.Exceptions {
    public class TableAlreadyExistsException : Exception {
        public TableAlreadyExistsException() : base("A Delta Lake table already exists at that location.") {
        }

        public TableAlreadyExistsException(string? message) : base(message) {
        }
    }
}