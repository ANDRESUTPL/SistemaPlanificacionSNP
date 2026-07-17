namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    /// <summary>
    /// DTO para Rol con permisos
    /// </summary>
    public class RolDto
    {
        public int RolId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public bool Activo { get; set; }
        public List<PermisoDto> Permisos { get; set; } = new();
    }

    /// <summary>
    /// DTO para catálogo de pantallas disponible en el catálogo de roles
    /// </summary>
    public class PantallaCatalogoDto
    {
        public int PantallaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Ruta { get; set; } = null!;
        public string Icono { get; set; } = null!;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>
    /// DTO para configurar permisos por pantalla en un rol
    /// </summary>
    public class RolPermisoConfigDto
    {
        public int PantallaId { get; set; }
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    /// <summary>
    /// DTO para crear/actualizar Rol
    /// </summary>
    public class RolCreateUpdateDto
    {
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public bool? Activo { get; set; }
        public List<RolPermisoConfigDto> Permisos { get; set; } = new();
        public List<int> PermisoIds { get; set; } = new();
    }

    /// <summary>
    /// DTO para asignar roles a usuario
    /// </summary>
    public class AsignarRolesDto
    {
        public List<int> RolIds { get; set; } = new();
    }
}
