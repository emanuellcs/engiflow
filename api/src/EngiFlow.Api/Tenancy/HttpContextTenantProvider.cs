using EngiFlow.Api.Auth;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Api.Tenancy;

/// <summary>
/// Resolves the current tenant and actor identifiers from the authenticated HTTP user's JWT claims.
/// </summary>
/// <remarks>
/// This provider is the request-time bridge between ASP.NET Core authentication and the
/// Application layer's tenant context contract. It does not validate tokens itself; it
/// consumes claims after the JWT bearer middleware has authenticated the request.
/// </remarks>
public sealed class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextTenantProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public CompanyId CurrentCompanyId => CompanyId.From(ReadGuidClaim(EngiFlowClaimTypes.Tenant));

    /// <inheritdoc />
    public UserId CurrentUserId => UserId.From(ReadGuidClaim(EngiFlowClaimTypes.Subject));

    private Guid ReadGuidClaim(string claimType)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("A valid bearer token is required.");
        }

        var claimValue = principal.FindFirst(claimType)?.Value;
        if (!Guid.TryParse(claimValue, out var value) || value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                $"The authenticated principal is missing a valid '{claimType}' claim.");
        }

        return value;
    }
}
