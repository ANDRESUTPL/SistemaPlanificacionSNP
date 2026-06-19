namespace SistemaPlanificacionSNP.Infrastructure.JWT
{
    /// <summary>
    /// Configuración de JWT desde appsettings.json
    /// </summary>
    public class JwtSettings
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
