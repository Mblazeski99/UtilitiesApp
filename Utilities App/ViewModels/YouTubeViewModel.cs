using System.Collections.ObjectModel;
using System.Windows.Input;
using UtilitiesApp.Services;
using YoutubeDLSharp;

namespace UtilitiesApp.ViewModels
{
    public sealed class YouTubeViewModel : ObservableObject
    {
        private readonly YouTubeDownloader _downloader;
        private readonly IDialogService _dialogService;
        private readonly ObservableCollection<YouTubeLinkItem> _links = [];
        private string _linkText = "";
        private YouTubeLinkItem? _selectedLink;
        private string _status = "Add a few songs, then convert them together.";
        private bool _isBusy;

        public YouTubeViewModel(YouTubeDownloader downloader, IDialogService dialogService)
        {
            _downloader = downloader;
            _dialogService = dialogService;
            _links.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasLinks));
                OnPropertyChanged(nameof(LinkCountText));
                CommandManager.InvalidateRequerySuggested();
            };
            AddCommand = new RelayCommand(_ => Add(), _ => !IsBusy);
            RemoveCommand = new RelayCommand(_ => Remove(), _ => !IsBusy && SelectedLink != null);
            ConvertCommand = new RelayCommand(_ => Convert(), _ => !IsBusy && _links.Count > 0);
        }

        public string LinkText
        {
            get => _linkText;
            set => SetProperty(ref _linkText, value);
        }

        public YouTubeLinkItem? SelectedLink
        {
            get => _selectedLink;
            set
            {
                if (SetProperty(ref _selectedLink, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public IReadOnlyList<YouTubeLinkItem> Links => _links;

        public bool HasLinks => _links.Count > 0;

        public string LinkCountText => _links.Count == 1 ? "1 link" : $"{_links.Count} links";

        public ICommand AddCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand ConvertCommand { get; }

        private void Add()
        {
            string text = LinkText.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _dialogService.ShowError("Paste a YouTube link first.", "YouTube to MP3");
                return;
            }

            IReadOnlyList<string> links = YouTubeLinks.Extract(text);
            bool skippedPlaylist = YouTubeLinks.ContainsPlaylist(text);
            if (links.Count == 0)
            {
                string message = skippedPlaylist
                    ? "Paste a video link. Playlists are not supported."
                    : "That text does not contain a YouTube link.";
                _dialogService.ShowError(message, "YouTube to MP3");
                return;
            }

            int added = 0;
            foreach (string link in links)
            {
                string? videoId = YouTubeLinks.TryGetVideoId(link);
                bool duplicate = _links.Any(item =>
                    string.Equals(YouTubeLinks.TryGetVideoId(item.Url), videoId, StringComparison.Ordinal));
                if (duplicate)
                {
                    continue;
                }

                _links.Add(new YouTubeLinkItem(link));
                added++;
            }

            if (added > 0)
            {
                LinkText = "";
            }

            Status = DescribeAdded(added, skippedPlaylist);

            CommandManager.InvalidateRequerySuggested();
        }

        private void Remove()
        {
            if (SelectedLink == null)
            {
                return;
            }

            YouTubeLinkItem selected = SelectedLink;
            int index = _links.IndexOf(selected);
            _links.Remove(selected);
            SelectedLink = _links.Count == 0
                ? null
                : _links[Math.Min(index, _links.Count - 1)];
            Status = "Removed 1 link.";
            CommandManager.InvalidateRequerySuggested();
        }

        private async void Convert()
        {
            if (IsBusy || _links.Count == 0)
            {
                return;
            }

            List<YouTubeLinkItem> pending = _links.Where(item => item.Status != "Done").ToList();
            if (pending.Count == 0)
            {
                _dialogService.ShowInformation("Those songs are already converted.", "YouTube to MP3");
                return;
            }

            string? folder = _dialogService.SelectFolder();
            if (folder == null)
            {
                return;
            }

            IsBusy = true;
            Status = "Preparing converter...";
            await Task.Yield();
            int succeeded = 0;
            int failed = 0;
            string? firstError = null;

            try
            {
                await _downloader.EnsureToolsAsync(
                    new Progress<string>(message => Status = message),
                    CancellationToken.None);

                for (int i = 0; i < pending.Count; i++)
                {
                    YouTubeLinkItem item = pending[i];
                    item.Status = "Downloading";
                    Status = $"Converting {i + 1} of {pending.Count}";

                    var progress = new Progress<DownloadProgress>(update =>
                    {
                        item.Status = Describe(update);
                    });

                    YouTubeDownloadResult result = await _downloader.DownloadMp3Async(
                        item.Url,
                        folder,
                        progress,
                        CancellationToken.None);

                    if (result.Success)
                    {
                        item.Status = "Done";
                        succeeded++;
                    }
                    else
                    {
                        item.Status = "Failed";
                        failed++;
                        firstError ??= result.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, "YouTube to MP3");
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (failed == 0)
            {
                Status = succeeded == 1 ? "Saved 1 MP3 file." : $"Saved {succeeded} MP3 files.";
                _dialogService.ShowInformation(
                    $"Saved {succeeded} MP3 file{(succeeded == 1 ? "" : "s")} to:{Environment.NewLine}{folder}",
                    "YouTube to MP3");
                return;
            }

            Status = $"Saved {succeeded}. {failed} failed.";
            string details = string.IsNullOrWhiteSpace(firstError) ? "" : Environment.NewLine + Environment.NewLine + firstError;
            _dialogService.ShowError(
                $"Saved {succeeded} file{(succeeded == 1 ? "" : "s")}. {failed} link{(failed == 1 ? "" : "s")} could not be converted.{details}",
                "YouTube to MP3");
        }

        private static string DescribeAdded(int added, bool skippedPlaylist)
        {
            if (added == 0)
            {
                return "Those links are already in the list.";
            }

            string links = added == 1 ? "Added 1 link." : $"Added {added} links.";
            return skippedPlaylist ? $"{links} Playlists were skipped." : links;
        }

        private static string Describe(DownloadProgress progress)
        {
            return progress.State switch
            {
                DownloadState.PreProcessing => "Starting",
                DownloadState.Downloading => progress.Progress > 0
                    ? $"Downloading {progress.Progress:P0}"
                    : "Downloading",
                DownloadState.PostProcessing => "Converting",
                DownloadState.Error => "Failed",
                _ => "Working"
            };
        }

    }
}
