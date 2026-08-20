using Microsoft.Playwright;
using SistemaPlanificacionSNP.E2ETests.Config;

namespace SistemaPlanificacionSNP.E2ETests.Infrastructure
{
    public sealed class PlaywrightFixture : IAsyncLifetime, IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;

        public E2ETestSettings Settings { get; private set; } = new E2ETestSettings();

        public async Task InitializeAsync()
        {
            Settings = E2ETestSettingsProvider.Load();
            _playwright = await Playwright.CreateAsync();

            var browserType = ResolveBrowserType(_playwright, Settings.Browser);
            _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = Settings.Headless
            });
        }

        public async Task<(IBrowserContext Context, IPage Page)> CreatePageAsync()
        {
            if (_browser == null)
            {
                throw new InvalidOperationException("Playwright browser no está inicializado.");
            }

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = Settings.IgnoreHttpsErrors
            });

            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(Settings.DefaultTimeoutMs);
            page.SetDefaultNavigationTimeout(Settings.NavigationTimeoutMs);

            return (context, page);
        }

        public async Task DisposeAsync()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
            }

            _playwright?.Dispose();
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return new ValueTask(DisposeAsync());
        }

        private static IBrowserType ResolveBrowserType(IPlaywright playwright, string browser)
        {
            return browser.ToLowerInvariant() switch
            {
                "firefox" => playwright.Firefox,
                "webkit" => playwright.Webkit,
                _ => playwright.Chromium
            };
        }
    }
}
