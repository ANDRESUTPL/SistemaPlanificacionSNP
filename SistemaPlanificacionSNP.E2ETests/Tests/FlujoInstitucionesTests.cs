using FluentAssertions;
using Microsoft.Playwright;
using SistemaPlanificacionSNP.E2ETests.Infrastructure;
using SistemaPlanificacionSNP.E2ETests.Pages;

namespace SistemaPlanificacionSNP.E2ETests.Tests
{
	public sealed class FlujoInstitucionesTests : E2ETestBase, IClassFixture<PlaywrightFixture>
	{
		public FlujoInstitucionesTests(PlaywrightFixture fixture)
			: base(fixture)
		{
		}

		[Fact]
		[Trait("Category", "E2E")]
		public async Task CrearPeriodo_DebeGuardarExitosamente_Y_MostrarAlerta()
		{
			// Arrange
			var loginPage = new LoginPage(Page);
			var instPage = new InstitucionesPage(Page);
			var codigoUnico = $"PER-{Guid.NewGuid().ToString()[..4]}";
			var anio = DateTime.Now.Year + 1; // Un año en el futuro para evitar cruces

			// Act
			await loginPage.NavigateAsync(Settings.BaseUrl);
			await loginPage.LoginAsync(Settings.Credentials.Username, Settings.Credentials.Password);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			await instPage.NavegarAInstitucionesAsync(Settings.NavigationTimeoutMs);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			await instPage.CrearPeriodoAsync(codigoUnico, $"Período E2E {anio}", $"{anio}-01-01", $"{anio}-12-31");

			// Assert
			var notificationDisplayed = await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5));
			notificationDisplayed.Should().BeTrue("El sistema debe emitir una alerta al guardar el período");

			var successTitle = Page.Locator(".swal2-title");
			(await successTitle.TextContentAsync()).Should().Contain("Éxito");
		}

		[Fact]
		[Trait("Category", "E2E")]
		public async Task CrearEntidadPublica_DebeGuardarExitosamente_Y_ListarseEnGrilla()
		{
			// Arrange
			var loginPage = new LoginPage(Page);
			var instPage = new InstitucionesPage(Page);
			var siglaUnica = $"E2E{Guid.NewGuid().ToString()[..3]}";
			var nombreEntidad = $"Ministerio de Pruebas {siglaUnica}";

			// Act
			await loginPage.NavigateAsync(Settings.BaseUrl);
			await loginPage.LoginAsync(Settings.Credentials.Username, Settings.Credentials.Password);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			await instPage.NavegarAInstitucionesAsync(Settings.NavigationTimeoutMs);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			await instPage.IrACrearEntidadAsync();
			await instPage.CrearEntidadAsync(siglaUnica, nombreEntidad, siglaUnica, "Ministerio", "Gobierno Central");

			// Assert
			var notificationDisplayed = await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5));
			notificationDisplayed.Should().BeTrue("El sistema debe emitir una alerta al guardar la entidad");
			await loginPage.DismissNotificationIfPresentAsync();

			var entidadEnGrilla = Page.Locator($"td:has-text('{nombreEntidad}')");
			await entidadEnGrilla.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			(await entidadEnGrilla.CountAsync()).Should().BeGreaterThan(0, "La entidad recién creada debe aparecer en el listado");
		}
	}
}