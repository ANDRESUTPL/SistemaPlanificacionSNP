using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	public class MacroPlanNacionalApiDto
	{
		public int PlanNacionalId { get; set; }
		public string Nombre { get; set; } = string.Empty;
		public int? PeriodoPlanificacionId { get; set; }
		public int PeriodoInicio { get; set; }
		public int PeriodoFin { get; set; }
		public string Estado { get; set; } = string.Empty;
		public DateTime FechaCreacion { get; set; }
	}

	public class MacroObjetivoEstrategicoApiDto
	{
		public int ObjetivoEstrategicoId { get; set; }
		public int PlanNacionalId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string? Descripcion { get; set; }
	}

	public class MacroPlanNacionalDetalleApiDto : MacroPlanNacionalApiDto
	{
		public List<MacroObjetivoEstrategicoApiDto> Objetivos { get; set; } = new();
	}

	public class MacroPlanNacionalResumenApiDto
	{
		public int TotalPlanes { get; set; }
		public int TotalObjetivos { get; set; }
	}

	public class MacroPlanificacionIndexViewModel
	{
		public List<MacroPlanNacionalApiDto> PlanesNacionales { get; set; } = new();
		public MacroPlanNacionalResumenApiDto Resumen { get; set; } = new();
		public string? Buscar { get; set; }
		public bool PuedeLeer { get; set; }
		public bool PuedeCrear { get; set; }
		public bool PuedeEditar { get; set; }
		public bool PuedeEliminar { get; set; }
	}

	public class PlanNacionalCreateViewModel
	{
		[Display(Name = "Nombre del Plan Nacional")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(200, ErrorMessage = "No puede superar los 200 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Período de Planificación")]
		[Required(ErrorMessage = "Debe seleccionar un período de planificación.")]
		public int? PeriodoPlanificacionId { get; set; }

		public List<PeriodoPlanificacionApiDto> PeriodosDisponibles { get; set; } = new();

		[Display(Name = "Año de Inicio")]
		public int PeriodoInicio { get; set; } = DateTime.Now.Year;

		[Display(Name = "Año de Fin")]
		public int PeriodoFin { get; set; } = DateTime.Now.Year + 4;
	}

	public class ObjetivoMacroCreateViewModel
	{
		[Required]
		public int PlanNacionalId { get; set; }

		[Display(Name = "Código")]
		[Required(ErrorMessage = "El código es obligatorio.")]
		[StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
		public string Codigo { get; set; } = string.Empty;

		[Display(Name = "Nombre / Enunciado del Objetivo")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Descripción detallada (Opcional)")]
		[StringLength(600, ErrorMessage = "Máximo 600 caracteres.")]
		public string? Descripcion { get; set; }
	}

	public class ObjetivoMacroEditViewModel : ObjetivoMacroCreateViewModel
	{
		[Required]
		public int ObjetivoEstrategicoId { get; set; }
	}

	public class PlanNacionalEditViewModel
	{
		[Required]
		public int PlanNacionalId { get; set; }

		[Display(Name = "Nombre del Plan Nacional")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(200, ErrorMessage = "No puede superar los 200 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Período de Planificación")]
		[Required(ErrorMessage = "Debe seleccionar un período de planificación.")]
		public int? PeriodoPlanificacionId { get; set; }

		public List<PeriodoPlanificacionApiDto> PeriodosDisponibles { get; set; } = new();

		[Display(Name = "Año de Inicio")]
		public int PeriodoInicio { get; set; }

		[Display(Name = "Año de Fin")]
		public int PeriodoFin { get; set; }

		[Display(Name = "Estado")]
		[StringLength(30, ErrorMessage = "No puede superar los 30 caracteres.")]
		public string Estado { get; set; } = "Borrador";
	}
}