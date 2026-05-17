using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EngiFlow.Infrastructure.Security;

/// <summary>
/// Password hashing adapter backed by ASP.NET Core Identity's hardened password hasher.
/// </summary>
internal sealed class AspNetCorePasswordHashService : IPasswordHashService
{
    private const int PasswordHashIterations = 210_000;

    private readonly PasswordHasher<User> _passwordHasher = new(new OptionsWrapper<PasswordHasherOptions>(
        new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = PasswordHashIterations
        }));

    /// <inheritdoc />
    public string HashPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(user, password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
