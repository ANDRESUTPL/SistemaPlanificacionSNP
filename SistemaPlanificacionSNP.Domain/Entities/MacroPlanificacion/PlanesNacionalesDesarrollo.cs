using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;

public partial class PlanesNacionalesDesarrollo
{
    public int PlanNacionalId { get; set; }

    public string Nombre { get; set; } = null!;

    public int PeriodoInicio { get; set; }

    public int PeriodoFin { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<ObjetivosEstrategico> ObjetivosEstrategicos { get; set; } = new List<ObjetivosEstrategico>();
}
