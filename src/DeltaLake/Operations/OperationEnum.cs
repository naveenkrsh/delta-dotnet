namespace DeltaLake.Operations {
    public enum OperationEnum {
        // Recorded when the table is created.
        CREATE_TABLE,

        // Recorded during batch inserts.
        WRITE,

        // Recorded during streaming inserts.
        STREAMING_UPDATE,

        // For any operation that doesn't fit the above categories.
        MANUAL_UPDATE,

    }
}