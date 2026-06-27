using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad;

public partial class AuditoriaTransaccional
{
    public int AuditoriaId { get; set; }

    public int UsuarioId { get; set; }

    public string Entidad { get; set; } = null!;

    public string TipoOperacion { get; set; } = null!;

    public int? IdRegistro { get; set; }

    public string? DatosAnteriores { get; set; }

    public string? DatosNuevos { get; set; }

    public DateTime FechaOperacion { get; set; }

    public string? Descripcion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
