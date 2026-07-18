using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	public class AuditoriaApiDto
	{
		public int AuditoriaId { get; set; }
		public int RevisionId { get; set; }
		public string Tipo { get; set; } = string.Empty;
		public string Resultado { get; set; } = string.Empty;
		public string Responsable { get; set; } = string.Empty;
		public DateTime FechaRegistro { get; set; }
	}

	public class RevisioneApiDto
	{
		public int RevisionId { get; set; }
		public string CodigoRevision { get; set; } = string.Empty;
		public string Modulo { get; set; } = string.Empty;
		public string Estado { get; set; } = string.Empty;
		public DateTime FechaRevision { get; set; }
		public string? Observaciones { get; set; }
		public List<AuditoriaApiDto> Auditorias { get; set; } = new();
	}

	public class ControlCalidadDashboardApiDto
	{
		public int TotalRevisiones { get; set; }
		public int RevisionesPendientes { get; set; }
		public int RevisionesAprobadas { get; set; }
		public int RevisionesRechazadas { get; set; }
		public int TotalAuditorias { get; set; }
		public int AuditoriasConformes { get; set; }
		public int AuditoriasNoConformes { get; set; }
	}

	public class ControlCalidadIndexViewModel
	{
		public List<RevisioneApiDto> Revisiones { get; set; } = new();
		public ControlCalidadDashboardApiDto Dashboard { get; set; } = new();
		public string? Buscar { get; set; }
		public int Page { get; set; } = 1;
		public int TotalPages { get; set; } = 1;
	}

	public class RevisionCreateViewModel
	{
		[Display(Name = "Código Único de Revisión")]
		[Required(ErrorMessage = "El código es obligatorio.")]
		[StringLength(40, ErrorMessage = "Máximo 40 caracteres.")]
		public string CodigoRevision { get; set; } = string.Empty;

		[Display(Name = "Módulo o Entidad Evaluada")]
		[Required(ErrorMessage = "Debe indicar qué se está revisando (ej. PEI-MSP-2026).")]
		[StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
		public string Modulo { get; set; } = string.Empty;

		[Display(Name = "Estado de la Revisión")]
		[Required(ErrorMessage = "El estado es obligatorio.")]
		public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobada, Rechazada

		[Display(Name = "Observaciones (Opcional)")]
		[StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
		public string? Observaciones { get; set; }
	}

	public class AuditoriaCreateViewModel
	{
		[Required]
		public int RevisionId { get; set; }

		[Display(Name = "Tipo de Auditoría")]
		[Required(ErrorMessage = "Debe seleccionar un tipo de auditoría.")]
		public string Tipo { get; set; } = string.Empty; // Interna, Externa, Seguimiento, Cumplimiento

		[Display(Name = "Resultado / Dictamen")]
		[Required(ErrorMessage = "Debe seleccionar un resultado.")]
		public string Resultado { get; set; } = string.Empty; // Conforme, No Conforme, Observado
	}
}