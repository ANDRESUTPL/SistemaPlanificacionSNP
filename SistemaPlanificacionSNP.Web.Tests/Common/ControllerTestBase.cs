using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace SistemaPlanificacionSNP.Web.Tests.Common;

public abstract class ControllerTestBase
{
    protected static DefaultHttpContext CreateHttpContext(bool authenticated = true)
    {
        var context = new DefaultHttpContext();

        if (authenticated)
        {
            context.User = CreateClaimsPrincipal();
        }

        return context;
    }

    protected static ClaimsPrincipal CreateClaimsPrincipal(
        string userId = "test-user-id",
        string userName = "Administrador",
        string role = "Administrador")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Role, role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, TestAuthHandler.SchemeName));
    }

    protected static void ConfigureController(Controller controller, bool authenticated = true)
    {
        var httpContext = CreateHttpContext(authenticated);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(
            httpContext,
            new Mock<ITempDataProvider>().Object);
    }
}