using Microsoft.Extensions.Configuration;

namespace SistemaPlanificacionSNP.E2ETests.Config
{
    public static class E2ETestSettingsProvider
    {
        public static E2ETestSettings Load()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.e2e.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var settings = new E2ETestSettings();
            configuration.GetSection("E2E").Bind(settings);

            Validate(settings);
            return settings;
        }

        private static void Validate(E2ETestSettings settings)
        {
            if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var parsedBaseUrl)
                || (parsedBaseUrl.Scheme != Uri.UriSchemeHttps && parsedBaseUrl.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException(
                    "E2E: BaseUrl no es válida. Configura E2E:BaseUrl en appsettings.e2e.json o E2E__BaseUrl.");
            }

            if (string.IsNullOrWhiteSpace(settings.Credentials.Username)
                || string.IsNullOrWhiteSpace(settings.Credentials.Password))
            {
                throw new InvalidOperationException(
                    "E2E: faltan credenciales. Define E2E__Credentials__Username y E2E__Credentials__Password en variables de entorno.");
            }
        }
    }
}
