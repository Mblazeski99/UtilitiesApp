using System.IO;
using System.Windows.Input;
using UtilitiesApp.Services;

namespace UtilitiesApp.ViewModels
{
    public sealed class RenamerViewModel : ObservableObject
    {
        private readonly FileRenamer _fileRenamer;
        private readonly IDialogService _dialogService;
        private string _fileName = "";
        private string _extension = ".jpg";

        public RenamerViewModel(FileRenamer fileRenamer, IDialogService dialogService)
        {
            _fileRenamer = fileRenamer;
            _dialogService = dialogService;
            SelectFolderCommand = new RelayCommand(SelectFolder);
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                if (SetProperty(ref _fileName, value))
                {
                    NotifyExamples();
                }
            }
        }

        public string Extension
        {
            get => _extension;
            set
            {
                if (SetProperty(ref _extension, value))
                {
                    NotifyExamples();
                }
            }
        }

        public string Example1 => $"{FileName}-1{Extension}";

        public string Example2 => $"{FileName}-2{Extension}";

        public string Example3 => $"{FileName}-3{Extension}";

        public ICommand SelectFolderCommand { get; }

        private void SelectFolder()
        {
            if (FileRenamer.HasInvalidFileNameChars(FileName) || FileRenamer.HasInvalidFileNameChars(Extension))
            {
                _dialogService.ShowError("The file name or extension contains invalid characters.", "Error");
                return;
            }

            string? folderPath = _dialogService.SelectFolder();
            if (folderPath == null)
            {
                return;
            }

            try
            {
                RenameOutcome outcome = _fileRenamer.Rename(folderPath, FileName, Extension);
                if (outcome == RenameOutcome.NoFiles)
                {
                    _dialogService.ShowInformation("The selected folder does not contain any files.", "No files");
                    return;
                }

                _dialogService.ShowInformation("Files renamed successfully!", "Success");
            }
            catch (IOException ex)
            {
                _dialogService.ShowError(ex.Message, "Error");
            }
        }

        private void NotifyExamples()
        {
            OnPropertyChanged(nameof(Example1));
            OnPropertyChanged(nameof(Example2));
            OnPropertyChanged(nameof(Example3));
        }
    }
}
