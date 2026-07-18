using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	public class EntidadPublicaApiDto
	{
		public int EntidadPublicaId { get; set; }
		public string Nombre { get; set; } = string.Empty;
		public string Sigla { get; set; } = string.Empty;
		public string Tipo { get; set; } = string.Empty;
		public string NivelGobierno { get; set; } = string.Empty;
		public bool Activa { get; set; }
		public DateTime FechaCreacion { get; set; }
	}

	public class PeriodoPlanificacionApiDto
	{
		public int PeriodoPlanificacionId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public DateTime FechaInicio { get; set; }
		public DateTime FechaFin { get; set; }
		public bool Activo { get; set; }
	}

	public class InstitucionesIndexViewModel
	{
		public List<EntidadPublicaApiDto> Entidades { get; set; } = new();
		public List<PeriodoPlanificacionApiDto> Periodos { get; set; } = new();
		public string? Buscar { get; set; }
	}

	public class EntidadPublicaCreateViewModel
	{
		[Display(Name = "Nombre de la Entidad")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Sigla / Acrónimo")]
		[Required(ErrorMessage = "La sigla es obligatoria.")]
		[StringLength(20, ErrorMessage = "La sigla no puede superar los 20 caracteres.")]
		public string Sigla { get; set; } = string.Empty;

		[Display(Name = "Tipo de Entidad")]
		[Required(ErrorMessage = "Debe seleccionar un tipo de entidad.")]
		public string Tipo { get; set; } = string.Empty;

		[Display(Name = "Nivel de Gobierno")]
		[Required(ErrorMessage = "Debe seleccionar el nivel de gobierno.")]
		public string NivelGobierno { get; set; } = string.Empty;
	}

	public class PeriodoPlanificacionCreateViewModel
	{
		[Display(Name = "Código del Período")]
		[Required(ErrorMessage = "El código es obligatorio.")]
		public string Codigo { get; set; } = string.Empty;

		[Display(Name = "Nombre descriptivo")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Fecha de Inicio")]
		[Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
		[DataType(DataType.Date)]
		public DateTime? FechaInicio { get; set; }

		[Display(Name = "Fecha de Fin")]
		[Required(ErrorMessage = "La fecha de fin es obligatoria.")]
		[DataType(DataType.Date)]
		public DateTime? FechaFin { get; set; }
	}
}