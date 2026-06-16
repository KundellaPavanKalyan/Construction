namespace EmployeePayroll.Api.Services;

public sealed class InvoiceFileStorage(IConfiguration configuration, IWebHostEnvironment environment)
{
    private readonly string _folder = ResolveFolder(configuration, environment);

    public string RootPath => _folder;

    public async Task<(string StoredFileName, long Size)> SaveAsync(IFormFile file, string extension, CancellationToken ct)
    {
        Directory.CreateDirectory(_folder);
        var ext = extension.StartsWith('.') ? extension : $".{extension}";
        var stored = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var path = Path.Combine(_folder, stored);
        await using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(fs, ct);
        return (stored, file.Length);
    }

    public string? GetFullPath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) || storedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return null;
        var path = Path.Combine(_folder, storedFileName);
        return File.Exists(path) ? path : null;
    }

    public void TryDelete(string storedFileName)
    {
        var path = GetFullPath(storedFileName);
        if (path is not null)
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
    }

    private static string ResolveFolder(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["InvoiceUpload:StoragePath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured);
        }

        return Path.Combine(environment.ContentRootPath, "InvoiceUploads");
    }
}
