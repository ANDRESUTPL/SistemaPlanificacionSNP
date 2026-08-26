using Microsoft.Playwright;

namespace SistemaPlanificacionSNP.E2ETests.Pages
{
	public sealed class MacroPlanificacionPage
	{
		private readonly IPage _page;

		public MacroPlanificacionPage(IPage page)
		{
			_page = page;
		}

		// Localizadores basados en IndexMacroPlanificacion.cshtml
		private ILocator MenuPadre => _page.GetByText("Macro Planificación", new PageGetByTextOptions { Exact = false }).First;
		private ILocator MenuHijoPnd => _page.GetByText("Plan Nacional (PND)", new PageGetByTextOptions { Exact = false }).First;
		private ILocator BtnNuevoPlan => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Nuevo Plan Nacional" });

		// Localizadores basados en CrearPlan.cshtml
		private ILocator InputNombre => _page.Locator("input[name='Nombre']");
		private ILocator SelectPeriodo => _page.Locator("select[name='PeriodoPlanificacionId']");
		private ILocator BtnGuardar => _page.Locator("#btnSubmit");

		public async Task NavegarAPlanesNacionalesAsync(int navigationTimeoutMs)
		{
			await MenuPadre.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await MenuPadre.ClickAsync();

			await MenuHijoPnd.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = navigationTimeoutMs });
			await MenuHijoPnd.ClickAsync();
		}

		public async Task IrACrearPlanAsync()
		{
			await BtnNuevoPlan.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await BtnNuevoPlan.ClickAsync();
		}

		public async Task CrearPlanAsync(string nombrePlan)
		{
			// Esperar a que el formulario esté visible
			await InputNombre.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

			// Llenar el nombre
			await InputNombre.FillAsync(nombrePlan);

			// Seleccionar el primer período válido disponible (índice 1, ya que el índice 0 es el placeholder "-- Seleccione el período --")
			// Si el select estuviera vacío, la vista muestra una alerta en su lugar, pero asumimos que en el entorno E2E hay data preparada.
			var selectOptionCount = await SelectPeriodo.Locator("option").CountAsync();
			if (selectOptionCount > 1)
			{
				await SelectPeriodo.SelectOptionAsync(new SelectOptionValue { Index = 1 });
			}
			else
			{
				throw new Exception("No hay períodos disponibles para seleccionar en la prueba E2E.");
			}

			// Guardar
			await BtnGuardar.ClickAsync();
		}
	}
}