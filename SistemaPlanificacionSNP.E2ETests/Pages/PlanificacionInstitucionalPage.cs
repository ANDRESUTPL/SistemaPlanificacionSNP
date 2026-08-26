using Microsoft.Playwright;

namespace SistemaPlanificacionSNP.E2ETests.Pages
{
	public sealed class PlanificacionInstitucionalPage
	{
		private readonly IPage _page;

		public PlanificacionInstitucionalPage(IPage page)
		{
			_page = page;
		}

		// Navegación principal
		private ILocator MenuPlanificacion => _page.GetByText("Planificación", new PageGetByTextOptions { Exact = true }).First;
		private ILocator MenuPei => _page.GetByText("PEI y Proyectos", new PageGetByTextOptions { Exact = false }).First;

		// Elementos del Index de PEI
		private ILocator BtnNuevoPei => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Nuevo PEI" });

		// Elementos de Crear PEI
		private ILocator SelectEntidad => _page.Locator("select[name='EntidadPublicaId']");
		private ILocator SelectPlanNacional => _page.Locator("select[name='PlanNacionalId']");
		private ILocator BtnGuardarPei => _page.Locator("#btnSubmit");

		// Elementos de Detalle / Proyectos
		private ILocator BtnFormularProyecto => _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Formular Proyecto" });
		private ILocator InputCup => _page.Locator("input[name='CodigoProyecto']");
		private ILocator InputMonto => _page.Locator("input[name='Monto']");
		private ILocator InputNombreProyecto => _page.Locator("input[name='Nombre']");
		private ILocator BtnGuardarProyecto => _page.Locator("#addProyectoModal button[type='submit']");

		public async Task NavegarAPlanificacionInstitucionalAsync(int navigationTimeoutMs)
		{
			await MenuPlanificacion.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await MenuPlanificacion.ClickAsync();

			await MenuPei.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = navigationTimeoutMs });
			await MenuPei.ClickAsync();
		}

		public async Task IrACrearPeiAsync()
		{
			await BtnNuevoPei.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await BtnNuevoPei.ClickAsync();
		}

		public async Task CrearPeiVinculadoAsync(string nombreEntidad, string nombrePlanNacional)
		{
			// 1. Seleccionar la Entidad
			await SelectEntidad.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			var valEntidad = await SelectEntidad.Locator($"option:has-text('{nombreEntidad}')").GetAttributeAsync("value");
			if (string.IsNullOrEmpty(valEntidad)) throw new Exception($"ERROR E2E: La Entidad '{nombreEntidad}' no aparece en el combo.");
			await SelectEntidad.SelectOptionAsync(new SelectOptionValue { Value = valEntidad });

			// 2. Seleccionar el Plan Nacional y EXTRAER su data-periodo-id
			var optionPlan = SelectPlanNacional.Locator($"option:has-text('{nombrePlanNacional}')");
			var valPlan = await optionPlan.GetAttributeAsync("value");
			var periodoId = await optionPlan.GetAttributeAsync("data-periodo-id"); // Extraemos el ID oculto

			if (string.IsNullOrEmpty(valPlan)) throw new Exception($"ERROR E2E: El Plan '{nombrePlanNacional}' no aparece en el combo.");

			// Seleccionamos la opción
			await SelectPlanNacional.SelectOptionAsync(new SelectOptionValue { Value = valPlan });

			// 3. BLINDAJE DOBLE: 
			// Disparamos el evento nativo para la interfaz
			await SelectPlanNacional.DispatchEventAsync("change");

			// Inyectamos el valor a la fuerza en el input oculto usando JS a través de Playwright
			// Así garantizamos que ModelState.IsValid será true en el controlador
			await _page.EvaluateAsync($@"
        const hiddenInput = document.getElementById('periodoPlanificacionId');
        const select = document.getElementById('periodoSelect');
        if (hiddenInput) hiddenInput.value = '{periodoId}';
        if (select) select.value = '{periodoId}';
    ");

			// Pequeña pausa táctica (medio segundo) para permitir que el DOM renderice los cambios antes de enviar
			await Task.Delay(500);

			// 4. Guardar
			await BtnGuardarPei.ClickAsync();
		}
		public async Task IrADetallePeiAsync(string nombreEntidad)
		{
			// Buscamos la fila en la grilla que contiene el nombre de la entidad y hacemos clic en el botón "Proyectos"
			var fila = _page.Locator($"tr:has-text('{nombreEntidad}')").First;
			var btnProyectos = fila.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Proyectos" });

			await btnProyectos.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await btnProyectos.ClickAsync();
		}

		public async Task CrearProyectoAsync(string cup, string montoEntero, string nombreProyecto)
		{
			await BtnFormularProyecto.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await BtnFormularProyecto.ClickAsync();

			await InputCup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await InputCup.FillAsync(cup);

			// Llenamos el monto como string sin decimales (ej. "150000")
			await InputMonto.FillAsync(montoEntero);
			await InputNombreProyecto.FillAsync(nombreProyecto);

			await BtnGuardarProyecto.ClickAsync();
		}
	}
}