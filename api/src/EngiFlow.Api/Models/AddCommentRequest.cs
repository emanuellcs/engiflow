namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to add a timeline comment to an ECO.
/// </summary>
/// <param name="Body">The comment body.</param>
public sealed record AddCommentRequest(string Body);
