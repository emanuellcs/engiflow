namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to reject an engineering change order.
/// </summary>
/// <param name="Reason">The business justification for rejecting the ECO.</param>
public sealed record RejectEcoRequest(string Reason);
