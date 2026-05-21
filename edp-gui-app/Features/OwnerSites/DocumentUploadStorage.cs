namespace edp_gui_app;

public sealed class DocumentUploadStorage
{
    private readonly string _rootPath;

    public DocumentUploadStorage()
        : this(Path.Combine(AppContext.BaseDirectory, "DocumentRequests"))
    {
    }

    public DocumentUploadStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public string BuildUploadPath(int tenantId, string token, string fileName)
    {
        var safeFileName = SanitizeFileName(Path.GetFileName(fileName));
        return Path.Combine(_rootPath, tenantId.ToString(), token, safeFileName);
    }

    public async Task<string> SaveAsync(
        int tenantId,
        string token,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var path = BuildUploadPath(tenantId, token, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return path;
    }

    public static string SanitizeFileName(string fileName)
    {
        var safe = new string(fileName.Select(ToSafeFileNameChar).ToArray()).Trim('_', '.');
        return string.IsNullOrWhiteSpace(safe) ? $"upload_{Guid.NewGuid():N}" : safe;
    }

    private static char ToSafeFileNameChar(char value)
    {
        return Path.GetInvalidFileNameChars().Contains(value) || value is '/' or '\\' ? '_' : value;
    }
}
