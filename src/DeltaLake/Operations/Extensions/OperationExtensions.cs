namespace DeltaLake.Operations.Extensions {
    internal static class OperationExtensions {
        // Method to get the string description for each operation
        public static string ToDeltaOperationString(this OperationEnum operation) {
            switch(operation) {
                case OperationEnum.CREATE_TABLE:
                    return "CREATE TABLE";
                case OperationEnum.WRITE:
                    return "WRITE";
                case OperationEnum.STREAMING_UPDATE:
                    return "STREAMING UPDATE";
                case OperationEnum.MANUAL_UPDATE:
                    return "MANUAL UPDATE";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}