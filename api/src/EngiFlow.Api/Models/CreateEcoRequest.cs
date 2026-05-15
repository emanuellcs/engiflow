using EngiFlow.Domain.Ecos;

namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to create a draft engineering change order.
/// </summary>
/// <param name="Title">The short business title of the requested engineering change.</param>
/// <param name="Description">The detailed engineering change description.</param>
/// <param name="Priority">The operational priority assigned to the ECO.</param>
public sealed record CreateEcoRequest(
    string Title,
    string Description,
    EcoPriority Priority);
