namespace DeltaLake.Operations.Models {
    internal class CommitInfoModel {
        public long Timestamp { get; set; }

        public string ClientVersion { get; set; }
        public string Operation { get; set; }
        public Dictionary<string, string> OperationParameters { get; set; }
        public bool IsBlindAppend { get; set; }
        public string TxnId { get; set; }
        public Dictionary<string, string> OperationMetrics { get; set; }

        // Constructor
        public CommitInfoModel(
            long timestamp,
            string clientVersion,
            string operation,
            Dictionary<string, string> operationParameters,
            bool isBlindAppend,
            string txnId,
            Dictionary<string, string> operationMetrics) {
            Timestamp = timestamp;
            ClientVersion = clientVersion;
            Operation = operation;
            OperationParameters = operationParameters;
            IsBlindAppend = isBlindAppend;
            TxnId = txnId;
            OperationMetrics = operationMetrics;
        }
    }
}