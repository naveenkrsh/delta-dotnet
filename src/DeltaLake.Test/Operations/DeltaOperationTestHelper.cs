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
    }
}
