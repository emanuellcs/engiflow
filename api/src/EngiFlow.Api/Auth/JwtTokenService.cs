using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EngiFlow.Api.Auth;

/// <summary>
/// Issues signed JWT bearer tokens for authenticated EngiFlow users.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="options">The configured JWT settings.</param>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }

    /// <inheritdoc />
    public AccessTokenResult CreateAccessToken(User user, string companyName)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(EngiFlowClaimTypes.Subject, user.Id.Value.ToString()),
            new Claim(EngiFlowClaimTypes.Tenant, user.CompanyId.Value.ToString()),
            new Claim(EngiFlowClaimTypes.UserName, user.DisplayName),
            new Claim(EngiFlowClaimTypes.CompanyName, companyName),
            new Claim(EngiFlowClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
