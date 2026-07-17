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
    /// DTO para crear/actualizar Rol
    /// </summary>
    public class RolCreateUpdateDto
    {
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public List<int> PermisoIds { get; set; } = new();
    }

    /// <summary>
    /// DTO para asignar roles a usuario
    /// </summary>
    public class AsignarRolesDto
    {
        public int UsuarioId { get; set; }
        public List<int> RolIds { get; set; } = new();
    }
}
