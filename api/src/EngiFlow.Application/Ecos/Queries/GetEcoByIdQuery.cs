using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Queries;

/// <summary>
/// Query that retrieves one ECO with its audit history.
/// </summary>
/// <param name="EcoId">The ECO identifier to retrieve.</param>
public sealed record GetEcoByIdQuery(Guid EcoId) : IQuery<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="GetEcoByIdQuery"/> requests before retrieval.
/// </summary>
public sealed class GetEcoByIdQueryValidator : AbstractValidator<GetEcoByIdQuery>
{
    /// <summary>
    /// Initializes validation rules for retrieving one ECO.
    /// </summary>
    public GetEcoByIdQueryValidator()
    {
        RuleFor(query => query.EcoId)
            .NotEmpty()
            .WithMessage("ECO id is required.");
    }
}

/// <summary>
/// Handles retrieval of a single tenant-scoped ECO with audit history.
/// </summary>
public sealed class GetEcoByIdQueryHandler : IQueryHandler<GetEcoByIdQuery, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEcoByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    public GetEcoByIdQueryHandler(IEngineeringChangeOrderRepository ecos)
    {
        _ecos = ecos;
    }

    /// <inheritdoc />
    public async Task<EcoDetailsDto> HandleAsync(
        GetEcoByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var eco = await _ecos.GetByIdWithEventsAsync(
                EngineeringChangeOrderId.From(query.EcoId),
                cancellationToken)
            .ConfigureAwait(false);

        if (eco is null)
        {
            throw new EntityNotFoundException("EngineeringChangeOrder", query.EcoId);
        }

        return eco.ToDetailsDto();
    }
}
