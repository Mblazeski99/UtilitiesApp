using System.IO;

namespace UtilitiesApp.Services
{
    public enum RenameOutcome
    {
        Success,
        NoFiles
    }

    public sealed class FileRenamer
    {
        public RenameOutcome Rename(string folderPath, string newFileName, string extension)
        {
            string[] files = Directory.GetFiles(folderPath);
            if (files.Length == 0)
            {
                return RenameOutcome.NoFiles;
            }

            // Move everything aside first. A direct rename fails when a target name
            // is still used by a file that has not been renamed yet.
            string temporaryDirectory = Path.Combine(folderPath, $".utilities-app-{Guid.NewGuid():N}");
            var stagedFiles = new List<(string OriginalPath, string CurrentPath)>(files.Length);

            try
            {
                Directory.CreateDirectory(temporaryDirectory);

                foreach (string filePath in files)
                {
                    string temporaryPath = Path.Combine(temporaryDirectory, Path.GetFileName(filePath));
                    File.Move(filePath, temporaryPath);
                    stagedFiles.Add((filePath, temporaryPath));
                }

                for (int i = 0; i < stagedFiles.Count; i++)
                {
                    string finalPath = Path.Combine(folderPath, $"{newFileName}-{i + 1}{extension}");
                    File.Move(stagedFiles[i].CurrentPath, finalPath);
                    stagedFiles[i] = (stagedFiles[i].OriginalPath, finalPath);
                }

                Directory.Delete(temporaryDirectory);
                return RenameOutcome.Success;
            }
            catch (Exception ex)
            {
                string message = $"An error occurred: {ex.Message}";
                try
                {
                    RestoreOriginalNames(temporaryDirectory, stagedFiles);
                }
                catch (Exception restoreException)
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}Some files could not be restored: {restoreException.Message}";
                }

                throw new IOException(message, ex);
            }
        }

        public static bool HasInvalidFileNameChars(string value)
        {
            return value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }

        private static void RestoreOriginalNames(string temporaryDirectory, List<(string OriginalPath, string CurrentPath)> stagedFiles)
        {
            if (!Directory.Exists(temporaryDirectory))
            {
                Directory.CreateDirectory(temporaryDirectory);
            }

            var displacedFiles = new List<(string OriginalPath, string TemporaryPath)>();
            foreach (var stagedFile in stagedFiles)
            {
                if (!File.Exists(stagedFile.CurrentPath) || PathsEqual(stagedFile.CurrentPath, stagedFile.OriginalPath))
                {
                    continue;
                }

                string temporaryPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(stagedFile.CurrentPath)}");
                File.Move(stagedFile.CurrentPath, temporaryPath);
                displacedFiles.Add((stagedFile.OriginalPath, temporaryPath));
            }

            foreach (var displacedFile in displacedFiles)
            {
                File.Move(displacedFile.TemporaryPath, displacedFile.OriginalPath);
            }

            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory);
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
    }
}
