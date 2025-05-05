using Parquet.Data;
using Parquet.Schema;

namespace DeltaLake.Operations.Utils {
    internal record RowGroupStatistics {

        public RowGroupStatistics(long rowCount, Dictionary<DataField, DataColumnStatistics> columnStatistics) {
            RowCount = rowCount;
            ColumnStatistics = columnStatistics;
        }

        public long RowCount { get; init; }
        public Dictionary<DataField, DataColumnStatistics> ColumnStatistics { get; init; }

    }
}