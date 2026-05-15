namespace EngiFlow.Api.Auth;

/// <summary>
/// Contains named authorization policies used by EngiFlow HTTP endpoints.
/// </summary>
internal static class EngiFlowAuthorizationPolicies
{
    /// <summary>
    /// Allows users who can create or submit engineering change orders.
    /// </summary>
    public const string EcoAuthoring = "EcoAuthoring";

    /// <summary>
    /// Allows users who can approve or reject engineering change orders.
    /// </summary>
    public const string EcoApproval = "EcoApproval";
}
