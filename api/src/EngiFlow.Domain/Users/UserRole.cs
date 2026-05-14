namespace EngiFlow.Domain.Users;

/// <summary>
/// Describes the broad responsibility a user can perform in the ECO workflow.
/// </summary>
/// <remarks>
/// The enum is intentionally small in the domain foundation. Application-layer RBAC can
/// map these roles to policies and permissions without diluting the core business model.
/// </remarks>
public enum UserRole
{
    /// <summary>
    /// Can originate an engineering change request.
    /// </summary>
    Requester = 0,

    /// <summary>
    /// Can review an ECO before a formal approval decision.
    /// </summary>
    Reviewer = 1,

    /// <summary>
    /// Can approve or reject an ECO under review.
    /// </summary>
    Approver = 2,

    /// <summary>
    /// Can administer company-scoped configuration and users.
    /// </summary>
    Administrator = 3
}
