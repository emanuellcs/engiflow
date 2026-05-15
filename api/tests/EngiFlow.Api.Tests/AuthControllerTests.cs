using EngiFlow.Api.Auth;
using EngiFlow.Api.Controllers;
using EngiFlow.Api.Models;
using EngiFlow.Api.Tenancy;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Auth.Queries;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace EngiFlow.Api.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task LoginAsync_DispatchesLoginQueryAndReturnsOk()
    {
        var loginResult = new LoginResultDto(
            "jwt-token",
            "Bearer",
            DateTimeOffset.Parse("2026-05-15T01:00:00Z"));
        var mediator = new FakeApplicationMediator { Dispatch = _ => loginResult };
        var controller = new AuthController(mediator);

        var result = await controller.LoginAsync(
            new LoginRequest("admin@engiflow.local", "EngiFlow_Admin_123!"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(loginResult, ok.Value);

        var query = Assert.IsType<LoginQuery>(mediator.LastRequest);
        Assert.Equal("admin@engiflow.local", query.Email);
        Assert.Equal("EngiFlow_Admin_123!", query.Password);
    }

    [Fact]
    public void LoginAsync_AllowsAnonymousRequests()
    {
        var allowAnonymous = typeof(AuthController)
            .GetMethod(nameof(AuthController.LoginAsync))!
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true);

        Assert.NotEmpty(allowAnonymous);
    }

    [Fact]
    public void HttpContextTenantProvider_ReadsCompanyAndUserFromJwtClaims()
    {
        var companyId = CompanyId.New();
        var userId = UserId.New();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("tenant", companyId.Value.ToString()),
                    new Claim("sub", userId.Value.ToString())
                ],
                "Bearer"))
        };
        var provider = new HttpContextTenantProvider(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal(companyId, provider.CurrentCompanyId);
        Assert.Equal(userId, provider.CurrentUserId);
    }

    [Fact]
    public void HttpContextTenantProvider_WhenClaimIsMissing_ThrowsUnauthorizedAccessException()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", UserId.New().Value.ToString())],
                "Bearer"))
        };
        var provider = new HttpContextTenantProvider(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Throws<UnauthorizedAccessException>(() => provider.CurrentCompanyId);
    }

    [Fact]
    public void JwtTokenService_EmitsSubjectTenantAndRoleClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "EngiFlow.Api.Tests",
            Audience = "EngiFlow.Tests",
            SigningKey = "EngiFlow_Test_Jwt_Signing_Key_Change_Me_2026!",
            AccessTokenMinutes = 30
        });
        var user = User.Create(
            CompanyId.New(),
            "admin@engiflow.local",
            "Administrator",
            UserRole.Administrator);
        var service = new JwtTokenService(options);

        var token = service.CreateAccessToken(user);
        using var payload = JsonDocument.Parse(Base64UrlDecode(token.AccessToken.Split('.')[1]));
        var claims = payload.RootElement;

        Assert.Equal(user.Id.Value.ToString(), claims.GetProperty("sub").GetString());
        Assert.Equal(user.CompanyId.Value.ToString(), claims.GetProperty("tenant").GetString());
        Assert.Equal(nameof(UserRole.Administrator), claims.GetProperty("role").GetString());
        Assert.True(token.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private sealed class FakeApplicationMediator : IApplicationMediator
    {
        public Func<object, object>? Dispatch { get; init; }

        public object? LastRequest { get; private set; }

        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse>
        {
            LastRequest = command;
            return Task.FromResult(Resolve<TResponse>(command));
        }

        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResponse>
        {
            LastRequest = query;
            return Task.FromResult(Resolve<TResponse>(query));
        }

        private TResponse Resolve<TResponse>(object request)
        {
            if (Dispatch is null)
            {
                throw new InvalidOperationException("No mediator dispatch delegate was configured.");
            }

            return (TResponse)Dispatch(request);
        }
    }
}
