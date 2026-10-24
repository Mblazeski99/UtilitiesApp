using System.Text.RegularExpressions;

namespace UtilitiesApp.Services
{
    public static class YouTubeLinks
    {
        private static readonly Regex UrlPattern = new(
            @"https?://[^\s<>""']+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IReadOnlyList<string> Extract(string text)
        {
            var links = new List<string>();
            foreach (Match match in UrlPattern.Matches(text))
            {
                string candidate = match.Value.TrimEnd('.', ',', ';', ')', ']', '>', '"', '\'');
                if (!IsVideoUrl(candidate))
                {
                    continue;
                }

                if (links.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                links.Add(candidate);
            }

            return links;
        }

        public static bool ContainsPlaylist(string text)
        {
            foreach (Match match in UrlPattern.Matches(text))
            {
                string candidate = match.Value.TrimEnd('.', ',', ';', ')', ']', '>', '"', '\'');
                if (IsYouTubeUrl(candidate) && !IsVideoUrl(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        public static string? TryGetVideoId(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            if (IsShortHost(uri))
            {
                return NormalizeId(uri.AbsolutePath.Trim('/'));
            }

            string? videoId = GetQueryValue(uri, "v");
            if (videoId != null)
            {
                return videoId;
            }

            string path = uri.AbsolutePath;
            foreach (string marker in new[] { "/shorts/", "/embed/", "/live/", "/v/" })
            {
                int index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    continue;
                }

                return NormalizeId(path[(index + marker.Length)..]);
            }

            return null;
        }

        public static bool IsVideoUrl(string value)
        {
            return IsYouTubeUrl(value) && TryGetVideoId(value) != null;
        }

        private static bool IsYouTubeUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            string host = uri.IdnHost;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                host = host[4..];
            }

            return host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
                || host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase)
                || host.Equals("youtube-nocookie.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".youtube-nocookie.com", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsShortHost(Uri uri)
        {
            string host = uri.IdnHost;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                host = host[4..];
            }

            return host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetQueryValue(Uri uri, string name)
        {
            string query = uri.Query;
            if (query.StartsWith('?'))
            {
                query = query[1..];
            }

            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = pair.IndexOf('=');
                string key = separator >= 0 ? pair[..separator] : pair;
                if (!key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string raw = separator >= 0 ? pair[(separator + 1)..] : "";
                return NormalizeId(Uri.UnescapeDataString(raw));
            }

            return null;
        }

        private static string? NormalizeId(string value)
        {
            int end = value.IndexOfAny(['/', '?', '&']);
            if (end >= 0)
            {
                value = value[..end];
            }

            if (value.Length != 11)
            {
                return null;
            }

            foreach (char character in value)
            {
                if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
                {
                    return null;
                }
            }

            return value;
        }
    }
}
