using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

public partial class PlanesEstrategico
{
    public int PlanEstrategicoId { get; set; }

    public string Entidad { get; set; } = null!;

    public int PeriodoInicio { get; set; }

    public int PeriodoFin { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<ProyectosInversion> ProyectosInversions { get; set; } = new List<ProyectosInversion>();
}
