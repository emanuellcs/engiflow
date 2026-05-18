namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used by the legacy route to request changes on an engineering change order.
/// </summary>
/// <param name="Reason">The business justification for returning the ECO to draft.</param>
public sealed record RejectEcoRequest(string Reason);
