using Microsoft.Playwright;

namespace SistemaPlanificacionSNP.E2ETests.Pages
{
    public sealed class LoginPage
    {
        private readonly IPage _page;

        public LoginPage(IPage page)
        {
            _page = page;
        }

        private ILocator UsernameInput => _page.Locator("input[name='NombreUsuario']");

        private ILocator PasswordInput => _page.Locator("input[name='Password']");

        private ILocator SubmitButton => _page.Locator("#submitBtn");

        private ILocator SweetAlertPopup => _page.Locator(".swal2-container .swal2-popup, .swal2-popup.swal2-toast");

        public async Task NavigateAsync(string baseUrl)
        {
            var loginUrl = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "account/login").ToString();
            await _page.GotoAsync(loginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await UsernameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }

        public async Task LoginAsync(string username, string password)
        {
            await UsernameInput.FillAsync(username);
            await PasswordInput.FillAsync(password);
            await SubmitButton.ClickAsync();

            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
            }
            catch (TimeoutException)
            {
                // The app may keep background network activity; continue with explicit UI waits in tests.
            }
        }

        public async Task<bool> WaitForAsyncNotificationAsync(TimeSpan timeout)
        {
            try
            {
                await SweetAlertPopup.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = (float)timeout.TotalMilliseconds
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (PlaywrightException)
            {
                return false;
            }
        }

        public async Task DismissNotificationIfPresentAsync()
        {
            var confirmButton = _page.Locator(".swal2-confirm");
            if (await confirmButton.CountAsync() > 0 && await confirmButton.First.IsVisibleAsync())
            {
                await confirmButton.First.ClickAsync();
            }
        }
    }
}
