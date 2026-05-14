using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Users;

namespace EngiFlow.Domain.Tests;

public sealed class CompanyTests
{
    [Fact]
    public void RegisterUser_PreservesCompanyTenant()
    {
        var company = Company.Create("Acme Engineering");

        var user = company.RegisterUser("REQUESTER@ACME.EXAMPLE", "Requester", UserRole.Requester);

        Assert.Equal(company.Id, user.CompanyId);
        Assert.Equal("requester@acme.example", user.Email);
        Assert.Contains(user, company.Users);
    }

    [Fact]
    public void Rename_WhenCompanyIsInactive_Throws()
    {
        var company = Company.Create("Acme Engineering");
        company.Deactivate();

        var exception = Assert.Throws<DomainException>(() => company.Rename("Acme Manufacturing"));

        Assert.Contains("Inactive companies", exception.Message);
    }
}
