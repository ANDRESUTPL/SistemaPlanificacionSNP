namespace SistemaPlanificacionSNP.E2ETests.Config
{
    public sealed class E2ETestSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:52550";

        public string Browser { get; set; } = "chromium";

        public bool Headless { get; set; } = false;

        public bool IgnoreHttpsErrors { get; set; } = true;

        public int DefaultTimeoutMs { get; set; } = 30000;

        public int NavigationTimeoutMs { get; set; } = 45000;

        public CredentialsSettings Credentials { get; set; } = new CredentialsSettings();

        public sealed class CredentialsSettings
        {
            public string Username { get; set; } = "admin.integration"
;

			public string Password { get; set; } = "Password123!";
        }
    }
}
