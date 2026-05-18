using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Settings.Dtos;
using EngiFlow.Domain.Companies;
using FluentValidation;

namespace EngiFlow.Application.Settings.Commands;

/// <summary>
/// Command that updates tenant workflow governance settings.
/// </summary>
/// <param name="MinApprovalsRequired">Minimum approvals required for an ECO review quorum.</param>
public sealed record UpdateCompanySettingsCommand(int MinApprovalsRequired) : ICommand<CompanySettingsDto>;

/// <summary>
/// Validates tenant workflow settings updates.
/// </summary>
public sealed class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    /// <summary>
    /// Initializes validation rules for settings updates.
    /// </summary>
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(command => command.MinApprovalsRequired)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Minimum approvals required must be at least one.");
    }
}

/// <summary>
/// Handles company settings updates for the current tenant.
/// </summary>
public sealed class UpdateCompanySettingsCommandHandler
    : ICommandHandler<UpdateCompanySettingsCommand, CompanySettingsDto>
{
    private readonly ICompanySettingsRepository _settings;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCompanySettingsCommandHandler"/> class.
    /// </summary>
    public UpdateCompanySettingsCommandHandler(
        ICompanySettingsRepository settings,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CompanySettingsDto> HandleAsync(
        UpdateCompanySettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentCompanyId;
        var settings = await _settings.GetByCompanyIdAsync(companyId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = CompanySettings.CreateDefault(companyId);
            await _settings.AddAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        settings.SetMinApprovalsRequired(command.MinApprovalsRequired);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return settings.ToDto();
    }
}
