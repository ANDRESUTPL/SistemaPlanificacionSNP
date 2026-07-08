using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;

public partial class Revisione
{
    public int RevisionId { get; set; }

    public string CodigoRevision { get; set; } = null!;

    public string Modulo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaRevision { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<Auditoria> Auditoria { get; set; } = new List<Auditoria>();
}
