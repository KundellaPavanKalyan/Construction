using System.Globalization;
using System.Text.RegularExpressions;

namespace EmployeePayroll.Api.Services;

public static class InvoiceDateParser
{
    public static DateOnly? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();

        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
            return iso;

        var lowered = raw.ToLowerInvariant();
        var formats = new[]
        {
            "d-MMM-yyyy", "dd-MMM-yyyy", "d-MMMM-yyyy", "dd-MMMM-yyyy",
            "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy",
            "d.M.yyyy", "dd.MM.yyyy"
        };
        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(lowered, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose))
            return loose;

        var normalized = Regex.Replace(raw, @"\s+", "-", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(normalized, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }

        return null;
    }
}
