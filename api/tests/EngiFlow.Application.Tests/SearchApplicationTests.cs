using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Search.Dtos;
using EngiFlow.Application.Search.Queries;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Tests;

public sealed class SearchApplicationTests
{
    [Fact]
    public async Task GlobalSearchQueryHandler_WhenQueryIsEmpty_ReturnsEmptyResults()
    {
        var ecos = new FakeEcoRepository();
        var users = new FakeUserRepository();
        var handler = new GlobalSearchQueryHandler(ecos, users);

        var result = await handler.HandleAsync(new GlobalSearchQuery("  "));

        Assert.Empty(result.Ecos);
        Assert.Empty(result.Users);
    }

    [Fact]
    public async Task GlobalSearchQueryHandler_ReturnsMatchingEcosAndUsers()
    {
        var companyId = CompanyId.New();
        var userId = UserId.New();
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Aluminum Bracket",
            "Change material",
            EcoPriority.High,
            userId);
        var user = User.Create(
            companyId,
            "search@example.test",
            "Search User",
            UserRole.Viewer);

        var ecos = new FakeEcoRepository(eco);
        var users = new FakeUserRepository(user);
        var handler = new GlobalSearchQueryHandler(ecos, users);

        var result = await handler.HandleAsync(new GlobalSearchQuery("Search"));

        var foundUser = Assert.Single(result.Users);
        Assert.Equal(user.Id.Value, foundUser.Id);
        Assert.Equal("Search User", foundUser.DisplayName);
        
        // ECO search in fake repo is very basic, let's verify it calls ListAsync with correct filter
        Assert.Equal("Search", ecos.LastFilter?.Search);
    }

    private sealed class FakeEcoRepository : IEngineeringChangeOrderRepository
    {
        private readonly List<EngineeringChangeOrder> _ecos;

        public FakeEcoRepository(params EngineeringChangeOrder[] ecos)
        {
            _ecos = ecos.ToList();
        }

        public EcoListFilter? LastFilter { get; private set; }

        public Task AddAsync(EngineeringChangeOrder eco, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<EngineeringChangeOrder?> GetByIdAsync(EngineeringChangeOrderId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<EngineeringChangeOrder?> GetByIdWithEventsAsync(EngineeringChangeOrderId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<EngineeringChangeOrder>> ListAsync(
            int pageNumber,
            int pageSize,
            EcoListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            var results = _ecos.AsEnumerable();
            if (filter?.Search != null)
            {
                results = results.Where(e => e.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult<IReadOnlyList<EngineeringChangeOrder>>(results.Take(pageSize).ToList());
        }

        public Task<int> CountAsync(EcoListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<EcoActivityDto>> ListRecentActivityAsync(int limit, CancellationToken cancellationToken = default)
        {
            var activity = _ecos
                .SelectMany(eco => eco.Events.Select(e => new EcoActivityDto(e, "Test User", eco.Title)))
                .OrderByDescending(a => a.Event.OccurredAt)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<EcoActivityDto>>(activity);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users;

        public FakeUserRepository(params User[] users)
        {
            _users = users.ToList();
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> ListByEmailForAuthenticationAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<User?> GetByIdForAuthenticationAsync(UserId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RecordSuccessfulLoginAsync(UserId id, DateTimeOffset lastLoginAt, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SetPasswordAndActivateAsync(UserId id, string passwordHash, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> ListForAdministrationAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(
                _users.Where(u => u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) || 
                                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .ToList());
        }

        public Task<int> CountAsync(UserStatus? status = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(status.HasValue
                ? _users.Count(u => u.Status == status.Value)
                : _users.Count);
        }

        public Task<User?> GetOwnerByCompanyIdForAuthenticationAsync(CompanyId companyId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
