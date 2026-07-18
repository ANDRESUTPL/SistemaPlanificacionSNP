using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	public class MacroPlanNacionalApiDto
	{
		public int PlanNacionalId { get; set; }
		public string Nombre { get; set; } = string.Empty;
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
	}

	public class PlanNacionalCreateViewModel
	{
		[Display(Name = "Nombre del Plan Nacional")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(200, ErrorMessage = "No puede superar los 200 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Año de Inicio")]
		[Required(ErrorMessage = "Requerido.")]
		[Range(2000, 2100, ErrorMessage = "Año inválido.")]
		public int PeriodoInicio { get; set; } = DateTime.Now.Year;

		[Display(Name = "Año de Fin")]
		[Required(ErrorMessage = "Requerido.")]
		[Range(2000, 2100, ErrorMessage = "Año inválido.")]
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
}