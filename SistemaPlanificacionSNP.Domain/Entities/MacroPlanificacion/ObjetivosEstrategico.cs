using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;

public partial class ObjetivosEstrategico
{
    public int ObjetivoEstrategicoId { get; set; }

    public int PlanNacionalId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual PlanesNacionalesDesarrollo PlanNacional { get; set; } = null!;
}
