namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to authenticate a user and issue a bearer token.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plain-text password.</param>
public sealed record LoginRequest(string Email, string Password);
