using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Tests;

public sealed class CompanySettingsTests
{
    [Fact]
    public void CreateDefault_SetsExpectedInitialValues()
    {
        var companyId = CompanyId.New();
        
        var settings = CompanySettings.CreateDefault(companyId);

        Assert.Equal(companyId, settings.CompanyId);
        Assert.Equal(1, settings.MinApprovalsRequired);
        Assert.Equal(5, settings.MaxReviewDaysBeforeSlabreach);
        Assert.False(settings.AllowSelfApproval);
    }

    [Fact]
    public void UpdatePolicies_UpdatesAllFields()
    {
        var settings = CompanySettings.CreateDefault(CompanyId.New());
        
        settings.UpdatePolicies(3, 10, true);

        Assert.Equal(3, settings.MinApprovalsRequired);
        Assert.Equal(10, settings.MaxReviewDaysBeforeSlabreach);
        Assert.True(settings.AllowSelfApproval);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 0)]
    [InlineData(-1, 5)]
    [InlineData(1, -1)]
    public void UpdatePolicies_ThrowsException_WhenValuesAreInvalid(int minApprovals, int slaDays)
    {
        var settings = CompanySettings.CreateDefault(CompanyId.New());

        Assert.Throws<DomainException>(() => settings.UpdatePolicies(minApprovals, slaDays, true));
    }
}
