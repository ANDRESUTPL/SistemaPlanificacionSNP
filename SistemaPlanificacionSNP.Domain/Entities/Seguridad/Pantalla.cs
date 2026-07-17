using System;
using System.Collections.Generic;

namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad;

public partial class Pantalla
{
    public int PantallaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Ruta { get; set; } = null!;

    public string Icono { get; set; } = null!;

    public int? PantallaPadrId { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Pantalla> InversePantallaPadr { get; set; } = new List<Pantalla>();

    public virtual Pantalla? PantallaPadr { get; set; }

    public virtual ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
