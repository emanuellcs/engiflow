using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Auth.Commands;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Auth.Queries;
using EngiFlow.Application.Behaviors;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;
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
            new FakeTenantProvider(companyId, currentUser.Id));

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
        var user = User.Create(
            CompanyId.New(),
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        user.SetPasswordHash(passwordHashService.HashPassword(user, "EngiFlow_Admin_123!"));
        var jwtTokenService = new FakeJwtTokenService();
        var handler = new LoginQueryHandler(
            new FakeUserRepository(user),
            passwordHashService,
            jwtTokenService);

        var result = await handler.HandleAsync(new LoginQuery(
            " ADMIN@ENGIFLOW.LOCAL ",
            "EngiFlow_Admin_123!"));

        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal($"token:{user.Id.Value}", result.AccessToken);
        Assert.Equal(jwtTokenService.ExpiresAtUtc, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task LoginQueryHandler_WhenPasswordIsInvalid_ThrowsAuthenticationFailedException()
    {
        var passwordHashService = new FakePasswordHashService();
        var user = User.Create(
            CompanyId.New(),
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        user.SetPasswordHash(passwordHashService.HashPassword(user, "correct-password"));
        var handler = new LoginQueryHandler(
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
        var user = User.Create(
            CompanyId.New(),
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        user.SetPasswordHash(passwordHashService.HashPassword(user, "EngiFlow_Admin_123!"));
        user.Deactivate();
        var handler = new LoginQueryHandler(
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
    public async Task RegisterCompanyCommandHandler_CreatesCompanyAdministratorAndReturnsBearerToken()
    {
        var companies = new FakeCompanyRepository();
        var users = new FakeUserRepository();
        var passwordHashService = new FakePasswordHashService();
        var jwtTokenService = new FakeJwtTokenService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterCompanyCommandHandler(
            companies,
            users,
            passwordHashService,
            jwtTokenService,
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
        Assert.Equal(UserRole.Administrator, admin.Role);
        Assert.Equal(passwordHashService.HashPassword(admin, "StrongPass123!"), admin.PasswordHash);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal($"token:{admin.Id.Value}", result.AccessToken);
        Assert.Equal(jwtTokenService.ExpiresAtUtc, result.ExpiresAtUtc);
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
    public async Task SubmitAndApproveHandlers_TransitionEcoThroughApproval()
    {
        var fixture = CreateFixtureWithEco();
        var submitHandler = new SubmitEcoCommandHandler(
            fixture.Ecos,
            fixture.Users,
            fixture.UnitOfWork,
            fixture.TenantProvider);
        var approveHandler = new ApproveEcoCommandHandler(
            fixture.Ecos,
            fixture.Users,
            fixture.UnitOfWork,
            fixture.TenantProvider);

        var submitted = await submitHandler.HandleAsync(new SubmitEcoCommand(fixture.Eco.Id.Value));
        var approved = await approveHandler.HandleAsync(new ApproveEcoCommand(fixture.Eco.Id.Value));

        Assert.Equal(EcoStatus.UnderReview, submitted.Status);
        Assert.Equal(EcoStatus.Approved, approved.Status);
        Assert.Equal(2, fixture.UnitOfWork.SaveCount);
        Assert.Contains(approved.Events, ecoEvent => ecoEvent.EventType == EcoEventType.SubmittedForReview);
        Assert.Contains(approved.Events, ecoEvent => ecoEvent.EventType == EcoEventType.Approved);
    }

    [Fact]
    public async Task RejectEcoCommandHandler_RejectsUnderReviewEcoWithReason()
    {
        var fixture = CreateFixtureWithEco();
        fixture.Eco.SubmitForReview(fixture.CurrentUser.Id);
        var handler = new RejectEcoCommandHandler(
            fixture.Ecos,
            fixture.Users,
            fixture.UnitOfWork,
            fixture.TenantProvider);

        var dto = await handler.HandleAsync(new RejectEcoCommand(
            fixture.Eco.Id.Value,
            "Specification is incomplete."));

        Assert.Equal(EcoStatus.Rejected, dto.Status);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Contains(dto.Events, ecoEvent =>
            ecoEvent.EventType == EcoEventType.Rejected
            && ecoEvent.Description.Contains("Specification is incomplete.", StringComparison.Ordinal));
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
            UserRole.Reviewer);
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
        var handler = new ListEcosQueryHandler(new FakeEcoRepository(ecos));

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

    private static EcoFixture CreateFixtureWithEco()
    {
        var companyId = CompanyId.New();
        var currentUser = User.Create(
            companyId,
            "actor@engiflow.example",
            "Actor",
            UserRole.Approver);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            currentUser.Id);
        var ecos = new FakeEcoRepository(eco);
        var users = new FakeUserRepository(currentUser);
        var unitOfWork = new FakeUnitOfWork();
        var tenantProvider = new FakeTenantProvider(companyId, currentUser.Id);

        return new EcoFixture(eco, currentUser, ecos, users, unitOfWork, tenantProvider);
    }

    private sealed record EcoFixture(
        EngineeringChangeOrder Eco,
        User CurrentUser,
        FakeEcoRepository Ecos,
        FakeUserRepository Users,
        FakeUnitOfWork UnitOfWork,
        FakeTenantProvider TenantProvider);

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
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<EngineeringChangeOrder> page = Ecos
                .OrderByDescending(eco => eco.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(page);
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Ecos.Count);
        }
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public List<Company> Companies { get; } = [];

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

        public AccessTokenResult CreateAccessToken(User user)
        {
            return new AccessTokenResult($"token:{user.Id.Value}", ExpiresAtUtc);
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
