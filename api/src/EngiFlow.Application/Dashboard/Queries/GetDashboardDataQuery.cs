using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Dashboard.Dtos;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Dashboard.Queries;

/// <summary>
/// Query that aggregates role-specific metrics and actionable items for the dashboard.
/// </summary>
public sealed record GetDashboardDataQuery : IQuery<DashboardDataDto>;

/// <summary>
/// Handles the aggregation of role-based dashboard data.
/// </summary>
public sealed class GetDashboardDataQueryHandler : IQueryHandler<GetDashboardDataQuery, DashboardDataDto>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserRepository _users;
    private readonly IEngineeringChangeOrderRepository _ecos;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDashboardDataQueryHandler"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider to identify the current user and company.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="ecos">The ECO repository.</param>
    public GetDashboardDataQueryHandler(
        ITenantProvider tenantProvider,
        IUserRepository users,
        IEngineeringChangeOrderRepository ecos)
    {
        _tenantProvider = tenantProvider;
        _users = users;
        _ecos = ecos;
    }

    /// <inheritdoc />
    public async Task<DashboardDataDto> HandleAsync(
        GetDashboardDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _tenantProvider.CurrentUserId;
        var user = await _users.GetByIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return new DashboardDataDto(
                Array.Empty<DashboardMetricDto>(),
                Array.Empty<DashboardAttentionItemDto>());
        }

        return user.Role switch
        {
            UserRole.Approver => await GetApproverDashboardAsync(currentUserId, cancellationToken).ConfigureAwait(false),
            UserRole.Requester => await GetRequesterDashboardAsync(currentUserId, cancellationToken).ConfigureAwait(false),
            UserRole.Administrator or UserRole.Owner => await GetAdminDashboardAsync(cancellationToken).ConfigureAwait(false),
            UserRole.Viewer => await GetViewerDashboardAsync(cancellationToken).ConfigureAwait(false),
            _ => new DashboardDataDto(Array.Empty<DashboardMetricDto>(), Array.Empty<DashboardAttentionItemDto>())
        };
    }

    private async Task<DashboardDataDto> GetViewerDashboardAsync(CancellationToken ct)
    {
        var recentActivity = await _ecos.ListRecentActivityAsync(10, ct).ConfigureAwait(false);

        var attentionItems = recentActivity.Select(activity => new DashboardAttentionItemDto(
            activity.Event.EngineeringChangeOrderId.Value,
            activity.Event.Description,
            $"Activity by {activity.ActorName} • {activity.EcoTitle}",
            "Activity",
            $"/ecos/{activity.Event.EngineeringChangeOrderId.Value}",
            activity.ActorName,
            activity.EcoTitle,
            activity.Event.OccurredAt)).ToList();

        return new DashboardDataDto(Array.Empty<DashboardMetricDto>(), attentionItems);
    }

    private async Task<DashboardDataDto> GetApproverDashboardAsync(UserId userId, CancellationToken ct)
    {
        var awaitingReviewCount = await _ecos.CountAsync(
                new EcoListFilter(null, EcoStatus.UnderReview, null, null, null, null, userId),
                ct)
            .ConfigureAwait(false);

        var metrics = new List<DashboardMetricDto>
        {
            new("Waiting for your review", awaitingReviewCount, "Just now")
        };

        var attentionEcos = await _ecos.ListAsync(
                1,
                10,
                new EcoListFilter(null, EcoStatus.UnderReview, null, null, null, null, userId),
                ct)
            .ConfigureAwait(false);

        var attentionItems = attentionEcos.Select(eco => new DashboardAttentionItemDto(
            eco.Id.Value,
            eco.Title,
            $"{eco.Priority} priority • Under Review",
            "ECO",
            $"/ecos/{eco.Id.Value}",
            null,
            null,
            eco.CreatedAt)).ToList();

        return new DashboardDataDto(metrics, attentionItems);
    }

    private async Task<DashboardDataDto> GetRequesterDashboardAsync(UserId userId, CancellationToken ct)
    {
        var authoredCount = await _ecos.CountAsync(
                new EcoListFilter(null, null, null, null, null, userId, null),
                ct)
            .ConfigureAwait(false);

        var draftsCount = await _ecos.CountAsync(
                new EcoListFilter(null, EcoStatus.Draft, null, null, null, userId, null),
                ct)
            .ConfigureAwait(false);

        var metrics = new List<DashboardMetricDto>
        {
            new("Authored by you", authoredCount, "Just now"),
            new("Assigned to you", draftsCount, "Just now")
        };

        var draftEcos = await _ecos.ListAsync(
                1,
                10,
                new EcoListFilter(null, EcoStatus.Draft, null, null, null, userId, null),
                ct)
            .ConfigureAwait(false);

        var attentionItems = draftEcos.Select(eco => new DashboardAttentionItemDto(
            eco.Id.Value,
            eco.Title,
            $"{eco.Priority} priority • Draft",
            "ECO",
            $"/ecos/{eco.Id.Value}",
            null,
            null,
            eco.CreatedAt)).ToList();

        return new DashboardDataDto(metrics, attentionItems);
    }

    private async Task<DashboardDataDto> GetAdminDashboardAsync(CancellationToken ct)
    {
        var fiveDaysAgo = DateTimeOffset.UtcNow.AddDays(-5);
        
        var slaAtRiskCount = await _ecos.CountAsync(
                new EcoListFilter(null, EcoStatus.UnderReview, null, null, fiveDaysAgo, null, null),
                ct)
            .ConfigureAwait(false);

        var openOrdersCount = await _ecos.CountAsync(
                new EcoListFilter(null, EcoStatus.UnderReview, null, null, null, null, null),
                ct)
            .ConfigureAwait(false);

        var pendingActivationsCount = await _users.CountAsync(UserStatus.PendingActivation, ct).ConfigureAwait(false);

        var metrics = new List<DashboardMetricDto>
        {
            new("SLA At Risk (>5 days)", slaAtRiskCount, "Just now"),
            new("Open Orders Count", openOrdersCount, "Just now"),
            new("Pending Activations", pendingActivationsCount, "Just now")
        };

        var slaAtRiskEcos = await _ecos.ListAsync(
                1,
                10,
                new EcoListFilter(null, EcoStatus.UnderReview, null, null, fiveDaysAgo, null, null),
                ct)
            .ConfigureAwait(false);

        var attentionItems = slaAtRiskEcos.Select(eco => new DashboardAttentionItemDto(
            eco.Id.Value,
            eco.Title,
            $"{eco.Priority} priority • SLA At Risk",
            "ECO",
            $"/ecos/{eco.Id.Value}",
            null,
            null,
            eco.CreatedAt)).ToList();

        return new DashboardDataDto(metrics, attentionItems);
    }
}
