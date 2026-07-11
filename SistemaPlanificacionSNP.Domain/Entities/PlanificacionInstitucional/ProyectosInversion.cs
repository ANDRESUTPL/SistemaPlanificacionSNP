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

    public virtual PlanesEstrategico PlanEstrategico { get; set; } = null!;
}
