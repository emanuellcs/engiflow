using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Users;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Domain.Users;
using FluentValidation;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Application.Users.Commands;

/// <summary>
/// Command that creates a new active user in the current tenant.
/// </summary>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's initial plain-text password.</param>
/// <param name="Role">The user's role.</param>
public sealed record CreateUserCommand(
    string Name,
    string Email,
    string Password,
    UserRole Role) : ICommand<UserSummaryDto>;

/// <summary>
/// Validates <see cref="CreateUserCommand"/> requests before user creation.
/// </summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>
    /// Initializes validation rules for administrator-created users.
    /// </summary>
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Email is invalid.");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(12)
            .WithMessage("Password must be at least 12 characters.")
            .MaximumLength(256)
            .WithMessage("Password cannot exceed 256 characters.")
            .Matches("[A-Z]")
            .WithMessage("Password must include at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must include at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must include at least one number.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must include at least one symbol.");

        RuleFor(command => command.Role)
            .Must(role => role is UserRole.Administrator or UserRole.Approver or UserRole.Requester or UserRole.Viewer)
            .WithMessage("Role must be Administrator, Approver, Requester, or Viewer.");
    }
}

/// <summary>
/// Handles administrator user creation inside the current tenant boundary.
/// </summary>
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserSummaryDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository used to validate the current tenant.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="passwordHashService">The password hashing service.</param>
    /// <param name="tenantProvider">The current tenant provider.</param>
    /// <param name="unitOfWork">The unit of work used to save the new user.</param>
    public CreateUserCommandHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IPasswordHashService passwordHashService,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _companies = companies;
        _users = users;
        _passwordHashService = passwordHashService;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<UserSummaryDto> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var actor = await UserManagementRules.GetActiveCurrentUserAsync(
                _users,
                _tenantProvider,
                cancellationToken)
            .ConfigureAwait(false);
        UserManagementRules.EnsureCanManageUsers(actor);

        var existingUser = await _users.GetByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (existingUser is not null)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserCommand.Email)] = ["Email is already registered."]
            });
        }

        var company = await _companies.GetByIdAsync(_tenantProvider.CurrentCompanyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
        {
            throw new EntityNotFoundException("Company", _tenantProvider.CurrentCompanyId.Value);
        }

        var user = company.RegisterUser(normalizedEmail, command.Name, command.Role);
        user.SetPasswordHash(_passwordHashService.HashPassword(user, command.Password));

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user.ToSummaryDto();
    }
}
