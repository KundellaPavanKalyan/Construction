namespace EmployeePayroll.Api;

public static class YearPolicy
{
    public const int MinPayrollYear = 2000;

    public static int MaxPayrollYear => DateTime.UtcNow.Year;

    public static bool IsValidPayrollYear(int year) => year >= MinPayrollYear && year <= MaxPayrollYear;
}
