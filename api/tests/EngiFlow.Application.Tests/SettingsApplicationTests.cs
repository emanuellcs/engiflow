using EngiFlow.Application.Settings.Commands;
using FluentValidation.TestHelper;

namespace EngiFlow.Application.Tests;

public sealed class SettingsApplicationTests
{
    private readonly UpdateCompanySettingsCommandValidator _validator = new();

    [Fact]
    public void Validator_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var command = new UpdateCompanySettingsCommand(2, 7, true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 0)]
    public void Validator_ShouldHaveErrors_WhenValuesAreInvalid(int minApprovals, int slaDays)
    {
        var command = new UpdateCompanySettingsCommand(minApprovals, slaDays, true);

        var result = _validator.TestValidate(command);

        if (minApprovals < 1)
        {
            result.ShouldHaveValidationErrorFor(x => x.MinApprovalsRequired);
        }

        if (slaDays < 1)
        {
            result.ShouldHaveValidationErrorFor(x => x.MaxReviewDaysBeforeSlabreach);
        }
    }
}
