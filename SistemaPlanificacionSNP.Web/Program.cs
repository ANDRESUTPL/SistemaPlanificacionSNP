using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaPlanificacionSNP.Web.Services;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// ==================== SERVICIOS ====================

// HttpClientFactory para comunicación con APIs
var apiGatewayBaseUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "https://localhost:52555";

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
	client.BaseAddress = new Uri(apiGatewayBaseUrl);
	client.Timeout = TimeSpan.FromSeconds(30);
});

// IHttpContextAccessor para acceder al contexto HTTP
builder.Services.AddHttpContextAccessor();

// Servicios de negocio
builder.Services.AddScoped<IAuthService, AuthService>();

// Autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Account/Login";
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Account/AccessDenied";
		options.ExpireTimeSpan = TimeSpan.FromDays(7);
		options.SlidingExpiration = true;
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
		options.Cookie.SameSite = SameSiteMode.Lax;
	});

// Autorización
builder.Services.AddAuthorization();

// Controladores y Vistas
builder.Services.AddControllersWithViews();

// Logging
builder.Services.AddLogging(config =>
{
	config.AddConsole();
	config.AddDebug();
});

// CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.WithOrigins("https://181.39.23.39", "http://181.39.23.39", "http://localhost:52555", "http://localhost:3000", "https://localhost:7010", "http://localhost:5000", "http://localhost:52550")
			   .AllowAnyMethod()
			   .AllowAnyHeader()
			   .AllowCredentials();
	});
});

// ==================== APLICACIÓN ====================

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();

// Middleware de consistencia de sesión: mantiene claims del principal alineados con JWT.
app.Use(async (context, next) =>
{
	if (context.User?.Identity?.IsAuthenticated == true)
	{
		var authService = context.RequestServices.GetRequiredService<IAuthService>();
		var accessToken = authService.GetAccessToken();
		var refreshToken = authService.GetRefreshToken();

		if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
		{
			await next();
			return;
		}

		bool shouldRefresh = false;
		if (string.IsNullOrWhiteSpace(accessToken))
		{
			shouldRefresh = true;
		}
		else
		{
			try
			{
				var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
				var remaining = token.ValidTo - DateTime.UtcNow;
				var hasPermissionClaims = context.User.Claims.Any(c =>
					c.Type.StartsWith("Lectura_", StringComparison.OrdinalIgnoreCase)
					|| c.Type.StartsWith("Creacion_", StringComparison.OrdinalIgnoreCase)
					|| c.Type.StartsWith("Edicion_", StringComparison.OrdinalIgnoreCase)
					|| c.Type.StartsWith("Eliminacion_", StringComparison.OrdinalIgnoreCase));

				shouldRefresh = remaining <= TimeSpan.FromMinutes(5) || !hasPermissionClaims;
			}
			catch
			{
				shouldRefresh = true;
			}
		}

		if (shouldRefresh)
		{
			var refreshed = await authService.RefreshTokenAsync();
			if (!refreshed)
			{
				authService.ClearAuthData();
				await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

				if (!context.Request.Path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase))
				{
					context.Response.Redirect("/Account/Login");
					return;
				}
			}
		}
	}

	await next();
});

app.UseAuthorization();


app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
}
