using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Controllers
{
    /// <summary>
    /// Controlador para gestión de Usuarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHashService _passwordService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            IUnitOfWork unitOfWork,
            IPasswordHashService passwordService,
            IAuditoriaService auditoriaService,
            IMapper mapper,
            ILogger<UsuariosController> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtener todos los usuarios (requiere permiso de Lectura)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UsuarioDto>>>> GetAll()
        {
            try
            {
                var usuarios = await _unitOfWork.Usuarios.GetAllAsync();
                var usuariosDto = _mapper.Map<List<UsuarioDto>>(usuarios);
                return Ok(ApiResponse<List<UsuarioDto>>.SuccessWith(usuariosDto, $"{usuariosDto.Count} usuarios encontrados"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAll: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<List<UsuarioDto>>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Obtener usuario por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UsuarioDto>>> GetById(int id)
        {
            try
            {
                var usuario = await _unitOfWork.Usuarios.GetWithRolesAsync(id);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<UsuarioDto>.FailureWith("Usuario no encontrado"));
                }

                var usuarioDto = _mapper.Map<UsuarioDto>(usuario);
                return Ok(ApiResponse<UsuarioDto>.SuccessWith(usuarioDto));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetById: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<UsuarioDto>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Crear nuevo usuario (requiere permiso de Creación)
        /// </summary>
        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<UsuarioDto>>> Crear([FromBody] UsuarioCreateDto usuarioCreateDto)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(usuarioCreateDto.NombreUsuario) ||
                    string.IsNullOrWhiteSpace(usuarioCreateDto.Email) ||
                    string.IsNullOrWhiteSpace(usuarioCreateDto.Password))
                {
                    return BadRequest(ApiResponse<UsuarioDto>.FailureWith("Datos requeridos incompletos"));
                }

                // Validar que no exista usuario con ese nombre
                if (await _unitOfWork.Usuarios.ExisteNombreUsuarioAsync(usuarioCreateDto.NombreUsuario))
                {
                    return BadRequest(ApiResponse<UsuarioDto>.FailureWith("El nombre de usuario ya existe"));
                }

                // Validar que no exista usuario con ese email
                if (await _unitOfWork.Usuarios.ExisteEmailAsync(usuarioCreateDto.Email))
                {
                    return BadRequest(ApiResponse<UsuarioDto>.FailureWith("El email ya está registrado"));
                }

                // Validar longitud de contraseña
                if (usuarioCreateDto.Password.Length < 8)
                {
                    return BadRequest(ApiResponse<UsuarioDto>.FailureWith("La contraseña debe tener al menos 8 caracteres"));
                }

                // Obtener UsuarioId del token para auditoría
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int usuarioCreadorId))
                {
                    return Unauthorized(ApiResponse<UsuarioDto>.FailureWith("Token inválido"));
                }

                // Crear usuario
                var usuario = _mapper.Map<Usuario>(usuarioCreateDto);
                usuario.PasswordHash = _passwordService.HashPassword(usuarioCreateDto.Password);
                usuario.UsuarioCreacionId = usuarioCreadorId;
                usuario.FechaCreacion = DateTime.UtcNow;
                usuario.Activo = true;

                await _unitOfWork.Usuarios.AddAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarCreacionAsync(
                    usuarioCreadorId,
                    "Usuario",
                    usuario.UsuarioId,
                    new { usuario.NombreUsuario, usuario.Email, usuario.Nombre, usuario.Apellido }
                );

                var usuarioDto = _mapper.Map<UsuarioDto>(usuario);
                _logger.LogInformation($"New user created: {usuario.NombreUsuario}");

                return CreatedAtAction(nameof(GetById), new { id = usuario.UsuarioId },
                    ApiResponse<UsuarioDto>.SuccessWith(usuarioDto, "Usuario creado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Crear: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<UsuarioDto>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Actualizar usuario (requiere permiso de Edición)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UsuarioDto>>> Actualizar(int id, [FromBody] UsuarioUpdateDto usuarioUpdateDto)
        {
            try
            {
                // Buscar usuario
                var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<UsuarioDto>.FailureWith("Usuario no encontrado"));
                }

                // Obtener UsuarioId del token para auditoría
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int usuarioEditorId))
                {
                    return Unauthorized(ApiResponse<UsuarioDto>.FailureWith("Token inválido"));
                }

                // Guardar datos anteriores para auditoría
                var datoAnterior = new
                {
                    usuario.Email,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Activo
                };

                // Actualizar campos
                if (!string.IsNullOrWhiteSpace(usuarioUpdateDto.Email) && usuarioUpdateDto.Email != usuario.Email)
                {
                    if (await _unitOfWork.Usuarios.ExisteEmailAsync(usuarioUpdateDto.Email))
                    {
                        return BadRequest(ApiResponse<UsuarioDto>.FailureWith("El email ya está en uso"));
                    }
                    usuario.Email = usuarioUpdateDto.Email;
                }

                if (!string.IsNullOrWhiteSpace(usuarioUpdateDto.Nombre))
                    usuario.Nombre = usuarioUpdateDto.Nombre;

                if (!string.IsNullOrWhiteSpace(usuarioUpdateDto.Apellido))
                    usuario.Apellido = usuarioUpdateDto.Apellido;

                if (usuarioUpdateDto.Activo.HasValue)
                    usuario.Activo = usuarioUpdateDto.Activo.Value;

                usuario.FechaUltimaActualizacion = DateTime.UtcNow;
                usuario.UsuarioUltimaActualizacionId = usuarioEditorId;

                await _unitOfWork.Usuarios.UpdateAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarActualizacionAsync(
                    usuarioEditorId,
                    "Usuario",
                    id,
                    datoAnterior,
                    new { usuario.Email, usuario.Nombre, usuario.Apellido, usuario.Activo }
                );

                var usuarioDto = _mapper.Map<UsuarioDto>(usuario);
                _logger.LogInformation($"User updated: {usuario.NombreUsuario}");

                return Ok(ApiResponse<UsuarioDto>.SuccessWith(usuarioDto, "Usuario actualizado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Actualizar: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<UsuarioDto>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Eliminar usuario (requiere permiso de Eliminación)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Eliminar(int id)
        {
            try
            {
                // Buscar usuario
                var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Usuario no encontrado"));
                }

                // Obtener UsuarioId del token para auditoría
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int usuarioBorradoId))
                {
                    return Unauthorized(ApiResponse<string>.FailureWith("Token inválido"));
                }

                // Prevenir que un usuario se elimine a sí mismo
                if (id == usuarioBorradoId)
                {
                    return BadRequest(ApiResponse<string>.FailureWith("No puedes eliminar tu propia cuenta"));
                }

                // Obtener datos para auditoría antes de eliminar
                var datosEliminados = new { usuario.NombreUsuario, usuario.Email, usuario.Nombre, usuario.Apellido };

                await _unitOfWork.Usuarios.RemoveAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarEliminacionAsync(
                    usuarioBorradoId,
                    "Usuario",
                    id,
                    datosEliminados
                );

                _logger.LogInformation($"User deleted: {usuario.NombreUsuario}");
                return Ok(ApiResponse<string>.Success("Usuario eliminado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Eliminar: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Obtener menú dinámico del usuario autenticado (jerarquía de pantallas con permisos)
        /// </summary>
        [HttpGet("menu/actual")]
        public async Task<ActionResult<ApiResponse<List<MenuPermisoDto>>>> ObtenerMenuActual()
        {
            try
            {
                // Obtener UsuarioId del token
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<List<MenuPermisoDto>>.FailureWith("Token inválido"));
                }

                // Obtener usuario con roles
                var usuario = await _unitOfWork.Usuarios.GetWithRolesAsync(userId);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<List<MenuPermisoDto>>.FailureWith("Usuario no encontrado"));
                }

                // Obtener todas las pantallas del sistema
                var repo = _unitOfWork.GetRepository<Pantalla>();
                var pantallas = await repo.FindAsync(p => p.Activa);

                // Obtener permisos del usuario basados en sus roles
                var permisosUsuario = new Dictionary<int, PermisoDto>();

                foreach (var usuarioRol in usuario.UsuariosRoles)
                {
                    foreach (var permiso in usuarioRol.Rol.RolesPermisos)
                    {
                        // Tomar el permiso más permisivo si ya existe
                        if (!permisosUsuario.ContainsKey(permiso.Permiso.PantallaId))
                        {
                            permisosUsuario[permiso.Permiso.PantallaId] = new PermisoDto
                            {
                                PermisoId = permiso.Permiso.PermisoId,
                                PantallaId = permiso.Permiso.PantallaId,
                                CodigoPermiso = permiso.Permiso.Pantalla.Codigo,
                                NombrePantalla = permiso.Permiso.Pantalla.Nombre,
                                Lectura = permiso.Permiso.Lectura,
                                Creacion = permiso.Permiso.Creacion,
                                Edicion = permiso.Permiso.Edicion,
                                Eliminacion = permiso.Permiso.Eliminacion
                            };
                        }
                        else
                        {
                            // Combinar permisos (OR lógico)
                            permisosUsuario[permiso.Permiso.PantallaId].Lectura |= permiso.Permiso.Lectura;
                            permisosUsuario[permiso.Permiso.PantallaId].Creacion |= permiso.Permiso.Creacion;
                            permisosUsuario[permiso.Permiso.PantallaId].Edicion |= permiso.Permiso.Edicion;
                            permisosUsuario[permiso.Permiso.PantallaId].Eliminacion |= permiso.Permiso.Eliminacion;
                        }
                    }
                }

                // Construir árbol de menú (solo pantallas donde usuario tiene lectura)
                var menuRaiz = BuildMenuTree(
                    pantallas.Where(p => p.PantallaPadreId == null).ToList(),
                    pantallas,
                    permisosUsuario
                );

                return Ok(ApiResponse<List<MenuPermisoDto>>.SuccessWith(menuRaiz, "Menú obtenido"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ObtenerMenuActual: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<List<MenuPermisoDto>>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Construir árbol de menú recursivamente
        /// </summary>
        private List<MenuPermisoDto> BuildMenuTree(
            List<Pantalla> pantallasActuales,
            IEnumerable<Pantalla> todasPantallas,
            Dictionary<int, PermisoDto> permisosUsuario)
        {
            var menu = new List<MenuPermisoDto>();

            foreach (var pantalla in pantallasActuales.OrderBy(p => p.Orden))
            {
                // Solo incluir si el usuario tiene al menos permiso de lectura
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
                    PantallaPadreId = pantalla.PantallaPadreId,
                    Orden = pantalla.Orden,
                    Permiso = permiso,
                    Subpantallas = BuildMenuTree(
                        pantalla.PantallasHijas.ToList(),
                        todasPantallas,
                        permisosUsuario
                    )
                };

                menu.Add(menuItem);
            }

            return menu;
        }

        /// <summary>
        /// Asignar roles a un usuario (requiere permiso especial)
        /// </summary>
        [HttpPost("{usuarioId}/asignar-roles")]
        public async Task<ActionResult<ApiResponse<string>>> AsignarRoles(int usuarioId, [FromBody] AsignarRolesDto asignarRolesDto)
        {
            try
            {
                // Buscar usuario
                var usuario = await _unitOfWork.Usuarios.GetWithRolesAsync(usuarioId);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Usuario no encontrado"));
                }

                // Obtener UsuarioId del token para auditoría
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int usuarioEditadoId))
                {
                    return Unauthorized(ApiResponse<string>.FailureWith("Token inválido"));
                }

                // Obtener rol repository
                var rolRepo = _unitOfWork.GetRepository<Rol>();

                // Remover roles actuales
                usuario.UsuariosRoles.Clear();

                // Agregar nuevos roles
                var rolesNuevos = new List<Rol>();
                foreach (var rolId in asignarRolesDto.RolIds)
                {
                    var rol = await rolRepo.GetByIdAsync(rolId);
                    if (rol != null)
                    {
                        usuario.UsuariosRoles.Add(new UsuarioRol
                        {
                            UsuarioId = usuarioId,
                            RolId = rolId,
                            Rol = rol
                        });
                        rolesNuevos.Add(rol);
                    }
                }

                await _unitOfWork.Usuarios.UpdateAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarActualizacionAsync(
                    usuarioEditadoId,
                    "UsuarioRol",
                    usuarioId,
                    new { Roles = "modificados" },
                    new { Roles = string.Join(",", rolesNuevos.Select(r => r.Nombre)) }
                );

                _logger.LogInformation($"Roles assigned to user: {usuario.NombreUsuario}");
                return Ok(ApiResponse<string>.Success("Roles asignados exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AsignarRoles: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }
    }
}
