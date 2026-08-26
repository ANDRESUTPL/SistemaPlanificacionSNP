using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	public class ReporteEjecutivoViewModel
	{
		public int TotalProyectos { get; set; }
		public int ProyectosEnEjecucion { get; set; }
		public int ProyectosCompletados { get; set; }
		public decimal PresupuestoTotal { get; set; }
		public decimal PresupuestoEjecutado { get; set; }
		public double PorcentajeAvanceGlobal { get; set; }

		// Datos para gráficos
		public List<string> EtiquetasEstado { get; set; } = new();
		public List<int> ValoresEstado { get; set; } = new();
		public List<string> EtiquetasEntidad { get; set; } = new();
		public List<decimal> ValoresPresupuesto { get; set; } = new();
	}

	public class AvanceProyectoViewModel
	{
		public int ProyectoId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string Entidad { get; set; } = string.Empty;
		public decimal PresupuestoAsignado { get; set; }
		public string Estado { get; set; } = string.Empty;
		public string Observaciones { get; set; } = string.Empty;
		public decimal AvanceFisico { get; set; }
		public decimal AvanceFinanciero { get; set; }
		//public DateTime FechaUltimaActualizacion { get; set; }
	}

	public class ListadoAvancesViewModel : IPermisosViewModel
	{
		public List<AvanceProyectoViewModel> Proyectos { get; set; } = new();
		public string? Buscar { get; set; }
		public int Page { get; set; } = 1;
		public int TotalPages { get; set; } = 1;

		public bool PuedeLeer { get; set; }
		public bool PuedeCrear { get; set; }
		public bool PuedeEditar { get; set; }
		public bool PuedeEliminar { get; set; }
	}

	public class RegistroAvanceViewModel
	{
		[Required]
		public int ProyectoId { get; set; }

		public string NombreProyecto { get; set; } = string.Empty;

		[Display(Name = "Estado Actual")]
		[Required(ErrorMessage = "El estado es requerido.")]
		public string Estado { get; set; } = string.Empty;

		[Display(Name = "Avance Físico (%)")]
		[Range(0, 100, ErrorMessage = "El avance debe estar entre 0 y 100.")]
		public decimal AvanceFisico { get; set; }

		[Display(Name = "Avance Financiero (%)")]
		[Range(0, 100, ErrorMessage = "El avance debe estar entre 0 y 100.")]
		public decimal AvanceFinanciero { get; set; }

		[Display(Name = "Observaciones de Evaluación")]
		[StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
		public string? Observaciones { get; set; }
	}
}