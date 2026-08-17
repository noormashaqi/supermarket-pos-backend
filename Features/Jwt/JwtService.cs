using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SupermarketSystem.Api.Services.Jwt;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(Employee employee, List<string> permissions)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes)
            ? minutes
            : 480; // 8 ساعات كافتراضي (طول شفت عادي)

        // Claims الأساسية: هوية الموظف
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.Username),
            new(ClaimTypes.Role, employee.Role),
            new("fullName", employee.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // كل صلاحية بتنحط كـ Claim منفصل بنفس النوع "permission"
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        claims.AddRange(permissions.Select(p => new Claim("permissions", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string ComputeRefreshTokenHash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var expiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days)
            ? days
            : 14;

        return DateTime.UtcNow.AddDays(expiryDays);
    }
}
