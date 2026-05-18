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
    /// Owns the company tenant and can manage administrators.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Can administer company-scoped configuration and users except owners.
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Can approve or request changes on an ECO under review.
    /// </summary>
    Approver = 2,

    /// <summary>
    /// Can originate an engineering change request.
    /// </summary>
    Requester = 3,

    /// <summary>
    /// Can read tenant-scoped workflow data without mutating it.
    /// </summary>
    Viewer = 4
}
