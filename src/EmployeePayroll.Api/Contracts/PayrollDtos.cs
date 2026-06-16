using System.ComponentModel.DataAnnotations;

namespace EmployeePayroll.Api.Contracts;

public record PayrollRowResponse(
    int PayrollId,
    int EmployeeId,
    string EmployeeName,
    string? Qualification,
    string? Role,
    decimal DailyWage,
    int DaysPresent,
    decimal MonthlySalary,
    decimal OtHours,
    decimal OtAmount,
    decimal AdvanceAmount,
    decimal TotalAmount);

public class PagedResult<T>(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class UpdatePayrollRequest
{
    [Range(0, 31)]
    public int DaysPresent { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OtHours { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AdvanceAmount { get; set; }
}

public class CreatePayrollMonthRequest
{
    [Range(1, 12)]
    public byte Month { get; set; }

    [Range(2000, 9999)]
    public int Year { get; set; }
}
