using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad;

public partial class Role
{
    public int RolId { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<UsuarioRole> UsuarioRoles { get; set; } = new List<UsuarioRole>();
}
