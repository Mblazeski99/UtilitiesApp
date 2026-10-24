namespace UtilitiesApp.ViewModels
{
    public sealed class YouTubeLinkItem : ObservableObject
    {
        private string _status = "Ready";

        public YouTubeLinkItem(string url)
        {
            Url = url;
        }

        public string Url { get; }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }
}
