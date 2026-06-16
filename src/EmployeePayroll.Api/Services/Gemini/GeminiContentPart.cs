namespace EmployeePayroll.Api.Services.Gemini;

public sealed class GeminiContentPart
{
    public string? Text { get; init; }

    public string? MimeType { get; init; }

    public byte[]? BinaryData { get; init; }

    public bool IsInlineData => BinaryData is { Length: > 0 } && !string.IsNullOrEmpty(MimeType);
}
