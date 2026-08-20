using Microsoft.Playwright;
using SistemaPlanificacionSNP.E2ETests.Config;

namespace SistemaPlanificacionSNP.E2ETests.Infrastructure
{
    public abstract class E2ETestBase : IAsyncLifetime
    {
        private readonly PlaywrightFixture _fixture;
        private IBrowserContext? _context;

        protected E2ETestBase(PlaywrightFixture fixture)
        {
            _fixture = fixture;
        }

        protected E2ETestSettings Settings => _fixture.Settings;

        protected IPage Page { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            var pageSession = await _fixture.CreatePageAsync();
            _context = pageSession.Context;
            Page = pageSession.Page;
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.CloseAsync();
            }
        }
    }
}
