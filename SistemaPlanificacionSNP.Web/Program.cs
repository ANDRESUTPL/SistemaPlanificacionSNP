using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaPlanificacionSNP.Web.Services;

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
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
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==================== APLICACIÓN ====================

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Middleware personalizado para verificar expiración de token
app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["accessToken"];
    
    if (!string.IsNullOrEmpty(token) && context.User?.Identity?.IsAuthenticated == true)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length == 3)
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(
                    System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(parts[1]))
                );

                if (payload != null && payload.TryGetValue("exp", out var expObj))
                {
                    if (long.TryParse(expObj.ToString(), out var exp))
                    {
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        if (now > exp)
                        {
                            context.Response.Cookies.Delete("accessToken");
                            context.Response.Cookies.Delete("refreshToken");
                            context.Response.Redirect("/Account/Login");
                            return;
                        }
                    }
                }
            }
        }
        catch { }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
