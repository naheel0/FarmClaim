using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace FarmClaim.Infrastructure.JWT
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly string _key, _issuer, _audience;
        private readonly int _expireMinutes;

        public JwtService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            var j = _config.GetSection("Jwt");
            _key = j["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            _issuer = j["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            _audience = j["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
            _expireMinutes = int.Parse(j["ExpireMinutes"] ?? "60");
        }

        public int AccessTokenExpirationMinutes => _expireMinutes;

        public string GenerateAccessToken(User user)
        {
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.ToString()),
                new System.Security.Claims.Claim("FirstName", user.FirstName),
                new System.Security.Claims.Claim("LastName", user.LastName),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(_issuer, _audience, claims,
                expires: DateTime.UtcNow.AddMinutes(_expireMinutes), signingCredentials: creds));
        }

        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var params_ = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, params_, out SecurityToken validated);
                if (validated is not JwtSecurityToken jwt || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256)) return null;
                return principal;
            }
            catch { return null; }
        }
    }
}