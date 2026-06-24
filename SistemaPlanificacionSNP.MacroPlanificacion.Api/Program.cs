var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => new { status = "healthy", service = "MacroPlanificacion" });

app.Run();