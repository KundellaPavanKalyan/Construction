using System.ComponentModel.DataAnnotations;

namespace EmployeePayroll.Api.Contracts;

public record EmployeeResponse(
    int EmployeeId,
    string EmployeeName,
    string? Qualification,
    string? Role,
    decimal DailyWage);

public class CreateEmployeeRequest
{
    [Required]
    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Qualification { get; set; }

    [MaxLength(150)]
    public string? Role { get; set; }

    public decimal DailyWage { get; set; }
}

public class UpdateEmployeeRequest : CreateEmployeeRequest;
