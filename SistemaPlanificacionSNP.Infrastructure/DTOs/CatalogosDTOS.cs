using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
	public class CatalogoDto
	{
		public int CatalogoId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string Descripcion { get; set; } = string.Empty;
		public bool Activo { get; set; }
		public DateTime FechaCreacion { get; set; }
		public List<ItemCatalogoDto> Items { get; set; } = new();
	}

	public class CatalogoCreateDto
	{
		[Required] public string Codigo { get; set; } = string.Empty;
		[Required] public string Nombre { get; set; } = string.Empty;
		public string Descripcion { get; set; } = string.Empty;
	}

	public class CatalogoUpdateDto : CatalogoCreateDto
	{
		public bool Activo { get; set; }
	}

	// --- ITEM CATALOGO DTOs ---
	public class ItemCatalogoDto
	{
		public int ItemCatalogoId { get; set; }
		public int CatalogoId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string? Descripcion { get; set; }
		public int Orden { get; set; }
		public bool Activo { get; set; }
	}

	public class ItemCatalogoCreateDto
	{
		[Required] public int CatalogoId { get; set; }
		[Required] public string Codigo { get; set; } = string.Empty;
		[Required] public string Nombre { get; set; } = string.Empty;
		public string? Descripcion { get; set; }
		public int Orden { get; set; }
	}

	public class ItemCatalogoUpdateDto : ItemCatalogoCreateDto
	{
		public bool Activo { get; set; }
	}

	// --- PERIODO PLANIFICACION DTOs ---
	public class PeriodoPlanificacionDto
	{
		public int PeriodoPlanificacionId { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public DateTime FechaInicio { get; set; }
		public DateTime FechaFin { get; set; }
		public bool Activo { get; set; }
	}

	public class PeriodoPlanificacionCreateUpdateDto
	{
		[Required] public string Codigo { get; set; } = string.Empty;
		[Required] public string Nombre { get; set; } = string.Empty;
		[Required] public DateTime FechaInicio { get; set; }
		[Required] public DateTime FechaFin { get; set; }
		public bool Activo { get; set; } = true;
	}

	// --- ENTIDAD PUBLICA DTOs ---



}
