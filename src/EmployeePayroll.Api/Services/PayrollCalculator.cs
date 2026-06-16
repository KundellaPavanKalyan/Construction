namespace EmployeePayroll.Api.Services;

public static class PayrollCalculator
{
    /// <summary>
    /// Monthly Salary = Daily Wage × Days Present; Hourly = Daily Wage ÷ 8; OT = Hourly × OT Hours; Total = Monthly + OT − Advance.
    /// </summary>
    public static void Apply(decimal dailyWage, int daysPresent, decimal otHours, decimal advanceAmount,
        out decimal monthlySalary, out decimal otAmount, out decimal totalAmount)
    {
        monthlySalary = Math.Round(dailyWage * daysPresent, 2, MidpointRounding.AwayFromZero);
        var hourlyWage = dailyWage / 8m;
        otAmount = Math.Round(hourlyWage * otHours, 2, MidpointRounding.AwayFromZero);
        totalAmount = Math.Round(monthlySalary + otAmount - advanceAmount, 2, MidpointRounding.AwayFromZero);
    }
}
