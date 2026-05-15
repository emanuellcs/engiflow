using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Behaviors;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Application.Exceptions;
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
