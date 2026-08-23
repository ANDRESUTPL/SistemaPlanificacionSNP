using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;

public partial class Revisione
{
    public int RevisionId { get; set; }

    public string CodigoRevision { get; set; } = null!;

    public string Modulo { get; set; } = null!;

    public int? PlanEstrategicoId { get; set; }

    public int? ProyectoInversionId { get; set; }

    public int? EntidadPublicaId { get; set; }

    // Snapshot: ControlCalidad vive en otra base, no puede unir contra PlanificacionInstitucional.
    public string? EntidadNombre { get; set; }

    public string? CodigoProyecto { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaRevision { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<Auditoria> Auditoria { get; set; } = new List<Auditoria>();
}
