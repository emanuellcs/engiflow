using EngiFlow.Domain.Ecos;

namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to update draft ECO details.
/// </summary>
/// <param name="Title">The updated ECO title.</param>
/// <param name="Description">The updated ECO description.</param>
/// <param name="Priority">The updated ECO priority.</param>
public sealed record UpdateEcoDetailsRequest(
    string Title,
    string Description,
    EcoPriority Priority);
