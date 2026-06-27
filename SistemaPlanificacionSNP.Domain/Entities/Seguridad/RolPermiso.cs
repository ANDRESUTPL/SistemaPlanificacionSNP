using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad;

public partial class RolPermiso
{
    public int RolPermisoId { get; set; }

    public int RolId { get; set; }

    public int PantallaId { get; set; }

    public bool Lectura { get; set; }

    public bool Creacion { get; set; }

    public bool Edicion { get; set; }

    public bool Eliminacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Pantalla Pantalla { get; set; } = null!;

    public virtual Rol Rol { get; set; } = null!;
}
