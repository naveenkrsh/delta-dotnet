namespace DeltaLake.Operations.Utils {
    internal static class FileNamesUtil {
        public static string DeltaFile(string path, long version) {
            return string.Format("{0}/{1:D20}.json", path, version);
        }

        public static string ClassicCheckPointFile(string path, long version) {
            return string.Format("{0}/{1:D20}.checkpoint.parquet", path, version);
        }
    }
}