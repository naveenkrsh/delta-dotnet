namespace DeltaLake.Util {
    public static class FileNames {
        public static string DeltaFile(string path, long version) {
            return string.Format("{0}/{1:D20}.json", path, version);
        }
    }
}