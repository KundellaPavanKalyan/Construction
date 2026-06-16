using System.Globalization;
using System.Text.RegularExpressions;

namespace EmployeePayroll.Api.Services.Material;

public static class MaterialLineTextParser
{
    private static readonly Regex LineWithQtyRx = new(
        @"^(.{2,80}?)\s+(\d+(?:\.\d{1,3})?)\s*(?:nos|bags|bag|kg|kgs|ton|tons|mt|pcs|pc|units|unit|ltr|litre|l)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex QtyThenNameRx = new(
        @"^(\d+(?:\.\d{1,3})?)\s+(.{2,80})$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public static IReadOnlyList<MaterialLineItem> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<MaterialLineItem>();

        var results = new List<MaterialLineItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length < 3) continue;
            if (IsSkipLine(line)) continue;

            var matchedNameThenQty = true;
            Match? m = LineWithQtyRx.Match(line);
            if (!m.Success)
            {
                matchedNameThenQty = false;
                m = QtyThenNameRx.Match(line);
            }
            if (!m.Success) continue;

            string name;
            decimal qty;
            if (matchedNameThenQty)
            {
                name = m.Groups[1].Value.Trim();
                if (!decimal.TryParse(m.Groups[2].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out qty))
                    continue;
            }
            else
            {
                if (!decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out qty))
                    continue;
                name = m.Groups[2].Value.Trim();
            }

            name = CleanMaterialName(name);
            if (name.Length < 2 || qty <= 0) continue;
            if (!seen.Add(name)) continue;

            results.Add(new MaterialLineItem { MaterialName = name, Quantity = qty });
        }

        return results;
    }

    private static bool IsSkipLine(string line)
    {
        if (Regex.IsMatch(line, @"(?i)^(total|sub\s*total|grand|cgst|sgst|igst|gst|invoice|bill|amount|rupees|rs\.|tax|transport|round)"))
            return true;
        if (Regex.IsMatch(line, @"(?i)@(gmail|yahoo|\.com)|\d{10}"))
            return true;
        return false;
    }

    private static string CleanMaterialName(string name)
    {
        name = Regex.Replace(name, @"\s+", " ").Trim();
        name = Regex.Replace(name, @"^[\d\.\-\s]+", "").Trim();
        return name;
    }
}
