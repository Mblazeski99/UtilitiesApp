using System.IO;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace UtilitiesApp.Services
{
    public sealed class YouTubeDownloadResult
    {
        private YouTubeDownloadResult(bool success, string? filePath, string? error)
        {
            Success = success;
            FilePath = filePath;
            Error = error;
        }

        public bool Success { get; }

        public string? FilePath { get; }

        public string? Error { get; }

        public static YouTubeDownloadResult Succeeded(string? filePath) => new(true, filePath, null);

        public static YouTubeDownloadResult Failed(string error) => new(false, null, error);
    }

    public sealed class YouTubeDownloader
    {
        private readonly SemaphoreSlim _setupLock = new(1, 1);
        private readonly string _toolsDirectory;

        public YouTubeDownloader()
        {
            _toolsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UtilitiesApp",
                "tools");
        }

        public async Task EnsureToolsAsync(IProgress<string>? status, CancellationToken cancellationToken)
        {
            await _setupLock.WaitAsync(cancellationToken);
            try
            {
                Directory.CreateDirectory(_toolsDirectory);
                AddToolsToPath();
                if (ToolsExist())
                {
                    return;
                }

                status?.Report("Downloading converter. This only happens once.");
                Utils.TimeoutSeconds = 60 * 15;
                await Utils.DownloadBinaries(skipExisting: true, directoryPath: _toolsDirectory, downloadJSRuntime: true);
                if (!ToolsExist())
                {
                    throw new InvalidOperationException("The converter files could not be downloaded.");
                }

                AddToolsToPath();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not prepare the converter. Check your internet connection and try again."
                    + Environment.NewLine
                    + ex.Message,
                    ex);
            }
            finally
            {
                _setupLock.Release();
            }
        }

        public async Task<YouTubeDownloadResult> DownloadMp3Async(
            string url,
            string outputFolder,
            IProgress<DownloadProgress>? progress,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(outputFolder);
            AddToolsToPath();

            var downloader = new YoutubeDL
            {
                YoutubeDLPath = YtDlpPath,
                FFmpegPath = FfmpegPath,
                OutputFolder = outputFolder,
                OutputFileTemplate = "%(title)s.%(ext)s",
                OverwriteFiles = true,
                IgnoreDownloadErrors = false
            };

            try
            {
                RunResult<string> result = await downloader.RunAudioDownload(
                    url,
                    AudioConversionFormat.Mp3,
                    cancellationToken,
                    progress,
                    overrideOptions: new OptionSet
                    {
                        AudioQuality = 0,
                        WindowsFilenames = true,
                        EmbedMetadata = true,
                        JsRuntimes = $"deno:{DenoPath}"
                    });

                if (!result.Success)
                {
                    return YouTubeDownloadResult.Failed(Summarize(result.ErrorOutput));
                }

                return YouTubeDownloadResult.Succeeded(result.Data);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return YouTubeDownloadResult.Failed(ex.Message);
            }
        }

        private string YtDlpPath => Path.Combine(_toolsDirectory, "yt-dlp.exe");

        private string FfmpegPath => Path.Combine(_toolsDirectory, "ffmpeg.exe");

        private string DenoPath => Path.Combine(_toolsDirectory, "deno.exe");

        private bool ToolsExist()
        {
            return File.Exists(YtDlpPath) && File.Exists(FfmpegPath) && File.Exists(DenoPath);
        }

        private void AddToolsToPath()
        {
            string current = Environment.GetEnvironmentVariable("PATH") ?? "";
            bool alreadyPresent = current
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Any(entry => string.Equals(
                    entry.Trim().TrimEnd('\\'),
                    _toolsDirectory.TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase));

            if (alreadyPresent)
            {
                return;
            }

            Environment.SetEnvironmentVariable("PATH", _toolsDirectory + Path.PathSeparator + current);
        }

        private static string Summarize(IEnumerable<string>? lines)
        {
            if (lines == null)
            {
                return "The download failed.";
            }

            string message = string.Join(
                Environment.NewLine,
                lines.Where(line => !string.IsNullOrWhiteSpace(line)).TakeLast(8));

            return string.IsNullOrWhiteSpace(message) ? "The download failed." : message;
        }
    }
}
