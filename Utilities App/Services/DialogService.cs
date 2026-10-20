using Microsoft.Win32;
using System.Windows;

namespace UtilitiesApp.Services
{
    public sealed class DialogService : IDialogService
    {
        public string? SelectFolder()
        {
            var folderDialog = new OpenFolderDialog();
            return folderDialog.ShowDialog() == true ? folderDialog.FolderName : null;
        }

        public void ShowInformation(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
