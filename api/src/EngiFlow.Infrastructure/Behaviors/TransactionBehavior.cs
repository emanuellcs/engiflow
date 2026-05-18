using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Messaging;
using EngiFlow.Infrastructure.Persistence;
using EngiFlow.Application.Mediation;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EngiFlow.Infrastructure.Behaviors;

/// <summary>
/// Wraps application commands in an EF Core transaction and coordinates post-commit work.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
internal sealed class TransactionBehavior<TRequest, TResponse> : EngiFlow.Application.Mediation.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IExternalOperationCompensation _compensation;
    private readonly EngiFlowDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private readonly IPublisher _publisher;
    private readonly IPostCommitNotificationQueue _postCommitNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core context that owns the command transaction.</param>
    /// <param name="postCommitNotifications">The scoped post-commit notification queue.</param>
    /// <param name="compensation">The scoped external compensation queue.</param>
    /// <param name="publisher">The MediatR publisher used after commit.</param>
    /// <param name="logger">The structured logger.</param>
    public TransactionBehavior(
        EngiFlowDbContext dbContext,
        IPostCommitNotificationQueue postCommitNotifications,
        IExternalOperationCompensation compensation,
        IPublisher publisher,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _postCommitNotifications = postCommitNotifications;
        _compensation = compensation;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        EngiFlow.Application.Mediation.RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommandBase)
        {
            return await next().ConfigureAwait(false);
        }

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await next().ConfigureAwait(false);
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var response = await next().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _compensation.Clear();

            await PublishPostCommitNotificationsAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (Exception)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            await CompensateAsync().ConfigureAwait(false);
            _postCommitNotifications.Clear();
            throw;
        }
    }

    private async Task PublishPostCommitNotificationsAsync(CancellationToken cancellationToken)
    {
        var notifications = _postCommitNotifications.Notifications.ToArray();
        _postCommitNotifications.Clear();

        foreach (var notification in notifications)
        {
            try
            {
                await _publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Post-commit notification {NotificationType} failed after the database transaction committed.",
                    notification.GetType().Name);
            }
        }
    }

    private async Task RollbackAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
            _logger.LogWarning(rollbackException, "Command transaction rollback failed.");
        }
    }

    private async Task CompensateAsync()
    {
        try
        {
            await _compensation.CompensateAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception compensationException)
        {
            _logger.LogError(compensationException, "External operation compensation failed.");
        }
    }
}
