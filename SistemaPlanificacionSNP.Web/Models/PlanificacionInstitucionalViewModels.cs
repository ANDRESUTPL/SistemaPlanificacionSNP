using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	// --- DTOs que consumen la API ---
	public class PlanesEstrategicoApiDto
	{
		public int PlanEstrategicoId { get; set; }
		public string Entidad { get; set; } = string.Empty;
		public int PeriodoInicio { get; set; }
		public int PeriodoFin { get; set; }
		public string Estado { get; set; } = string.Empty;
		public DateTime FechaCreacion { get; set; }
	}

	public class ProyectosInversionApiDto
	{
		public int ProyectoInversionId { get; set; }
		public int PlanEstrategicoId { get; set; }
		public string CodigoProyecto { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public decimal Monto { get; set; }
		public string Estado { get; set; } = string.Empty;
	}

	public class PlanesEstrategicoDetailApiDto : PlanesEstrategicoApiDto
	{
		public List<ProyectosInversionApiDto> Proyectos { get; set; } = new();
	}

	public class PlanificacionDashboardApiDto
	{
		public int TotalPlanes { get; set; }
		public int TotalPlanesActivos { get; set; }
		public int TotalProyectos { get; set; }
		public int TotalProyectosActivos { get; set; }
		public decimal MontoTotalProyectosActivos { get; set; }
	}

	// --- ViewModels para las Vistas MVC ---
	public class PlanificacionIndexViewModel
	{
		public List<PlanesEstrategicoApiDto> Planes { get; set; } = new();
		public PlanificacionDashboardApiDto Dashboard { get; set; } = new();
		public string? Buscar { get; set; }
		public int Page { get; set; } = 1;
		public int TotalPages { get; set; } = 1;
	}

	public class PlanEstrategicoCreateViewModel
	{
		[Display(Name = "Entidad Pública")]
		[Required(ErrorMessage = "Debe seleccionar una entidad.")]
		public string Entidad { get; set; } = string.Empty;

		[Display(Name = "Año de Inicio")]
		[Required(ErrorMessage = "Requerido.")]
		[Range(2020, 2100, ErrorMessage = "Año inválido.")]
		public int PeriodoInicio { get; set; } = DateTime.Now.Year;

		[Display(Name = "Año de Fin")]
		[Required(ErrorMessage = "Requerido.")]
		[Range(2020, 2100, ErrorMessage = "Año inválido.")]
		public int PeriodoFin { get; set; } = DateTime.Now.Year + 4;

		// Para poblar el combo box de entidades desde Parametrización
		public List<EntidadPublicaApiDto> EntidadesDisponibles { get; set; } = new();
	}

	public class ProyectoInversionCreateViewModel
	{
		[Required]
		public int PlanEstrategicoId { get; set; }

		[Display(Name = "Código Único de Proyecto (CUP)")]
		[Required(ErrorMessage = "El código es obligatorio.")]
		[StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
		public string CodigoProyecto { get; set; } = string.Empty;

		[Display(Name = "Nombre del Proyecto")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Monto de Inversión Asignado ($)")]
		[Required(ErrorMessage = "El monto es obligatorio.")]
		[Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
		[DataType(DataType.Currency)]
		public decimal Monto { get; set; }
	}
}