using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaPlanificacionSNP.Web.Services;
using Microsoft.AspNetCore.Authentication;

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

// Middleware personalizado para verificar expiración de token
app.Use(async (context, next) =>
{
	var token = context.Request.Cookies["accessToken"];
	await next();
});


app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
}
