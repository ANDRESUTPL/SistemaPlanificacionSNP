using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
	public class CatalogoApiDto
	{
		public int CatalogoId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string Descripcion { get; set; } = string.Empty;
		public bool Activo { get; set; }
		public DateTime FechaCreacion { get; set; }
		public List<ItemCatalogoApiDto> Items { get; set; } = new();
	}

	public class ItemCatalogoApiDto
	{
		public int ItemCatalogoId { get; set; }
		public int CatalogoId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string? Descripcion { get; set; }
		public int Orden { get; set; }
		public bool Activo { get; set; }
	}

	public class CatalogosIndexViewModel
	{
		public List<CatalogoApiDto> Catalogos { get; set; } = new();
		public string? Buscar { get; set; }
	}

	public class CatalogoCreateViewModel
	{
		[Display(Name = "Código Único")]
		[Required(ErrorMessage = "El código es obligatorio.")]
		[StringLength(30, ErrorMessage = "El código no puede superar los 30 caracteres.")]
		[RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "El código solo puede contener mayúsculas, números y guiones bajos.")]
		public string Codigo { get; set; } = string.Empty;

		[Display(Name = "Nombre del Catálogo")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		[StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Descripción")]
		[StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
		public string Descripcion { get; set; } = string.Empty;
	}

	public class ItemCatalogoCreateViewModel
	{
		[Required]
		public int CatalogoId { get; set; }

		public string CatalogoCodigo { get; set; } = string.Empty;

		[Display(Name = "Código del Ítem")]
		[Required(ErrorMessage = "El código es obligatorio.")]
		public string Codigo { get; set; } = string.Empty;

		[Display(Name = "Nombre / Valor")]
		[Required(ErrorMessage = "El nombre es obligatorio.")]
		public string Nombre { get; set; } = string.Empty;

		[Display(Name = "Descripción Opcional")]
		public string? Descripcion { get; set; }

		[Display(Name = "Orden de despliegue")]
		[Required]
		public int Orden { get; set; } = 1;
	}
}