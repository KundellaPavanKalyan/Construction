using System.Text;
using System.Text.Json;
using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using EmployeePayroll.Api.Services;
using EmployeePayroll.Api.Services.Gemini;
using EmployeePayroll.Api.Services.Material;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var useSqlite = builder.Configuration.GetValue("UseSqlite", false);

builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = builder.Configuration.GetValue("InvoiceUpload:MaxFileSizeBytes", 10 * 1024 * 1024);
});
builder.Services.AddSingleton<InvoiceDocumentExtractor>();
builder.Services.AddSingleton<InvoiceFileStorage>();
builder.Services.AddSingleton<PdfConversionService>();
builder.Services.AddSingleton<InvoiceMediaPreparer>();
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient(nameof(GeminiService), c => c.Timeout = TimeSpan.FromMinutes(2));
builder.Services.AddSingleton<GeminiService>();
builder.Services.AddSingleton<GeminiInvoiceExtractionService>();
builder.Services.AddSingleton<InvoiceExcelExportService>();
builder.Services.AddSingleton<MaterialInvoiceExtractionService>();
builder.Services.AddScoped<MaterialCatalogService>();
builder.Services.AddScoped<MaterialGridService>();
builder.Services.AddSingleton<MaterialGridExcelExportService>();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
    jwtKey = "ConstructionPayrollDevSigningKey_32chars!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EmployeePayroll",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EmployeePayroll.Web",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

if (useSqlite)
{
    var relative = builder.Configuration["Sqlite:DatabasePath"] ?? "payroll_dev.db";
    var fullPath = Path.IsPathRooted(relative)
        ? relative
        : Path.Combine(builder.Environment.ContentRootPath, relative);
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={fullPath}"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServer")
                           ?? builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Configure ConnectionStrings:SqlServer (or Default) for SQL Server, or set UseSqlite to true. See appsettings.json.");

    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useSqlite)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();
    await SeedSampleDataIfEmptyAsync(db);
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task SeedSampleDataIfEmptyAsync(AppDbContext db)
{
    if (await db.Employees.AnyAsync()) return;

    var employees = new[]
    {
        new Employee { EmployeeName = "Ravi Kumar", Qualification = "ITI", Role = "Welder", DailyWage = 850 },
        new Employee { EmployeeName = "Ram Prasad", Qualification = "10th", Role = "Helper", DailyWage = 650 },
        new Employee { EmployeeName = "John Peter", Qualification = "Diploma", Role = "Supervisor", DailyWage = 1200 }
    };
    db.Employees.AddRange(employees);
    await db.SaveChangesAsync();

    const byte month = 5;
    const int year = 2026;
    foreach (var e in await db.Employees.ToListAsync())
    {
        var (days, ot, advance) = e.EmployeeName switch
        {
            "Ravi Kumar" => (24, 10m, 3000m),
            "Ram Prasad" => (26, 5m, 0m),
            "John Peter" => (25, 8m, 1000m),
            _ => (0, 0m, 0m)
        };

        var p = new Payroll
        {
            EmployeeId = e.EmployeeId,
            Month = month,
            Year = year,
            DaysPresent = days,
            OtHours = ot,
            AdvanceAmount = advance
        };
        PayrollCalculator.Apply(e.DailyWage, p.DaysPresent, p.OtHours, p.AdvanceAmount,
            out _, out var otAmt, out var total);
        p.OtAmount = otAmt;
        p.TotalAmount = total;
        db.Payrolls.Add(p);
    }

    await db.SaveChangesAsync();

    foreach (var e in await db.Employees.ToListAsync())
    {
        var (days, ot) = e.EmployeeName switch
        {
            "Ravi Kumar" => (24, 10m),
            "Ram Prasad" => (26, 5m),
            "John Peter" => (25, 8m),
            _ => (0, 0m)
        };
        var present = Enumerable.Range(1, days).ToDictionary(i => i.ToString(), _ => "P");
        var otJson = JsonSerializer.Serialize(new Dictionary<string, decimal> { { "28", ot } });
        db.Attendances.Add(new Attendance
        {
            EmployeeId = e.EmployeeId,
            Month = month,
            Year = year,
            PresentByDayJson = JsonSerializer.Serialize(present),
            OtByDayJson = otJson
        });
    }

    await db.SaveChangesAsync();
}
