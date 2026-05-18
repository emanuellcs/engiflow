using EngiFlow.Domain.Ecos;

namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to add an affected engineering item to an ECO.
/// </summary>
/// <param name="PartNumber">The affected part or document number.</param>
/// <param name="Description">The affected item description.</param>
/// <param name="CurrentRevision">The current revision.</param>
/// <param name="NewRevision">The proposed revision.</param>
/// <param name="Action">The action represented by this affected item.</param>
public sealed record AddAffectedItemRequest(
    string PartNumber,
    string Description,
    string CurrentRevision,
    string NewRevision,
    EcoAffectedItemAction Action);
