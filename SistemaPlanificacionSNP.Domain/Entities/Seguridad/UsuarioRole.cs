using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad;

public partial class UsuarioRole
{
    public int UsuarioRolId { get; set; }

    public int UsuarioId { get; set; }

    public int RolId { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public virtual Role Rol { get; set; } = null!;

    public virtual Usuario1 Usuario { get; set; } = null!;
}
