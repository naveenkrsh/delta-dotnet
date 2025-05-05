namespace DeltaLake.Test.Operations {
    internal class DeltaOperationTestHelper {

        public static void CopyDirectory(string sourceDir, string targetDir) {
            // Ensure the target directory exists
            Directory.CreateDirectory(targetDir);

            // Copy all files
            foreach(string file in Directory.GetFiles(sourceDir)) {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            // Recursively copy all subdirectories
            foreach(string subDir in Directory.GetDirectories(sourceDir)) {
                string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        public static void CopyDirectoryIfNotExists(string source, string targetPath) {
            if(!Directory.Exists(targetPath)) {
                Directory.CreateDirectory(targetPath);
            }

            if(!Directory.EnumerateFileSystemEntries(targetPath).Any()) {
                DeltaOperationTestHelper.CopyDirectory(
                    source,
                    targetPath
                );
            }
        }

        public static void DeleteDeltaLogFolder(string path) {
            if(Directory.Exists(Path.Combine(path, "_delta_log")))
                Directory.Delete(Path.Combine(path, "_delta_log"), recursive: true);
        }
    }
}
