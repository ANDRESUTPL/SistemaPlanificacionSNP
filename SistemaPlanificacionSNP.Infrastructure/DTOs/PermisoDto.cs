namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    /// <summary>
    /// DTO para Permiso
    /// </summary>
    public class PermisoDto
    {
        public int PermisoId { get; set; }
        public int PantallaId { get; set; }
        public string CodigoPermiso { get; set; } = null!;
        public string NombrePantalla { get; set; } = null!;
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    /// <summary>
    /// DTO para crear/actualizar Permiso
    /// </summary>
    public class PermisoCreateUpdateDto
    {
        public int PantallaId { get; set; }
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    /// <summary>
    /// DTO para RolPermiso
    /// </summary>
    public class RolPermisoDto
    {
        public int RolPermisoId { get; set; }
        public int RolId { get; set; }
        public int PantallaId { get; set; }
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    /// <summary>
    /// DTO para permisos en menú (jerarquía de pantallas)
    /// </summary>
    public class MenuPermisoDto
    {
        public int PantallaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Icono { get; set; } = null!;
        public string? Ruta { get; set; }
        public int? PantallaPadreId { get; set; }
        public int Orden { get; set; }
        public List<RolPermisoDto> RolPermisos { get; set; } = new();
        public List<MenuPermisoDto> Subpantallas { get; set; } = new();
    }
}
