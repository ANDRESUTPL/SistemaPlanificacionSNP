using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	// --- DTOs que consumen la API ---
	public class PlanesEstrategicoApiDto
	{
		public int PlanEstrategicoId { get; set; }
		public string Entidad { get; set; } = string.Empty;
		public int? EntidadPublicaId { get; set; }
		public int? PlanNacionalId { get; set; }
		public int? PeriodoPlanificacionId { get; set; }
		public int PeriodoInicio { get; set; }
		public int PeriodoFin { get; set; }
		public string Estado { get; set; } = string.Empty;
		public DateTime FechaCreacion { get; set; }
		public int CantidadProyectos { get; set; }
	}

	public class ProyectosInversionApiDto
	{
		public int ProyectoInversionId { get; set; }
		public int PlanEstrategicoId { get; set; }
		public string CodigoProyecto { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public decimal Monto { get; set; }
		public string Estado { get; set; } = string.Empty;
		public decimal? AvanceFisico { get; set; }
		public decimal? AvanceFinanciero { get; set; }
		public List<RespaldoEjecucionApiDto> RespaldosEjecucion { get; set; } = new();
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
	public class PlanificacionIndexViewModel : IPermisosViewModel
	{
		public List<PlanesEstrategicoApiDto> Planes { get; set; } = new();
		public PlanificacionDashboardApiDto Dashboard { get; set; } = new();
		public string? Buscar { get; set; }
		public int Page { get; set; } = 1;
		public int TotalPages { get; set; } = 1;
		
		public bool PuedeLeer { get; set; }
		public bool PuedeCrear { get; set; }
		public bool PuedeEditar { get; set; }
		public bool PuedeEliminar { get; set; }
	}

	public class PlanEstrategicoCreateViewModel
	{
		[Display(Name = "Entidad Pública")]
		[Required(ErrorMessage = "Debe seleccionar una entidad.")]
		public int? EntidadPublicaId { get; set; }

		[Display(Name = "Plan Nacional de Desarrollo")]
		[Required(ErrorMessage = "Debe seleccionar un Plan Nacional de Desarrollo.")]
		public int? PlanNacionalId { get; set; }

		[Display(Name = "Período de Planificación")]
		[Required(ErrorMessage = "Debe seleccionar un período de planificación.")]
		public int? PeriodoPlanificacionId { get; set; }

		public List<PeriodoPlanificacionApiDto> PeriodosDisponibles { get; set; } = new();
		public List<MacroPlanNacionalApiDto> PlanesNacionalesDisponibles { get; set; } = new();

		[Display(Name = "Año de Inicio")]
		public int PeriodoInicio { get; set; } = DateTime.Now.Year;

		[Display(Name = "Año de Fin")]
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

		[Display(Name = "Respaldos de ejecución")]
		public List<IFormFile> RespaldosEjecucion { get; set; } = new();
	}

	public class ProyectoInversionEditViewModel : ProyectoInversionCreateViewModel
	{
		[Required]
		public int ProyectoInversionId { get; set; }

		[Display(Name = "Estado del Proyecto")]
		[Required(ErrorMessage = "El estado es obligatorio.")]
		[StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
		public string Estado { get; set; } = string.Empty;
		public List<RespaldoEjecucionApiDto> Respaldos { get; set; } = new();
	}

	public class RespaldoEjecucionApiDto
	{
		public int RespaldoEjecucionId { get; set; }
		public string NombreArchivo { get; set; } = string.Empty;
		public long TamanoBytes { get; set; }
		public DateTime FechaCarga { get; set; }
	}

	public class PlanEstrategicoEditViewModel : PlanEstrategicoCreateViewModel
	{
		[Required]
		public int PlanEstrategicoId { get; set; }

		[Display(Name = "Estado")]
		[Required(ErrorMessage = "El estado es obligatorio.")]
		[StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
		public string Estado { get; set; } = "Borrador";
	}
}