using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Web;

namespace edp_gui_app;

public sealed class LocalDocumentRequestServer : IDisposable
{
    private readonly ConcurrentDictionary<string, DocumentRequestState> _requests = new();
    private readonly DocumentUploadStorage _storage;
    private readonly Func<int, string, string, CancellationToken, Task> _recordUploadAsync;
    private readonly HttpListener _listener = new();
    private readonly int _port;
    private CancellationTokenSource? _stopSignal;
    private Task? _serverTask;

    public LocalDocumentRequestServer(
        DocumentUploadStorage storage,
        Func<int, string, string, CancellationToken, Task> recordUploadAsync,
        int port = 5087)
    {
        _storage = storage;
        _recordUploadAsync = recordUploadAsync;
        _port = port;
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    public string RegisterRequest(int tenantId, string tenantName, IReadOnlyList<string> requestedDocuments)
    {
        var token = Guid.NewGuid().ToString("N");
        _requests[token] = new DocumentRequestState(token, tenantId, tenantName, requestedDocuments);
        return $"http://localhost:{_port}/request/{token}";
    }

    public void EnsureStarted()
    {
        if (_listener.IsListening)
        {
            return;
        }

        _stopSignal = new CancellationTokenSource();
        _listener.Start();
        _serverTask = Task.Run(() => ListenAsync(_stopSignal.Token));
    }

    public static string BuildRequestPageHtml(DocumentRequestState request)
    {
        var fields = string.Join(Environment.NewLine, request.RequestedDocuments.Select((document, index) => $"""
            <label>
                <span>{HtmlEncode(document)}</span>
                <input type="file" name="file{index}" required data-document="{HtmlEncode(document)}">
            </label>
            """));

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Document Request</title>
                <style>
                    body { font-family: Segoe UI, Arial, sans-serif; margin: 0; background: #f6f7f9; color: #20242a; }
                    main { max-width: 680px; margin: 40px auto; background: #fff; padding: 28px; border: 1px solid #d9dee7; }
                    h1 { margin-top: 0; font-size: 24px; }
                    label { display: block; margin: 16px 0; }
                    label span { display: block; font-weight: 600; margin-bottom: 6px; }
                    input { width: 100%; }
                    button { margin-top: 12px; padding: 10px 16px; font-weight: 600; }
                    .status { margin-top: 14px; color: #245d30; }
                </style>
            </head>
            <body>
                <main>
                    <h1>Document Request</h1>
                    <p>{{HtmlEncode(request.TenantName)}}, upload the requested files below.</p>
                    <form method="post" action="/upload/{{HtmlEncode(request.Token)}}" enctype="multipart/form-data">
                        {{fields}}
                        <button type="submit">Submit Documents</button>
                    </form>
                    <p class="status" id="status"></p>
                </main>
                <script>
                    document.querySelector('form').addEventListener('submit', () => {
                        document.getElementById('status').textContent = 'Uploading files...';
                    });
                </script>
            </body>
            </html>
            """;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var path = context.Request.Url?.AbsolutePath ?? string.Empty;
        if (context.Request.HttpMethod == "GET" && path.StartsWith("/request/", StringComparison.OrdinalIgnoreCase))
        {
            await ServeRequestPageAsync(context, path["/request/".Length..], cancellationToken);
            return;
        }

        if (context.Request.HttpMethod == "POST" && path.StartsWith("/upload/", StringComparison.OrdinalIgnoreCase))
        {
            await HandleUploadAsync(context, path["/upload/".Length..], cancellationToken);
            return;
        }

        await WriteTextAsync(context.Response, "Not found.", "text/plain", HttpStatusCode.NotFound, cancellationToken);
    }

    private Task ServeRequestPageAsync(
        HttpListenerContext context,
        string token,
        CancellationToken cancellationToken)
    {
        return !_requests.TryGetValue(token, out var request)
            ? WriteTextAsync(context.Response, "Request link was not found.", "text/plain", HttpStatusCode.NotFound, cancellationToken)
            : WriteTextAsync(context.Response, BuildRequestPageHtml(request), "text/html; charset=utf-8", HttpStatusCode.OK, cancellationToken);
    }

    private async Task HandleUploadAsync(
        HttpListenerContext context,
        string token,
        CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(token, out var request))
        {
            await WriteTextAsync(context.Response, "Request link was not found.", "text/plain", HttpStatusCode.NotFound, cancellationToken);
            return;
        }

        var boundary = GetBoundary(context.Request.ContentType);
        if (string.IsNullOrWhiteSpace(boundary))
        {
            await WriteTextAsync(context.Response, "Upload request was invalid.", "text/plain", HttpStatusCode.BadRequest, cancellationToken);
            return;
        }

        await using var memory = new MemoryStream();
        await context.Request.InputStream.CopyToAsync(memory, cancellationToken);
        var uploads = MultipartUploadParser.Parse(memory.ToArray(), boundary);

        foreach (var upload in uploads.Where(upload => upload.Content.Length > 0))
        {
            await using var content = new MemoryStream(upload.Content);
            var path = await _storage.SaveAsync(request.TenantId, token, upload.FileName, content, cancellationToken);
            var documentName = GetRequestedDocumentName(request, upload.FileIndex);
            await _recordUploadAsync(request.TenantId, documentName, path, cancellationToken);
        }

        await WriteTextAsync(context.Response, "Documents uploaded successfully.", "text/plain", HttpStatusCode.OK, cancellationToken);
    }

    private static string? GetBoundary(string? contentType)
    {
        return contentType?.Split(';')
            .Select(part => part.Trim())
            .FirstOrDefault(part => part.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))?
            ["boundary=".Length..]
            .Trim('"');
    }

    private static string GetRequestedDocumentName(DocumentRequestState request, int fileIndex)
    {
        return fileIndex >= 0 && fileIndex < request.RequestedDocuments.Count
            ? request.RequestedDocuments[fileIndex]
            : "Document";
    }

    private static async Task WriteTextAsync(
        HttpListenerResponse response,
        string content,
        string contentType,
        HttpStatusCode statusCode,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        response.StatusCode = (int)statusCode;
        response.ContentType = contentType;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
        response.Close();
    }

    private static string HtmlEncode(string value) => HttpUtility.HtmlEncode(value);

    public void Dispose()
    {
        _stopSignal?.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _stopSignal?.Dispose();
        _serverTask?.Dispose();
    }

}
