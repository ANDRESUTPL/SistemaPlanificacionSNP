using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
    public class RolCatalogoListItemViewModel
    {
        public int RolId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int UsuariosAsignados { get; set; }
    }

    public class CatalogoRolesIndexViewModel
    {
        public List<RolCatalogoListItemViewModel> Roles { get; set; } = new();
        public string? Buscar { get; set; }
        public bool? Activo { get; set; }
    }

    public class CatalogoRolCreateViewModel
    {
        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "La descripción debe tener entre 5 y 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public List<CatalogoRolPermisoViewModel> PermisosPantalla { get; set; } = new();
    }

    public class CatalogoRolEditViewModel : CatalogoRolCreateViewModel
    {
        public int RolId { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }

    public class CatalogoRolPermisoViewModel
    {
        public int PantallaId { get; set; }
        public string NombrePantalla { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    public class PantallaCatalogoApiDto
    {
        public int PantallaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class RolPermisoApiDto
    {
        public int PantallaId { get; set; }
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    public class RolCreateUpdateApiDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool? Activo { get; set; }
        public List<RolPermisoApiDto> Permisos { get; set; } = new();
        public List<int> PermisoIds { get; set; } = new();
    }
}
