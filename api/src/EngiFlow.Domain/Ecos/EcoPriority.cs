namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Indicates the operational urgency of an engineering change order.
/// </summary>
public enum EcoPriority
{
    /// <summary>
    /// Low urgency work that can follow normal planning cycles.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Standard priority work.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Important work that should be reviewed ahead of normal priority changes.
    /// </summary>
    High = 2,

    /// <summary>
    /// Time-sensitive work that may affect safety, compliance, or production continuity.
    /// </summary>
    Critical = 3
}
