using System.Text;
using System.Text.RegularExpressions;

namespace edp_gui_app;

public static class MultipartUploadParser
{
    private static readonly byte[] HeaderSeparator = "\r\n\r\n"u8.ToArray();
    private static readonly byte[] LineBreak = "\r\n"u8.ToArray();

    public static IReadOnlyList<MultipartUpload> Parse(byte[] body, string boundary)
    {
        var boundaryBytes = Encoding.UTF8.GetBytes($"--{boundary}");
        var uploads = new List<MultipartUpload>();
        var position = 0;

        while (true)
        {
            var boundaryStart = IndexOf(body, boundaryBytes, position);
            if (boundaryStart < 0)
            {
                break;
            }

            var partStart = boundaryStart + boundaryBytes.Length;
            if (StartsWith(body, partStart, "--"u8.ToArray()))
            {
                break;
            }

            if (StartsWith(body, partStart, LineBreak))
            {
                partStart += LineBreak.Length;
            }

            var nextBoundary = IndexOf(body, boundaryBytes, partStart);
            if (nextBoundary < 0)
            {
                break;
            }

            var headerEnd = IndexOf(body, HeaderSeparator, partStart);
            if (headerEnd < 0 || headerEnd > nextBoundary)
            {
                position = nextBoundary;
                continue;
            }

            var headers = Encoding.UTF8.GetString(body, partStart, headerEnd - partStart);
            if (!headers.Contains("filename=", StringComparison.OrdinalIgnoreCase))
            {
                position = nextBoundary;
                continue;
            }

            var contentStart = headerEnd + HeaderSeparator.Length;
            var contentEnd = nextBoundary;
            if (contentEnd >= LineBreak.Length && StartsWith(body, contentEnd - LineBreak.Length, LineBreak))
            {
                contentEnd -= LineBreak.Length;
            }

            uploads.Add(new MultipartUpload(
                MatchFileIndex(headers),
                MatchHeaderValue(headers, "filename"),
                body[contentStart..contentEnd]));

            position = nextBoundary;
        }

        return uploads;
    }

    private static int MatchFileIndex(string headers)
    {
        var match = Regex.Match(headers, "name=\"file(\\d+)\"", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var index) ? index : -1;
    }

    private static string MatchHeaderValue(string headers, string key)
    {
        var match = Regex.Match(headers, $"{key}=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static int IndexOf(byte[] source, byte[] pattern, int start)
    {
        for (var i = start; i <= source.Length - pattern.Length; i++)
        {
            if (StartsWith(source, i, pattern))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool StartsWith(byte[] source, int start, byte[] pattern)
    {
        if (start < 0 || start + pattern.Length > source.Length)
        {
            return false;
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            if (source[start + i] != pattern[i])
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record MultipartUpload(int FileIndex, string FileName, byte[] Content);
