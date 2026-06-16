using System.Globalization;
using System.Text.Json;
using EmployeePayroll.Api.Models;

namespace EmployeePayroll.Api.Services;

public static class AttendancePayrollSync
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static int CountPresentDays(string? presentByDayJson)
    {
        var json = string.IsNullOrWhiteSpace(presentByDayJson) ? "{}" : presentByDayJson;
        using var doc = JsonDocument.Parse(json);
        var n = 0;
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!IsValidDayKey(prop.Name)) continue;
            var v = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : null;
            if (string.Equals(v, "P", StringComparison.OrdinalIgnoreCase))
                n++;
        }

        return n;
    }

    public static decimal SumOtHoursFromDailyJson(string? otByDayJson)
    {
        var json = string.IsNullOrWhiteSpace(otByDayJson) ? "{}" : otByDayJson;
        using var doc = JsonDocument.Parse(json);
        decimal sum = 0;
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!IsValidDayKey(prop.Name)) continue;
            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.Number:
                    sum += prop.Value.GetDecimal();
                    break;
                case JsonValueKind.String when decimal.TryParse(prop.Value.GetString(), NumberStyles.Any,
                             CultureInfo.InvariantCulture, out var d):
                    sum += d;
                    break;
            }
        }

        return sum;
    }

    public static void ApplyToPayroll(Employee employee, Payroll payroll, Attendance attendance)
    {
        var days = CountPresentDays(attendance.PresentByDayJson);
        if (days > 31) days = 31;
        var otHours = SumOtHoursFromDailyJson(attendance.OtByDayJson);
        if (otHours < 0) otHours = 0;

        payroll.DaysPresent = days;
        payroll.OtHours = otHours;
        PayrollCalculator.Apply(employee.DailyWage, payroll.DaysPresent, payroll.OtHours, payroll.AdvanceAmount,
            out _, out var otAmt, out var total);
        payroll.OtAmount = otAmt;
        payroll.TotalAmount = total;
    }

    public static string NormalizePresentJson(string? json)
    {
        var dict = DeserializeStringDict(json);
        var clean = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in dict)
        {
            if (!IsValidDayKey(k)) continue;
            if (string.IsNullOrWhiteSpace(v)) continue;
            var mark = v.Trim().ToUpperInvariant()[0] switch
            {
                'P' => "P",
                'A' => "A",
                _ => (string?)null
            };
            if (mark is not null)
                clean[k.Trim()] = mark;
        }

        return JsonSerializer.Serialize(clean, JsonOptions);
    }

    public static string NormalizeOtJson(string? json)
    {
        var raw = DeserializeDecimalDict(json);
        var clean = new Dictionary<string, decimal>();
        foreach (var (k, v) in raw)
        {
            if (!IsValidDayKey(k)) continue;
            if (v < 0) continue;
            clean[k.Trim()] = Math.Round(v, 2, MidpointRounding.AwayFromZero);
        }

        return JsonSerializer.Serialize(clean, JsonOptions);
    }

    private static bool IsValidDayKey(string key)
    {
        if (!int.TryParse(key.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
            return false;
        return d is >= 1 and <= 31;
    }

    private static Dictionary<string, string> DeserializeStringDict(string? json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(string.IsNullOrWhiteSpace(json) ? "{}"
                       : json, JsonOptions)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static Dictionary<string, decimal> DeserializeDecimalDict(string? json)
    {
        try
        {
            var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                decimal val = prop.Value.ValueKind switch
                {
                    JsonValueKind.Number => prop.Value.GetDecimal(),
                    JsonValueKind.String when decimal.TryParse(prop.Value.GetString(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var d) => d,
                    _ => 0
                };
                result[prop.Name] = val;
            }

            return result;
        }
        catch
        {
            return new Dictionary<string, decimal>();
        }
    }
}
