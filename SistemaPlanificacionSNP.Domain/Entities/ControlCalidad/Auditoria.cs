using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;

public partial class Auditoria
{
    public int AuditoriaId { get; set; }

    public int RevisionId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Resultado { get; set; } = null!;

    public string Responsable { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual Revisione Revision { get; set; } = null!;
}
