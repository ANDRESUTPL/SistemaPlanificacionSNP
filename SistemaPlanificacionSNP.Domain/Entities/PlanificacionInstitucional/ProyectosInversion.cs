using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

public partial class ProyectosInversion
{
    public int ProyectoInversionId { get; set; }

    public int PlanEstrategicoId { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public decimal Monto { get; set; }

    public string Estado { get; set; } = null!;

	public decimal? AvanceFisico { get; set; }
	public decimal? AvanceFinanciero { get; set; }
	public string? Observaciones { get; set; }

	public virtual PlanesEstrategico PlanEstrategico { get; set; } = null!;

    public virtual ICollection<RespaldoEjecucion> RespaldosEjecucion { get; set; } = new List<RespaldoEjecucion>();
}
