using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Describes a valid setup-password token context for the public password page.
/// </summary>
/// <param name="Purpose">The token purpose.</param>
/// <param name="Email">The account email associated with the token.</param>
public sealed record SetupPasswordContextDto(PasswordSetupTokenPurpose Purpose, string Email);

/// <summary>
/// Represents a completed password setup or reset operation.
/// </summary>
public sealed record SetupPasswordResultDto;
