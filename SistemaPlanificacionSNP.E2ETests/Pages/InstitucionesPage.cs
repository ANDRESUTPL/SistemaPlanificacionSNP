using Microsoft.Playwright;

namespace SistemaPlanificacionSNP.E2ETests.Pages
{
	public sealed class InstitucionesPage
	{
		private readonly IPage _page;

		public InstitucionesPage(IPage page)
		{
			_page = page;
		}

		// Navegación
		private ILocator MenuParametrizacion => _page.GetByText("Parametrización", new PageGetByTextOptions { Exact = false }).First;
		private ILocator MenuEntidades => _page.GetByText("Entidades Públicas", new PageGetByTextOptions { Exact = false }).First;

		// Botones principales
		private ILocator BtnNuevoPeriodo => _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Nuevo Período" });
		private ILocator BtnNuevaEntidad => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Nueva Entidad" });

		// Modal de Período
		private ILocator InputCodigoPeriodo => _page.Locator("#addPeriodoModal input[name='Codigo']");
		private ILocator InputNombrePeriodo => _page.Locator("#addPeriodoModal input[name='Nombre']");
		private ILocator InputFechaInicio => _page.Locator("#addPeriodoModal input[name='FechaInicio']");
		private ILocator InputFechaFin => _page.Locator("#addPeriodoModal input[name='FechaFin']");
		private ILocator BtnGuardarPeriodo => _page.Locator("#addPeriodoModal button[type='submit']");

		// Formulario de Entidad
		private ILocator InputCodigoEntidad => _page.Locator("input[name='Codigo']");
		private ILocator InputNombreEntidad => _page.Locator("input[name='Nombre']");
		private ILocator InputSiglaEntidad => _page.Locator("input[name='Sigla']");
		private ILocator SelectTipoEntidad => _page.Locator("select[name='Tipo']");
		private ILocator SelectNivelGobierno => _page.Locator("select[name='NivelGobierno']");
		private ILocator BtnGuardarEntidad => _page.Locator("#btnSubmit");

		public async Task NavegarAInstitucionesAsync(int navigationTimeoutMs)
		{
			await MenuParametrizacion.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await MenuParametrizacion.ClickAsync();

			await MenuEntidades.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = navigationTimeoutMs });
			await MenuEntidades.ClickAsync();
		}

		public async Task CrearPeriodoAsync(string codigo, string nombre, string fechaInicio, string fechaFin)
		{
			await BtnNuevoPeriodo.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await BtnNuevoPeriodo.ClickAsync();

			await InputCodigoPeriodo.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await InputCodigoPeriodo.FillAsync(codigo);
			await InputNombrePeriodo.FillAsync(nombre);
			await InputFechaInicio.FillAsync(fechaInicio); // Formato esperado: yyyy-MM-dd
			await InputFechaFin.FillAsync(fechaFin);

			await BtnGuardarPeriodo.ClickAsync();
		}

		public async Task IrACrearEntidadAsync()
		{
			await BtnNuevaEntidad.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await BtnNuevaEntidad.ClickAsync();
		}

		public async Task CrearEntidadAsync(string codigo, string nombre, string sigla, string tipo, string nivelGobierno)
		{
			await InputNombreEntidad.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await InputCodigoEntidad.FillAsync(codigo);
			await InputNombreEntidad.FillAsync(nombre);
			await InputSiglaEntidad.FillAsync(sigla);

			await SelectTipoEntidad.SelectOptionAsync(new SelectOptionValue { Label = tipo });
			await SelectNivelGobierno.SelectOptionAsync(new SelectOptionValue { Label = nivelGobierno });

			await BtnGuardarEntidad.ClickAsync();
		}
	}
}