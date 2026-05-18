using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Abstractions.Storage;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Auth.Commands;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Auth.Queries;
using EngiFlow.Application.Behaviors;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Messaging;
using EngiFlow.Application.Settings.Commands;
using EngiFlow.Application.Settings.Queries;
using EngiFlow.Application.Users.Commands;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Application.Users.Notifications;
using EngiFlow.Application.Users.Queries;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Application.Tests;

public sealed class EcoApplicationTests
{
    [Fact]
    public async Task ValidationBehavior_WhenCommandIsInvalid_ThrowsAndDoesNotInvokeHandler()
    {
        var behavior = new ValidationBehavior<CreateEcoCommand, EcoDetailsDto>(
            new IValidator<CreateEcoCommand>[] { new CreateEcoCommandValidator() });
        var handlerInvoked = false;

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new CreateEcoCommand(string.Empty, string.Empty, (EcoPriority)999),
                () =>
                {
                    handlerInvoked = true;
                    return Task.FromResult(default(EcoDetailsDto)!);
                }));

        Assert.False(handlerInvoked);
        Assert.Contains(nameof(CreateEcoCommand.Title), exception.Errors.Keys);
        Assert.Contains(nameof(CreateEcoCommand.Description), exception.Errors.Keys);
        Assert.Contains(nameof(CreateEcoCommand.Priority), exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEcoCommandHandler_UsesCurrentTenantAndUserAndCreatesDraftEco()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "requester@engiflow.example",
            "Requester",
            UserRole.Requester);
        var ecos = new FakeEcoRepository();
        var users = new FakeUserRepository(currentUser);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateEcoCommandHandler(
            ecos,
            users,
            unitOfWork,
            new FakeTenantProvider(companyId, currentUser.Id),
            new FakePostCommitNotificationQueue());

        var dto = await handler.HandleAsync(new CreateEcoCommand(
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium));

        var createdEco = Assert.Single(ecos.Ecos);
        Assert.Equal(createdEco.Id.Value, dto.Id);
        Assert.Equal(companyId.Value, dto.CompanyId);
        Assert.Equal(currentUser.Id.Value, dto.CreatedByUserId);
        Assert.Equal(EcoStatus.Draft, dto.Status);
        Assert.Equal(EcoEventType.Created, Assert.Single(dto.Events).EventType);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task LoginQueryHandler_WhenCredentialsAreValid_ReturnsBearerToken()
    {
        var passwordHashService = new FakePasswordHashService();
        var company = Company.Create("EngiFlow Demo Company");
        var user = company.RegisterUser(
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        user.SetPasswordHash(passwordHashService.HashPassword(user, "EngiFlow_Admin_123!"));
        var jwtTokenService = new FakeJwtTokenService();
        var users = new FakeUserRepository(user);
        var handler = new LoginQueryHandler(
            new FakeCompanyRepository(company),
            users,
            passwordHashService,
            jwtTokenService);

        var result = await handler.HandleAsync(new LoginQuery(
            " ADMIN@ENGIFLOW.LOCAL ",
            "EngiFlow_Admin_123!"));

        Assert.NotNull(user.LastLoginAt);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal($"token:{user.Id.Value}", result.AccessToken);
        Assert.Equal(jwtTokenService.ExpiresAtUtc, result.ExpiresAtUtc);
        Assert.Equal("Administrator", result.UserName);
        Assert.Equal("EngiFlow Demo Company", result.CompanyName);
        Assert.Equal(new[] { nameof(UserRole.Administrator) }, result.Roles);
        Assert.Equal(user.Id, users.LastSuccessfulLoginUserId);
        Assert.Equal(user.LastLoginAt, users.LastSuccessfulLoginAt);
    }

    [Fact]
    public async Task LoginQueryHandler_WhenPasswordIsInvalid_ThrowsAuthenticationFailedException()
    {
        var passwordHashService = new FakePasswordHashService();
        var company = Company.Create("EngiFlow Demo Company");
        var user = company.RegisterUser(
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        user.SetPasswordHash(passwordHashService.HashPassword(user, "correct-password"));
        var handler = new LoginQueryHandler(
            new FakeCompanyRepository(company),
            new FakeUserRepository(user),
            passwordHashService,
            new FakeJwtTokenService());

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(new LoginQuery("admin@engiflow.local", "wrong-password")));
    }

    [Fact]
    public async Task LoginQueryHandler_WhenUserIsInactive_ThrowsAuthenticationFailedException()
    {
        var passwordHashService = new FakePasswordHashService();
        var company = Company.Create("EngiFlow Demo Company");
        var user = company.RegisterUser(
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        user.SetPasswordHash(passwordHashService.HashPassword(user, "EngiFlow_Admin_123!"));
        user.Deactivate();
        var handler = new LoginQueryHandler(
            new FakeCompanyRepository(company),
            new FakeUserRepository(user),
            passwordHashService,
            new FakeJwtTokenService());

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(new LoginQuery("admin@engiflow.local", "EngiFlow_Admin_123!")));
    }

    [Fact]
    public async Task LoginQueryHandler_WhenUserIsUnknown_ThrowsAuthenticationFailedException()
    {
        var handler = new LoginQueryHandler(
            new FakeCompanyRepository(),
            new FakeUserRepository(),
            new FakePasswordHashService(),
            new FakeJwtTokenService());

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(new LoginQuery("missing@engiflow.local", "EngiFlow_Admin_123!")));
    }

    [Fact]
    public async Task LoginQueryValidator_WhenCredentialsAreMissing_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<LoginQuery, LoginResultDto>(
            new IValidator<LoginQuery>[] { new LoginQueryValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new LoginQuery(string.Empty, string.Empty),
                () => Task.FromResult(default(LoginResultDto)!)));

        Assert.Contains(nameof(LoginQuery.Email), exception.Errors.Keys);
        Assert.Contains(nameof(LoginQuery.Password), exception.Errors.Keys);
    }

    [Fact]
    public async Task ForgotPasswordCommandHandler_SendsResetEmail()
    {
        var resetEmailSender = new FakePasswordResetEmailSender();
        var handler = new ForgotPasswordCommandHandler(resetEmailSender);

        await handler.HandleAsync(new ForgotPasswordCommand(" ADA@ACME.EXAMPLE "));

        Assert.Equal("ada@acme.example", resetEmailSender.Email);
        Assert.NotNull(resetEmailSender.ResetLink);
        Assert.Contains("ada%40acme.example", resetEmailSender.ResetLink, StringComparison.Ordinal);
        Assert.Contains("mock-", resetEmailSender.ResetLink, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForgotPasswordCommandValidator_WhenEmailIsInvalid_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<ForgotPasswordCommand, ForgotPasswordResultDto>(
            new IValidator<ForgotPasswordCommand>[] { new ForgotPasswordCommandValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new ForgotPasswordCommand("not-an-email"),
                () => Task.FromResult(default(ForgotPasswordResultDto)!)));

        Assert.Contains(nameof(ForgotPasswordCommand.Email), exception.Errors.Keys);
    }

    [Fact]
    public async Task RegisterCompanyCommandHandler_CreatesCompanyAdministratorAndReturnsBearerToken()
    {
        var companies = new FakeCompanyRepository();
        var users = new FakeUserRepository();
        var passwordHashService = new FakePasswordHashService();
        var jwtTokenService = new FakeJwtTokenService();
        var unitOfWork = new FakeUnitOfWork();
        var settings = new FakeCompanySettingsRepository();
        var handler = new RegisterCompanyCommandHandler(
            companies,
            users,
            passwordHashService,
            jwtTokenService,
            settings,
            unitOfWork);

        var result = await handler.HandleAsync(new RegisterCompanyCommand(
            "Acme Engineering",
            "Ada Lovelace",
            " ADA@ACME.EXAMPLE ",
            "StrongPass123!"));

        var company = Assert.Single(companies.Companies);
        Assert.Equal("Acme Engineering", company.Name);

        var admin = Assert.Single(company.Users);
        Assert.Equal(company.Id, admin.CompanyId);
        Assert.Equal("ada@acme.example", admin.Email);
        Assert.Equal("Ada Lovelace", admin.DisplayName);
        Assert.Equal(UserRole.Owner, admin.Role);
        Assert.Equal(passwordHashService.HashPassword(admin, "StrongPass123!"), admin.PasswordHash);
        Assert.Single(settings.Settings);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal($"token:{admin.Id.Value}", result.AccessToken);
        Assert.Equal(jwtTokenService.ExpiresAtUtc, result.ExpiresAtUtc);
        Assert.Equal("Ada Lovelace", result.UserName);
        Assert.Equal("Acme Engineering", result.CompanyName);
        Assert.Equal(new[] { nameof(UserRole.Owner) }, result.Roles);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterCompanyCommandHandler_WhenEmailAlreadyExists_ThrowsValidationExceptionAndDoesNotSave()
    {
        var existingUser = User.Create(
            CompanyId.New(),
            "ada@acme.example",
            "Ada Lovelace",
            UserRole.Administrator);
        var companies = new FakeCompanyRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterCompanyCommandHandler(
            companies,
            new FakeUserRepository(existingUser),
            new FakePasswordHashService(),
            new FakeJwtTokenService(),
            new FakeCompanySettingsRepository(),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new RegisterCompanyCommand(
                "Acme Engineering",
                "Ada Lovelace",
                " ADA@ACME.EXAMPLE ",
                "StrongPass123!")));

        Assert.Contains(nameof(RegisterCompanyCommand.AdminEmail), exception.Errors.Keys);
        Assert.Empty(companies.Companies);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterCompanyCommandValidator_WhenRequiredFieldsAreMissing_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<RegisterCompanyCommand, LoginResultDto>(
            new IValidator<RegisterCompanyCommand>[] { new RegisterCompanyCommandValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new RegisterCompanyCommand(string.Empty, string.Empty, string.Empty, string.Empty),
                () => Task.FromResult(default(LoginResultDto)!)));

        Assert.Contains(nameof(RegisterCompanyCommand.CompanyName), exception.Errors.Keys);
        Assert.Contains(nameof(RegisterCompanyCommand.AdminName), exception.Errors.Keys);
        Assert.Contains(nameof(RegisterCompanyCommand.AdminEmail), exception.Errors.Keys);
        Assert.Contains(nameof(RegisterCompanyCommand.AdminPassword), exception.Errors.Keys);
    }

    [Fact]
    public async Task RegisterCompanyCommandValidator_WhenPasswordMissesComplexity_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<RegisterCompanyCommand, LoginResultDto>(
            new IValidator<RegisterCompanyCommand>[] { new RegisterCompanyCommandValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new RegisterCompanyCommand(
                    "Acme Engineering",
                    "Ada Lovelace",
                    "ada@acme.example",
                    "longpassword"),
                () => Task.FromResult(default(LoginResultDto)!)));

        var passwordErrors = Assert.Contains(nameof(RegisterCompanyCommand.AdminPassword), exception.Errors);
        Assert.Contains(passwordErrors, message => message.Contains("uppercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListUsersQueryHandler_ReturnsActiveUserSummaries()
    {
        var company = Company.Create("Acme Engineering");
        var admin = company.RegisterUser(
            "admin@acme.example",
            "Ada Lovelace",
            UserRole.Administrator);
        var requester = company.RegisterUser(
            "requester@acme.example",
            "Grace Hopper",
            UserRole.Requester);
        var inactive = company.RegisterUser(
            "inactive@acme.example",
            "Inactive User",
            UserRole.Requester);
        inactive.Deactivate();
        var handler = new ListUsersQueryHandler(new FakeUserRepository(admin, requester, inactive));

        var result = await handler.HandleAsync(new ListUsersQuery());

        Assert.Equal(2, result.Count);
        Assert.Contains(result, user => user.Email == "admin@acme.example");
        Assert.Contains(result, user => user.Email == "requester@acme.example");
        Assert.DoesNotContain(result, user => user.Email == "inactive@acme.example");
    }

    [Fact]
    public async Task CreateUserCommandHandler_CreatesTenantUserWithPasswordHash()
    {
        var company = Company.Create("Acme Engineering");
        var owner = company.RegisterUser(
            "owner@acme.example",
            "Tenant Owner",
            UserRole.Owner);
        var users = new FakeUserRepository();
        var passwordHashService = new FakePasswordHashService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateUserCommandHandler(
            new FakeCompanyRepository(company),
            new FakeUserRepository(owner),
            passwordHashService,
            new FakeTenantProvider(company.Id, owner.Id),
            unitOfWork);

        var result = await handler.HandleAsync(new CreateUserCommand(
            "Grace Hopper",
            " GRACE@ACME.EXAMPLE ",
            "StrongPass123!",
            UserRole.Approver));

        var createdUser = Assert.Single(company.Users, user => user.Email == "grace@acme.example");
        Assert.Equal(createdUser.Id.Value, result.Id);
        Assert.Equal(company.Id, createdUser.CompanyId);
        Assert.Equal("grace@acme.example", createdUser.Email);
        Assert.Equal("Grace Hopper", createdUser.DisplayName);
        Assert.Equal(UserRole.Approver, createdUser.Role);
        Assert.Equal(passwordHashService.HashPassword(createdUser, "StrongPass123!"), createdUser.PasswordHash);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateUserCommandHandler_WhenEmailAlreadyExists_ThrowsValidationExceptionAndDoesNotSave()
    {
        var company = Company.Create("Acme Engineering");
        var owner = company.RegisterUser(
            "owner@acme.example",
            "Tenant Owner",
            UserRole.Owner);
        var existingUser = company.RegisterUser(
            "grace@acme.example",
            "Grace Hopper",
            UserRole.Requester);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateUserCommandHandler(
            new FakeCompanyRepository(company),
            new FakeUserRepository(owner, existingUser),
            new FakePasswordHashService(),
            new FakeTenantProvider(company.Id, owner.Id),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateUserCommand(
                "Grace Hopper",
                " GRACE@ACME.EXAMPLE ",
                "StrongPass123!",
                UserRole.Approver)));

        Assert.Contains(nameof(CreateUserCommand.Email), exception.Errors.Keys);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateUserCommandValidator_WhenRoleIsOwner_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<CreateUserCommand, UserSummaryDto>(
            new IValidator<CreateUserCommand>[] { new CreateUserCommandValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new CreateUserCommand(
                    "Review User",
                    "reviewer@acme.example",
                    "StrongPass123!",
                    UserRole.Owner),
                () => Task.FromResult(default(UserSummaryDto)!)));

        Assert.Contains(nameof(CreateUserCommand.Role), exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateUserCommandValidator_WhenPasswordMissesComplexity_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<CreateUserCommand, UserSummaryDto>(
            new IValidator<CreateUserCommand>[] { new CreateUserCommandValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new CreateUserCommand(
                    "Grace Hopper",
                    "grace@acme.example",
                    "longpassword",
                    UserRole.Requester),
                () => Task.FromResult(default(UserSummaryDto)!)));

        Assert.Contains(nameof(CreateUserCommand.Password), exception.Errors.Keys);
    }

    [Fact]
    public async Task SubmitAndApproveHandlers_TransitionEcoThroughApproval()
    {
        var fixture = CreateFixtureWithEco();
        var submitHandler = new SubmitEcoCommandHandler(
            fixture.Ecos,
            fixture.Users,
            fixture.UnitOfWork,
            fixture.TenantProvider,
            fixture.Notifications);
        var approveHandler = new ApproveEcoCommandHandler(
            fixture.Ecos,
            fixture.Users,
            fixture.Settings,
            fixture.UnitOfWork,
            fixture.TenantProvider,
            fixture.Notifications);

        var submitted = await submitHandler.HandleAsync(new SubmitEcoCommand(fixture.Eco.Id.Value));
        var approved = await approveHandler.HandleAsync(new ApproveEcoCommand(fixture.Eco.Id.Value));

        Assert.Equal(EcoStatus.UnderReview, submitted.Status);
        Assert.Equal(EcoStatus.Approved, approved.Status);
        Assert.Equal(2, fixture.UnitOfWork.SaveCount);
        Assert.Contains(approved.Events, ecoEvent => ecoEvent.EventType == EcoEventType.SubmittedForReview);
        Assert.Contains(approved.Events, ecoEvent => ecoEvent.EventType == EcoEventType.Approved);
    }

    [Fact]
    public async Task SubmitReviewDecisionCommandHandler_WhenActorCreatedEco_RejectsForCompliance()
    {
        var companyId = CompanyId.New();
        var author = User.Create(
            companyId,
            "author@engiflow.example",
            "Author",
            UserRole.Approver);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            author.Id);
        eco.SubmitForReview(author.Id);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SubmitReviewDecisionCommandHandler(
            new FakeEcoRepository(eco),
            new FakeUserRepository(author),
            new FakeCompanySettingsRepository(CompanySettings.CreateDefault(companyId)),
            unitOfWork,
            new FakeTenantProvider(companyId, author.Id),
            new FakePostCommitNotificationQueue());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new SubmitReviewDecisionCommand(
                eco.Id.Value,
                EcoApprovalDecision.Approve,
                null)));

        Assert.Equal(
            "Compliance Rule: The author of the ECO cannot participate in its approval quorum",
            exception.Message);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SettingsHandlers_MaterializeDefaultsAndUpdateQuorum()
    {
        var companyId = CompanyId.New();
        var repository = new FakeCompanySettingsRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tenantProvider = new FakeTenantProvider(companyId, UserId.New());
        var queryHandler = new GetCompanySettingsQueryHandler(repository, tenantProvider, unitOfWork);

        var defaults = await queryHandler.HandleAsync(new GetCompanySettingsQuery());

        Assert.Equal(1, defaults.MinApprovalsRequired);
        Assert.Single(repository.Settings);
        Assert.Equal(1, unitOfWork.SaveCount);

        var updateHandler = new UpdateCompanySettingsCommandHandler(repository, tenantProvider, unitOfWork);
        var updated = await updateHandler.HandleAsync(new UpdateCompanySettingsCommand(3));

        Assert.Equal(3, updated.MinApprovalsRequired);
        Assert.Equal(2, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateUserRoleCommandHandler_UpdatesRoleAndQueuesPermissionNotification()
    {
        var companyId = CompanyId.New();
        var admin = User.Create(companyId, "admin@acme.example", "Admin", UserRole.Administrator);
        var target = User.Create(companyId, "approver@acme.example", "Approver", UserRole.Approver);
        var notifications = new FakePostCommitNotificationQueue();
        var handler = new UpdateUserRoleCommandHandler(
            new FakeUserRepository(admin, target),
            new FakeUnitOfWork(),
            new FakeTenantProvider(companyId, admin.Id),
            notifications);

        var updated = await handler.HandleAsync(new UpdateUserRoleCommand(target.Id.Value, UserRole.Viewer));

        Assert.Equal(nameof(UserRole.Viewer), updated.Role);
        var notification = Assert.IsType<UserPermissionsChangedNotification>(
            Assert.Single(notifications.Notifications));
        Assert.Equal(target.Id.Value, notification.UserId);
        Assert.Equal(UserRole.Viewer, notification.NewRole);
    }

    [Fact]
    public async Task DeactivateUserCommandHandler_WhenActorTargetsSelf_ThrowsDomainException()
    {
        var companyId = CompanyId.New();
        var admin = User.Create(companyId, "admin@acme.example", "Admin", UserRole.Administrator);
        var unitOfWork = new FakeUnitOfWork();
        var notifications = new FakePostCommitNotificationQueue();
        var handler = new DeactivateUserCommandHandler(
            new FakeUserRepository(admin),
            unitOfWork,
            new FakeTenantProvider(companyId, admin.Id),
            notifications);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new DeactivateUserCommand(admin.Id.Value)));

        Assert.Equal("A user cannot deactivate themselves.", exception.Message);
        Assert.True(admin.IsActive);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Empty(notifications.Notifications);
    }

    [Fact]
    public async Task DeactivateUserCommandHandler_DeactivatesUserAndQueuesNotification()
    {
        var companyId = CompanyId.New();
        var admin = User.Create(companyId, "admin@acme.example", "Admin", UserRole.Administrator);
        var target = User.Create(companyId, "viewer@acme.example", "Viewer", UserRole.Viewer);
        var notifications = new FakePostCommitNotificationQueue();
        var handler = new DeactivateUserCommandHandler(
            new FakeUserRepository(admin, target),
            new FakeUnitOfWork(),
            new FakeTenantProvider(companyId, admin.Id),
            notifications);

        await handler.HandleAsync(new DeactivateUserCommand(target.Id.Value));

        Assert.False(target.IsActive);
        var notification = Assert.IsType<UserDeactivatedNotification>(
            Assert.Single(notifications.Notifications));
        Assert.Equal(target.Id.Value, notification.UserId);
    }

    [Fact]
    public async Task RejectEcoCommandHandler_ReturnsUnderReviewEcoToDraftWithReason()
    {
        var fixture = CreateFixtureWithEco();
        fixture.Eco.SubmitForReview(fixture.CurrentUser.Id);
        var handler = new RejectEcoCommandHandler(
            fixture.Ecos,
            fixture.Users,
            fixture.Settings,
            fixture.UnitOfWork,
            fixture.TenantProvider,
            fixture.Notifications);

        var dto = await handler.HandleAsync(new RejectEcoCommand(
            fixture.Eco.Id.Value,
            "Specification is incomplete."));

        Assert.Equal(EcoStatus.Draft, dto.Status);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Contains(dto.Events, ecoEvent => ecoEvent.EventType == EcoEventType.ChangesRequested);
        Assert.NotNull(dto.Comments);
        Assert.Contains(dto.Comments!, comment =>
            comment.Body.Contains("Specification is incomplete.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RejectEcoCommandValidator_WhenReasonIsMissing_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<RejectEcoCommand, EcoDetailsDto>(
            new IValidator<RejectEcoCommand>[] { new RejectEcoCommandValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new RejectEcoCommand(Guid.NewGuid(), string.Empty),
                () => Task.FromResult(default(EcoDetailsDto)!)));

        Assert.Contains(nameof(RejectEcoCommand.Reason), exception.Errors.Keys);
    }

    [Fact]
    public async Task GetEcoByIdQueryHandler_ReturnsEcoWithSortedAuditHistory()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "reviewer@engiflow.example",
            "Reviewer",
            UserRole.Approver);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            currentUser.Id,
            DateTimeOffset.Parse("2026-05-03T00:00:00Z"));
        eco.SubmitForReview(currentUser.Id, DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        var handler = new GetEcoByIdQueryHandler(new FakeEcoRepository(eco));

        var dto = await handler.HandleAsync(new GetEcoByIdQuery(eco.Id.Value));

        Assert.Equal(2, dto.Events.Count);
        Assert.True(dto.Events[0].OccurredAt <= dto.Events[1].OccurredAt);
        Assert.Equal(EcoEventType.SubmittedForReview, dto.Events[0].EventType);
        Assert.Equal(EcoEventType.Created, dto.Events[1].EventType);
    }

    [Fact]
    public async Task ListEcosQueryHandler_ReturnsPaginationMetadata()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "requester@engiflow.example",
            "Requester",
            UserRole.Requester);
        var ecos = Enumerable.Range(0, 5)
            .Select(index => EngineeringChangeOrder.Create(
                companyId,
                $"Change {index}",
                "Update a controlled engineering artifact.",
                EcoPriority.Medium,
                currentUser.Id,
                DateTimeOffset.Parse("2026-05-01T00:00:00Z").AddMinutes(index)))
            .ToArray();
        var handler = new ListEcosQueryHandler(
            new FakeEcoRepository(ecos),
            new FakeTenantProvider(companyId, currentUser.Id));

        var page = await handler.HandleAsync(new ListEcosQuery(PageNumber: 2, PageSize: 2));

        Assert.Equal(2, page.PageNumber);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task ListEcosQueryHandler_AppliesCurrentUserFilters()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "reviewer@engiflow.example",
            "Reviewer",
            UserRole.Approver);
        var otherUser = User.Create(
            companyId,
            "requester@engiflow.example",
            "Requester",
            UserRole.Requester);
        var createdByCurrentUser = EngineeringChangeOrder.Create(
            companyId,
            "Created by current actor",
            "Update a controlled engineering artifact.",
            EcoPriority.Medium,
            currentUser.Id);
        var createdByOtherUser = EngineeringChangeOrder.Create(
            companyId,
            "Created by another actor",
            "Update a controlled engineering artifact.",
            EcoPriority.Medium,
            otherUser.Id);
        var repository = new FakeEcoRepository(createdByCurrentUser, createdByOtherUser);
        var handler = new ListEcosQueryHandler(
            repository,
            new FakeTenantProvider(companyId, currentUser.Id));

        var page = await handler.HandleAsync(new ListEcosQuery(CreatedByMe: true));

        Assert.Equal(currentUser.Id.Value, Assert.Single(page.Items).CreatedByUserId);
        Assert.Equal(currentUser.Id, repository.LastFilter?.CreatedByUserId);
    }

    [Fact]
    public async Task ListEcosQueryValidator_WhenPageSizeIsOutOfRange_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<ListEcosQuery, PagedResult<EcoSummaryDto>>(
            new IValidator<ListEcosQuery>[] { new ListEcosQueryValidator() });

        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.HandleAsync(
                new ListEcosQuery(PageNumber: 1, PageSize: 101),
                () => Task.FromResult(default(PagedResult<EcoSummaryDto>)!)));

        Assert.Contains(nameof(ListEcosQuery.PageSize), exception.Errors.Keys);
    }

    [Fact]
    public async Task GetEcoReviewContextQueryHandler_ReturnsQuorumAndActiveUsers()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "owner@engiflow.example",
            "Owner",
            UserRole.Owner);
        var approver = User.Create(
            companyId,
            "approver@engiflow.example",
            "Approver",
            UserRole.Approver);
        var inactive = User.Create(
            companyId,
            "inactive@engiflow.example",
            "Inactive",
            UserRole.Approver);
        inactive.Deactivate();
        var settings = CompanySettings.CreateDefault(companyId);
        settings.SetMinApprovalsRequired(2);
        var handler = new GetEcoReviewContextQueryHandler(
            new FakeUserRepository(currentUser, approver, inactive),
            new FakeCompanySettingsRepository(settings),
            new FakeTenantProvider(companyId, currentUser.Id));

        var context = await handler.HandleAsync(new GetEcoReviewContextQuery());

        Assert.Equal(2, context.MinApprovalsRequired);
        Assert.Equal(2, context.Users.Count);
        Assert.Contains(context.Users, user => user.Id == approver.Id.Value);
        Assert.DoesNotContain(context.Users, user => user.Id == inactive.Id.Value);
    }

    [Fact]
    public async Task GetEcoAttachmentDownloadUrlQueryHandler_ChecksEcoOwnershipAndSignsObjectKey()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "requester@engiflow.example",
            "Requester",
            UserRole.Requester);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            currentUser.Id);
        var attachment = eco.AddAttachment(
            "drawing.pdf",
            1024,
            "tenants/acme/ecos/eco/attachments/drawing.pdf",
            "application/pdf",
            currentUser.Id);
        var storage = new FakeStorageService();
        var handler = new GetEcoAttachmentDownloadUrlQueryHandler(
            new FakeEcoRepository(eco),
            storage);

        var result = await handler.HandleAsync(
            new GetEcoAttachmentDownloadUrlQuery(eco.Id.Value, attachment.Id.Value));

        Assert.Equal("https://storage.example.test/signed", result.Url);
        Assert.Equal(attachment.ObjectKey, storage.SignedObjectKey);
        Assert.True(result.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetEcoAttachmentDownloadUrlQueryHandler_WhenAttachmentIsMissing_ThrowsNotFound()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "requester@engiflow.example",
            "Requester",
            UserRole.Requester);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            currentUser.Id);
        var handler = new GetEcoAttachmentDownloadUrlQueryHandler(
            new FakeEcoRepository(eco),
            new FakeStorageService());

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.HandleAsync(new GetEcoAttachmentDownloadUrlQuery(eco.Id.Value, Guid.NewGuid())));
    }

    private static EcoFixture CreateFixtureWithEco()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "actor@engiflow.example",
            "Actor",
            UserRole.Approver);
        var requester = User.Create(
            companyId,
            "requester@engiflow.example",
            "Requester",
            UserRole.Requester);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            requester.Id);
        var ecos = new FakeEcoRepository(eco);
        var users = new FakeUserRepository(currentUser, requester);
        var unitOfWork = new FakeUnitOfWork();
        var tenantProvider = new FakeTenantProvider(companyId, currentUser.Id);
        var settings = new FakeCompanySettingsRepository(CompanySettings.CreateDefault(companyId));
        var notifications = new FakePostCommitNotificationQueue();

        return new EcoFixture(eco, currentUser, ecos, users, unitOfWork, tenantProvider, settings, notifications);
    }

    private sealed record EcoFixture(
        EngineeringChangeOrder Eco,
        User CurrentUser,
        FakeEcoRepository Ecos,
        FakeUserRepository Users,
        FakeUnitOfWork UnitOfWork,
        FakeTenantProvider TenantProvider,
        FakeCompanySettingsRepository Settings,
        FakePostCommitNotificationQueue Notifications);

    private sealed class FakeTenantProvider : ITenantProvider
    {
        public FakeTenantProvider(CompanyId currentCompanyId, UserId currentUserId)
        {
            CurrentCompanyId = currentCompanyId;
            CurrentUserId = currentUserId;
        }

        public CompanyId CurrentCompanyId { get; }

        public UserId CurrentUserId { get; }
    }

    private sealed class FakeEcoRepository : IEngineeringChangeOrderRepository
    {
        public FakeEcoRepository(params EngineeringChangeOrder[] ecos)
        {
            Ecos = ecos.ToList();
        }

        public List<EngineeringChangeOrder> Ecos { get; }

        public EcoListFilter? LastFilter { get; private set; }

        public Task AddAsync(EngineeringChangeOrder eco, CancellationToken cancellationToken = default)
        {
            Ecos.Add(eco);
            return Task.CompletedTask;
        }

        public Task<EngineeringChangeOrder?> GetByIdAsync(
            EngineeringChangeOrderId id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Ecos.SingleOrDefault(eco => eco.Id == id));
        }

        public Task<EngineeringChangeOrder?> GetByIdWithEventsAsync(
            EngineeringChangeOrderId id,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, cancellationToken);
        }

        public Task<IReadOnlyList<EngineeringChangeOrder>> ListAsync(
            int pageNumber,
            int pageSize,
            EcoListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            IReadOnlyList<EngineeringChangeOrder> page = ApplyFilter(Ecos, filter)
                .OrderByDescending(eco => eco.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(page);
        }

        public Task<int> CountAsync(EcoListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return Task.FromResult(ApplyFilter(Ecos, filter).Count());
        }

        private static IEnumerable<EngineeringChangeOrder> ApplyFilter(
            IEnumerable<EngineeringChangeOrder> ecos,
            EcoListFilter? filter)
        {
            if (filter is null)
            {
                return ecos;
            }

            if (filter.CreatedByUserId is not null)
            {
                var createdByUserId = filter.CreatedByUserId.Value;
                ecos = ecos.Where(eco => eco.CreatedByUserId == createdByUserId);
            }

            return ecos;
        }
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public FakeCompanyRepository(params Company[] companies)
        {
            Companies = companies.ToList();
        }

        public List<Company> Companies { get; } = [];

        public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Companies.SingleOrDefault(company => company.Id == id));
        }

        public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        {
            Companies.Add(company);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly IReadOnlyCollection<User> _users;

        public FakeUserRepository(params User[] users)
        {
            _users = users;
        }

        public UserId? LastSuccessfulLoginUserId { get; private set; }

        public DateTimeOffset? LastSuccessfulLoginAt { get; private set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use Company.RegisterUser in application tests.");
        }

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByEmailForAuthenticationAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Email == normalizedEmail));
        }

        public Task<User?> GetByIdForAuthenticationAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, cancellationToken);
        }

        public Task RecordSuccessfulLoginAsync(
            UserId id,
            DateTimeOffset lastLoginAt,
            CancellationToken cancellationToken = default)
        {
            LastSuccessfulLoginUserId = id;
            LastSuccessfulLoginAt = lastLoginAt;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> users = _users
                .Where(user => user.IsActive)
                .OrderBy(user => user.DisplayName)
                .ThenBy(user => user.Email)
                .ToArray();

            return Task.FromResult(users);
        }
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(User user, string password)
        {
            return $"hash:{user.Id.Value}:{password}";
        }

        public bool VerifyPassword(User user, string password)
        {
            return user.PasswordHash == HashPassword(user, password);
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public DateTimeOffset ExpiresAtUtc { get; } = DateTimeOffset.Parse("2026-05-15T01:00:00Z");

        public AccessTokenResult CreateAccessToken(User user, string companyName)
        {
            return new AccessTokenResult($"token:{user.Id.Value}", ExpiresAtUtc);
        }
    }

    private sealed class FakePasswordResetEmailSender : IPasswordResetEmailSender
    {
        public string? Email { get; private set; }

        public string? ResetLink { get; private set; }

        public Task SendPasswordResetAsync(
            string email,
            string resetLink,
            CancellationToken cancellationToken = default)
        {
            Email = email;
            ResetLink = resetLink;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorageService : IStorageService
    {
        public string? SignedObjectKey { get; private set; }

        public Task<StorageUploadResult> UploadAsync(
            StorageUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Upload is not used by this test fake.");
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<string> GeneratePreSignedUrlAsync(
            string objectKey,
            TimeSpan expiresIn,
            CancellationToken cancellationToken = default)
        {
            SignedObjectKey = objectKey;
            return Task.FromResult("https://storage.example.test/signed");
        }
    }

    private sealed class FakeCompanySettingsRepository : ICompanySettingsRepository
    {
        public FakeCompanySettingsRepository(params CompanySettings[] settings)
        {
            Settings = settings.ToList();
        }

        public List<CompanySettings> Settings { get; }

        public Task<CompanySettings?> GetByCompanyIdAsync(
            CompanyId companyId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Settings.SingleOrDefault(settings => settings.CompanyId == companyId));
        }

        public Task AddAsync(CompanySettings settings, CancellationToken cancellationToken = default)
        {
            Settings.Add(settings);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePostCommitNotificationQueue : IPostCommitNotificationQueue
    {
        private readonly List<INotification> _notifications = [];

        public IReadOnlyCollection<INotification> Notifications => _notifications.AsReadOnly();

        public void Enqueue(INotification notification)
        {
            _notifications.Add(notification);
        }

        public void Clear()
        {
            _notifications.Clear();
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
