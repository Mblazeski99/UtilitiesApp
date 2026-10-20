using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace UtilitiesApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateExamples();
        }


        private void ExtensionTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateExamples();
        }

        private void UpdateExamples()
        {
            string fileName = FileNameTextBox.Text;
            string extension = ExtensionTextBox.Text;

            if (Examples1 != null && Examples2 != null && Examples3 != null)
            {
                Examples1.Content = $"{fileName}-1{extension}";
                Examples2.Content = $"{fileName}-2{extension}";
                Examples3.Content = $"{fileName}-3{extension}";
            }
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();
            if (folderDialog.ShowDialog() != true)
            {
                return;
            }

            string folderPath = folderDialog.FolderName;
            string newFileName = FileNameTextBox.Text;
            string extension = ExtensionTextBox.Text;

            if (HasInvalidFileNameChars(newFileName) || HasInvalidFileNameChars(extension))
            {
                MessageBox.Show(
                    "The file name or extension contains invalid characters.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            string[] files = Directory.GetFiles(folderPath);
            if (files.Length == 0)
            {
                MessageBox.Show("The selected folder does not contain any files.", "No files", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
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
                MessageBox.Show("Files renamed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool HasInvalidFileNameChars(string value)
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