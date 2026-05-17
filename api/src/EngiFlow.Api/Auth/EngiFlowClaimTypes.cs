namespace EngiFlow.Api.Auth;

/// <summary>
/// Defines the JWT claim names EngiFlow issues and consumes.
/// </summary>
internal static class EngiFlowClaimTypes
{
    /// <summary>
    /// Identifies the authenticated user.
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// Identifies the tenant company for tenant-scoped operations.
    /// </summary>
    public const string Tenant = "tenant";

    /// <summary>
    /// Identifies the user's role for role-based authorization.
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// Identifies the user's display name for client workspace personalization.
    /// </summary>
    public const string UserName = "user_name";

    /// <summary>
    /// Identifies the tenant company's display name for client workspace personalization.
    /// </summary>
    public const string CompanyName = "company_name";
}
