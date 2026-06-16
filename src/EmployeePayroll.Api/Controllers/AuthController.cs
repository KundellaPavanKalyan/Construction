using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeePayroll.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EmployeePayroll.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest body)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var expectedUser = configuration["Auth:Username"] ?? "admin";
        var expectedPass = configuration["Auth:Password"] ?? "admin";
        if (!string.Equals(body.Username.Trim(), expectedUser, StringComparison.Ordinal) ||
            body.Password != expectedPass)
            return Unauthorized(new { message = "Invalid username or password." });

        var key = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key) || key.Length < 32)
            key = "ConstructionPayrollDevSigningKey_32chars!";

        var issuer = configuration["Jwt:Issuer"] ?? "EmployeePayroll";
        var audience = configuration["Jwt:Audience"] ?? "EmployeePayroll.Web";
        var hours = int.TryParse(configuration["Jwt:ExpireHours"], out var h) ? h : 12;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, body.Username.Trim()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(hours);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new LoginResponse(jwt, body.Username.Trim(), (int)(expires - DateTime.UtcNow).TotalSeconds));
    }
}
