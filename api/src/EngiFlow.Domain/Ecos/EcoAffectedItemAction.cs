namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Describes the engineering action represented by an ECO affected item.
/// </summary>
public enum EcoAffectedItemAction
{
    /// <summary>
    /// A new controlled item is being introduced.
    /// </summary>
    Add = 0,

    /// <summary>
    /// An existing controlled item is being changed.
    /// </summary>
    Modify = 1,

    /// <summary>
    /// An existing controlled item is being removed or retired.
    /// </summary>
    Remove = 2
}
