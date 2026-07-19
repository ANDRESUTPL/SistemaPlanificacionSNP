using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaPlanificacionSNP.Web.Services;

namespace SistemaPlanificacionSNP.Web.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public void SaveAuthData_ShouldWriteAuthCookies()
    {
        var context = new DefaultHttpContext();
        var service = CreateService(context);

        service.SaveAuthData("access-token", "refresh-token", "admin.user");

        var setCookieHeaders = context.Response.Headers.SetCookie.ToString();
        setCookieHeaders.Should().Contain("accessToken=access-token");
        setCookieHeaders.Should().Contain("refreshToken=refresh-token");
        setCookieHeaders.Should().Contain("userName=admin.user");
        setCookieHeaders.Should().Contain("httponly", Exactly.Twice());
        setCookieHeaders.Should().Contain("samesite=strict");
        setCookieHeaders.Should().Contain("secure");
    }

    [Fact]
    public void ClearAuthData_ShouldDeleteAuthCookies()
    {
        var context = new DefaultHttpContext();
        var service = CreateService(context);

        service.ClearAuthData();

        var setCookieHeaders = context.Response.Headers.SetCookie.ToString();
        setCookieHeaders.Should().Contain("accessToken=");
        setCookieHeaders.Should().Contain("refreshToken=");
        setCookieHeaders.Should().Contain("userName=");
        setCookieHeaders.Should().Contain("expires=Thu, 01 Jan 1970");
    }

    [Fact]
    public void GetTokensAndUserName_ShouldReadRequestCookies()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "accessToken=access-token; refreshToken=refresh-token; userName=admin.user";
        var service = CreateService(context);

        service.GetAccessToken().Should().Be("access-token");
        service.GetRefreshToken().Should().Be("refresh-token");
        service.GetUserName().Should().Be("admin.user");
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnUserAuthenticationState()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "Administrador") },
                "TestAuth"))
        };
        var service = CreateService(context);

        service.IsAuthenticated().Should().BeTrue();
    }

    [Fact]
    public void Methods_ShouldReturnSafeValues_WhenHttpContextIsNull()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.SetupGet(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new AuthService(accessorMock.Object, NullLogger<AuthService>.Instance);

        service.GetAccessToken().Should().BeNull();
        service.GetRefreshToken().Should().BeNull();
        service.GetUserName().Should().BeNull();
        service.IsAuthenticated().Should().BeFalse();
        service.Invoking(x => x.SaveAuthData("a", "r", "u")).Should().NotThrow();
        service.Invoking(x => x.ClearAuthData()).Should().NotThrow();
    }

    private static AuthService CreateService(HttpContext context)
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.SetupGet(x => x.HttpContext).Returns(context);

        return new AuthService(accessorMock.Object, NullLogger<AuthService>.Instance);
    }
}