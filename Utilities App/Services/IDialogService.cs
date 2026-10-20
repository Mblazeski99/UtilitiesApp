namespace UtilitiesApp.Services
{
    public interface IDialogService
    {
        string? SelectFolder();

        void ShowInformation(string message, string title);

        void ShowError(string message, string title);
    }
}
