using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Controllers
{
    /// <summary>
    /// Controlador para gestion del catalogo de roles.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IMapper _mapper;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IAuditoriaService auditoriaService,
            IMapper mapper,
            ILogger<RolesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RolDto>>>> GetAll()
        {
            try
            {
                var roles = await _context.Rols
                    .Include(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Pantalla)
                    .OrderBy(r => r.Nombre)
                    .ToListAsync();

                var rolesDto = _mapper.Map<List<RolDto>>(roles);
                return Ok(ApiResponse<List<RolDto>>.SuccessWith(rolesDto, $"{rolesDto.Count} rol(es) encontrados"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.GetAll: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<List<RolDto>>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("activos")]
        public async Task<ActionResult<ApiResponse<List<RolDto>>>> GetActivos()
        {
            try
            {
                var roles = await _context.Rols
                    .Include(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Pantalla)
                    .Where(r => r.Activo)
                    .OrderBy(r => r.Nombre)
                    .ToListAsync();

                var rolesDto = _mapper.Map<List<RolDto>>(roles);
                return Ok(ApiResponse<List<RolDto>>.SuccessWith(rolesDto, $"{rolesDto.Count} rol(es) activos encontrados"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.GetActivos: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<List<RolDto>>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("pantallas")]
        public async Task<ActionResult<ApiResponse<List<PantallaCatalogoDto>>>> GetPantallas()
        {
            try
            {
                var pantallas = await _context.Pantallas
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Orden)
                    .ThenBy(p => p.Nombre)
                    .Select(p => new PantallaCatalogoDto
                    {
                        PantallaId = p.PantallaId,
                        Nombre = p.Nombre,
                        Ruta = p.Ruta,
                        Icono = p.Icono,
                        Orden = p.Orden,
                        Activo = p.Activo
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<PantallaCatalogoDto>>.SuccessWith(pantallas, $"{pantallas.Count} pantalla(s) encontradas"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.GetPantallas: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<List<PantallaCatalogoDto>>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RolDto>>> GetById(int id)
        {
            try
            {
                var rol = await _context.Rols
                    .Include(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Pantalla)
                    .FirstOrDefaultAsync(r => r.RolId == id);

                if (rol == null)
                {
                    return NotFound(ApiResponse<RolDto>.FailureWith("Rol no encontrado"));
                }

                var rolDto = _mapper.Map<RolDto>(rol);
                return Ok(ApiResponse<RolDto>.SuccessWith(rolDto));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.GetById: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<RolDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<RolDto>>> Crear([FromBody] RolCreateUpdateDto rolDto)
        {
            try
            {
                var validationErrors = ValidateRolDto(rolDto);
                if (validationErrors.Count > 0)
                {
                    return BadRequest(ApiResponse<RolDto>.FailureWith("Datos inválidos para crear el rol", validationErrors));
                }

                var nombreNormalizado = rolDto.Nombre.Trim();
                var existe = await _context.Rols.AnyAsync(r => r.Nombre == nombreNormalizado);
                if (existe)
                {
                    return BadRequest(ApiResponse<RolDto>.FailureWith("Ya existe un rol con ese nombre"));
                }

                var rol = new Rol
                {
                    Nombre = nombreNormalizado,
                    Descripcion = rolDto.Descripcion.Trim(),
                    Activo = rolDto.Activo ?? true,
                    FechaCreacion = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<Rol>().AddAsync(rol);
                await _unitOfWork.SaveChangesAsync();

                await SyncRolPermisosAsync(rol.RolId, rolDto.Permisos, rolDto.PermisoIds);

                var usuarioEditorId = GetAuthenticatedUserId();
                if (usuarioEditorId.HasValue)
                {
                    await _auditoriaService.RegistrarCreacionAsync(
                        usuarioEditorId.Value,
                        "Rol",
                        rol.RolId,
                        new { rol.Nombre, rol.Descripcion, rol.Activo });
                }

                var rolCreado = await _context.Rols
                    .Include(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Pantalla)
                    .FirstAsync(r => r.RolId == rol.RolId);

                var responseDto = _mapper.Map<RolDto>(rolCreado);
                return CreatedAtAction(nameof(GetById), new { id = rol.RolId },
                    ApiResponse<RolDto>.SuccessWith(responseDto, "Rol creado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.Crear: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<RolDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<RolDto>>> Actualizar(int id, [FromBody] RolCreateUpdateDto rolDto)
        {
            try
            {
                var rol = await _context.Rols
                    .Include(r => r.RolPermisos)
                    .FirstOrDefaultAsync(r => r.RolId == id);

                if (rol == null)
                {
                    return NotFound(ApiResponse<RolDto>.FailureWith("Rol no encontrado"));
                }

                var validationErrors = ValidateRolDto(rolDto);
                if (validationErrors.Count > 0)
                {
                    return BadRequest(ApiResponse<RolDto>.FailureWith("Datos inválidos para actualizar el rol", validationErrors));
                }

                var nombreNormalizado = rolDto.Nombre.Trim();
                var existe = await _context.Rols.AnyAsync(r => r.RolId != id && r.Nombre == nombreNormalizado);
                if (existe)
                {
                    return BadRequest(ApiResponse<RolDto>.FailureWith("Ya existe otro rol con ese nombre"));
                }

                var datoAnterior = new
                {
                    rol.Nombre,
                    rol.Descripcion,
                    rol.Activo
                };

                rol.Nombre = nombreNormalizado;
                rol.Descripcion = rolDto.Descripcion.Trim();
                if (rolDto.Activo.HasValue)
                {
                    rol.Activo = rolDto.Activo.Value;
                }

                await _unitOfWork.GetRepository<Rol>().UpdateAsync(rol);
                await _unitOfWork.SaveChangesAsync();

                await SyncRolPermisosAsync(rol.RolId, rolDto.Permisos, rolDto.PermisoIds);

                var usuarioEditorId = GetAuthenticatedUserId();
                if (usuarioEditorId.HasValue)
                {
                    await _auditoriaService.RegistrarActualizacionAsync(
                        usuarioEditorId.Value,
                        "Rol",
                        rol.RolId,
                        datoAnterior,
                        new { rol.Nombre, rol.Descripcion, rol.Activo });
                }

                var rolActualizado = await _context.Rols
                    .Include(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Pantalla)
                    .FirstAsync(r => r.RolId == rol.RolId);

                var responseDto = _mapper.Map<RolDto>(rolActualizado);
                return Ok(ApiResponse<RolDto>.SuccessWith(responseDto, "Rol actualizado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.Actualizar: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<RolDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Eliminar(int id)
        {
            try
            {
                var rol = await _context.Rols.FirstOrDefaultAsync(r => r.RolId == id);
                if (rol == null)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Rol no encontrado"));
                }

                var usuariosAsignados = await _context.UsuarioRols.CountAsync(ur => ur.RolId == id);
                if (usuariosAsignados > 0)
                {
                    return BadRequest(ApiResponse<string>.FailureWith(
                        $"No se puede eliminar el rol porque tiene {usuariosAsignados} usuario(s) asignado(s). Desactívalo para impedir nuevas asignaciones."));
                }

                var permisos = await _context.RolPermisos.Where(rp => rp.RolId == id).ToListAsync();
                if (permisos.Count > 0)
                {
                    _context.RolPermisos.RemoveRange(permisos);
                }

                var datosEliminados = new { rol.Nombre, rol.Descripcion, rol.Activo };
                await _unitOfWork.GetRepository<Rol>().RemoveAsync(rol);
                await _unitOfWork.SaveChangesAsync();

                var usuarioEditorId = GetAuthenticatedUserId();
                if (usuarioEditorId.HasValue)
                {
                    await _auditoriaService.RegistrarEliminacionAsync(
                        usuarioEditorId.Value,
                        "Rol",
                        id,
                        datosEliminados);
                }

                return Ok(ApiResponse<string>.Succeeded("Rol eliminado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Roles.Eliminar: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }

        private async Task SyncRolPermisosAsync(int rolId, List<RolPermisoConfigDto>? permisosConfigurados, List<int>? pantallaIds)
        {
            var permisosMapeados = new List<RolPermiso>();

            if (permisosConfigurados != null && permisosConfigurados.Count > 0)
            {
                var requestedIds = permisosConfigurados
                    .Where(p => p.PantallaId > 0)
                    .Select(p => p.PantallaId)
                    .Distinct()
                    .ToList();

                var pantallasValidas = await _context.Pantallas
                    .Where(p => requestedIds.Contains(p.PantallaId))
                    .Select(p => p.PantallaId)
                    .ToListAsync();

                foreach (var permiso in permisosConfigurados.Where(p => p.PantallaId > 0 && pantallasValidas.Contains(p.PantallaId)))
                {
                    if (!permiso.Lectura && !permiso.Creacion && !permiso.Edicion && !permiso.Eliminacion)
                    {
                        continue;
                    }

                    permisosMapeados.Add(new RolPermiso
                    {
                        RolId = rolId,
                        PantallaId = permiso.PantallaId,
                        Lectura = permiso.Lectura,
                        Creacion = permiso.Creacion,
                        Edicion = permiso.Edicion,
                        Eliminacion = permiso.Eliminacion,
                        FechaCreacion = DateTime.UtcNow
                    });
                }
            }
            else
            {
                var requestedIds = (pantallaIds ?? new List<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var ids = await _context.Pantallas
                    .Where(p => requestedIds.Contains(p.PantallaId))
                    .Select(p => p.PantallaId)
                    .ToListAsync();

                permisosMapeados.AddRange(ids.Select(pantallaId => new RolPermiso
                {
                    RolId = rolId,
                    PantallaId = pantallaId,
                    Lectura = true,
                    Creacion = false,
                    Edicion = false,
                    Eliminacion = false,
                    FechaCreacion = DateTime.UtcNow
                }));
            }

            var actuales = await _context.RolPermisos.Where(rp => rp.RolId == rolId).ToListAsync();
            var nuevosIds = permisosMapeados.Select(rp => rp.PantallaId).ToHashSet();

            var paraEliminar = actuales.Where(rp => !nuevosIds.Contains(rp.PantallaId)).ToList();
            if (paraEliminar.Count > 0)
            {
                _context.RolPermisos.RemoveRange(paraEliminar);
            }

            var actualPorPantalla = actuales.ToDictionary(rp => rp.PantallaId);
            foreach (var permiso in permisosMapeados)
            {
                if (actualPorPantalla.TryGetValue(permiso.PantallaId, out var existente))
                {
                    existente.Lectura = permiso.Lectura;
                    existente.Creacion = permiso.Creacion;
                    existente.Edicion = permiso.Edicion;
                    existente.Eliminacion = permiso.Eliminacion;
                }
                else
                {
                    _context.RolPermisos.Add(permiso);
                }
            }

            await _context.SaveChangesAsync();
        }

        private static List<string> ValidateRolDto(RolCreateUpdateDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                errors.Add("El nombre del rol es obligatorio");
            }
            else if (dto.Nombre.Trim().Length < 3 || dto.Nombre.Trim().Length > 100)
            {
                errors.Add("El nombre del rol debe tener entre 3 y 100 caracteres");
            }

            if (string.IsNullOrWhiteSpace(dto.Descripcion))
            {
                errors.Add("La descripción del rol es obligatoria");
            }
            else if (dto.Descripcion.Trim().Length < 5 || dto.Descripcion.Trim().Length > 500)
            {
                errors.Add("La descripción del rol debe tener entre 5 y 500 caracteres");
            }

            return errors;
        }

        private int? GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
