using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaPlanificacionSNP.Web.Services;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// ==================== SERVICIOS ====================

// HttpClientFactory para comunicación con APIs
var apiGatewayBaseUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "https://localhost:52555";

var apiClientBuilder = builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
	client.BaseAddress = new Uri(apiGatewayBaseUrl);
	client.Timeout = TimeSpan.FromSeconds(30);
});

// Bypass SSL solo en Development para certificados locales no confiables (Hipótesis B)
if (builder.Environment.IsDevelopment())
{
	apiClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
	});
}

// IHttpContextAccessor para acceder al contexto HTTP
builder.Services.AddHttpContextAccessor();

// Servicios de negocio
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClaimsTransformation, JwtClaimsTransformation>();

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
		options.Events.OnRedirectToLogin = context =>
		{
			var logger = context.HttpContext.RequestServices
				.GetRequiredService<ILoggerFactory>()
				.CreateLogger("CookieAuthentication");
			logger.LogWarning("Cookie challenge for {Path}; authenticated: {Authenticated}; cookie header present: {CookiePresent}",
				context.Request.Path,
				context.HttpContext.User.Identity?.IsAuthenticated == true,
				context.Request.Headers.ContainsKey("Cookie"));
				context.Response.Redirect(context.RedirectUri);
				return Task.CompletedTask;
		};
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

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromHours(1);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseCors("AllowAll");

app.UseAuthentication();

// Middleware de consistencia de sesión
app.Use(async (context, next) =>
{
	if (context.User?.Identity?.IsAuthenticated == true)
	{
		var authService = context.RequestServices.GetRequiredService<IAuthService>();
		var accessToken = authService.GetAccessToken();

		// Si no hay token de acceso, dejamos que continúe. 
		// El atributo [Authorize] del MVC y las APIs se encargarán de rechazar si es necesario.
		if (string.IsNullOrWhiteSpace(accessToken))
		{
			await next();
			return;
		}

		bool shouldRefresh = false;
		var remaining = TimeSpan.Zero;
		try
		{
			var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
			remaining = token.ValidTo - DateTime.UtcNow;

			// Solo refrescamos si genuinamente está por expirar (menos de 5 minutos)
			if (remaining <= TimeSpan.FromMinutes(5))
			{
				shouldRefresh = true;
			}
		}
		catch(Exception ex)
		{
			app.Logger.LogWarning(ex, "Could not parse access token for {Path}; continuing without refresh.", context.Request.Path);
			// Si hay un error parseando el token, no cerramos la sesión abruptamente.
			// Dejamos que el flujo continúe.
		}

		if (shouldRefresh)
		{
			app.Logger.LogInformation("Refreshing access token for {Path}; remaining lifetime: {Remaining}", context.Request.Path, remaining);
			var refreshed = await authService.RefreshTokenAsync();
			if (!refreshed)
			{
				app.Logger.LogWarning("Access token refresh failed for {Path}; clearing authentication cookies.", context.Request.Path);
				// Solo si realmente necesitábamos refrescar y falló, cerramos la sesión.
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
