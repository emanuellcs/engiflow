using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Create_WithDefaultCompanyId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => User.Create(
            default,
            "approver@engiflow.example",
            "Approver",
            UserRole.Approver));

        Assert.Contains("companyId is required", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidEmail_Throws()
    {
        var companyId = CompanyId.New();

        var exception = Assert.Throws<DomainException>(() => User.Create(
            companyId,
            "not-an-email",
            "Approver",
            UserRole.Approver));

        Assert.Contains("Email is invalid", exception.Message);
    }

    [Fact]
    public void ChangeRole_WhenInactive_Throws()
    {
        var user = User.Create(
            CompanyId.New(),
            "reviewer@engiflow.example",
            "Reviewer",
            UserRole.Viewer);
        user.Deactivate();

        Assert.Throws<DomainException>(() => user.ChangeRole(UserRole.Approver));
    }

    [Fact]
    public void SetPasswordHash_WhenHashIsValid_StoresHash()
    {
        var user = User.Create(
            CompanyId.New(),
            "admin@engiflow.example",
            "Administrator",
            UserRole.Administrator);

        user.SetPasswordHash("hashed-password");

        Assert.Equal("hashed-password", user.PasswordHash);
    }
}
