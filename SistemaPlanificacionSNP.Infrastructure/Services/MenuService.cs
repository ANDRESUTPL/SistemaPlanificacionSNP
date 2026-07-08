using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Infrastructure.Services
{
    public interface IMenuService
    {
        Task<List<MenuPermisoDto>> ObtenerMenuParaUsuarioAsync(int usuarioId);
    }

    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MenuService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<MenuPermisoDto>> ObtenerMenuParaUsuarioAsync(int usuarioId)
        {
            var usuario = await _unitOfWork.Usuarios.GetWithRolesAsync(usuarioId);
            if (usuario == null)
            {
                throw new InvalidOperationException("Usuario no encontrado");
            }

            var repo = _unitOfWork.GetRepository<Pantalla>();
            var pantallas = await repo.FindAsync(p => p.Activo);

            var permisosUsuario = ConstruirPermisosUsuario(usuario);
            var pantallasRaiz = pantallas.Where(p => p.PantallaPadrId == null).ToList();

            return BuildMenuTree(pantallasRaiz, permisosUsuario);
        }

        private static Dictionary<int, PermisoDto> ConstruirPermisosUsuario(Usuario usuario)
        {
            var permisosUsuario = new Dictionary<int, PermisoDto>();

            foreach (var usuarioRol in usuario.UsuarioRols)
            {
                foreach (var permiso in usuarioRol.Rol.RolPermisos)
                {
                    if (!permisosUsuario.ContainsKey(permiso.PantallaId))
                    {
                        permisosUsuario[permiso.PantallaId] = new PermisoDto
                        {
                            PermisoId = permiso.RolPermisoId,
                            PantallaId = permiso.PantallaId,
                            CodigoPermiso = permiso.Pantalla.Nombre,
                            NombrePantalla = permiso.Pantalla.Nombre,
                            Lectura = permiso.Lectura,
                            Creacion = permiso.Creacion,
                            Edicion = permiso.Edicion,
                            Eliminacion = permiso.Eliminacion
                        };
                    }
                    else
                    {
                        permisosUsuario[permiso.PantallaId].Lectura |= permiso.Lectura;
                        permisosUsuario[permiso.PantallaId].Creacion |= permiso.Creacion;
                        permisosUsuario[permiso.PantallaId].Edicion |= permiso.Edicion;
                        permisosUsuario[permiso.PantallaId].Eliminacion |= permiso.Eliminacion;
                    }
                }
            }

            return permisosUsuario;
        }

        private static List<MenuPermisoDto> BuildMenuTree(
            List<Pantalla> pantallasActuales,
            Dictionary<int, PermisoDto> permisosUsuario)
        {
            var menu = new List<MenuPermisoDto>();

            foreach (var pantalla in pantallasActuales.OrderBy(p => p.Orden))
            {
                if (!permisosUsuario.TryGetValue(pantalla.PantallaId, out var permiso) || !permiso.Lectura)
                {
                    continue;
                }

                var menuItem = new MenuPermisoDto
                {
                    PantallaId = pantalla.PantallaId,
                    Nombre = pantalla.Nombre,
                    Icono = pantalla.Icono,
                    Ruta = pantalla.Ruta,
                    PantallaPadreId = pantalla.PantallaPadrId,
                    Orden = pantalla.Orden,
                    RolPermisos = new List<RolPermisoDto>
                    {
                        new RolPermisoDto
                        {
                            RolPermisoId = permiso.PermisoId,
                            RolId = 0,
                            PantallaId = permiso.PantallaId,
                            Lectura = permiso.Lectura,
                            Creacion = permiso.Creacion,
                            Edicion = permiso.Edicion,
                            Eliminacion = permiso.Eliminacion
                        }
                    },
                    Subpantallas = BuildMenuTree(pantalla.InversePantallaPadr.ToList(), permisosUsuario)
                };

                menu.Add(menuItem);
            }

            return menu;
        }
    }
}
