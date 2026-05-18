using EngiFlow.Domain.Companies;

namespace EngiFlow.Application.Settings.Dtos;

/// <summary>
/// Converts company settings domain entities into application DTOs.
/// </summary>
internal static class CompanySettingsMappingExtensions
{
    /// <summary>
    /// Converts settings to the API-facing governance DTO.
    /// </summary>
    public static CompanySettingsDto ToDto(this CompanySettings settings)
    {
        return new CompanySettingsDto(settings.MinApprovalsRequired, settings.UpdatedAt);
    }
}
