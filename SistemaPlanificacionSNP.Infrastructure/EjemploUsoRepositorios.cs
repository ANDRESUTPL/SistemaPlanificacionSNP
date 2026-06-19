using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Infrastructure
{
    /// <summary>
    /// Ejemplo de cómo usar el patrón Unit of Work + Repositorios + Seguridad
    /// Este archivo es de referencia y puede ser eliminado después
    /// </summary>
    public class EjemploUsoRepositorios
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHashService _passwordService;
        private readonly IAuditoriaService _auditoriaService;

        public EjemploUsoRepositorios(IUnitOfWork unitOfWork, 
            IPasswordHashService passwordService,
            IAuditoriaService auditoriaService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _auditoriaService = auditoriaService;
        }

        /// <summary>
        /// Ejemplo: Crear un nuevo usuario con contraseña hasheada
        /// </summary>
        public async Task<bool> CrearUsuarioEjemploAsync(string nombreUsuario, string email, string password)
        {
            try
            {
                // 1. Verificar que no exista el usuario
                bool existeUsuario = await _unitOfWork.Usuarios.ExisteNombreUsuarioAsync(nombreUsuario);
                if (existeUsuario)
                    return false;

                // 2. Hashear la contraseña
                string passwordHash = _passwordService.HashPassword(password);

                // 3. Crear el usuario
                var usuario = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    Email = email,
                    PasswordHash = passwordHash,
                    Nombre = "Nombre",
                    Apellido = "Apellido",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                // 4. Agregar y guardar
                await _unitOfWork.Usuarios.AddAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // 5. Registrar en auditoría
                await _auditoriaService.RegistrarCreacionAsync(
                    usuarioId: 1, // El usuario que realiza la acción
                    entidad: "Usuario",
                    idRegistro: usuario.UsuarioId,
                    datoNuevo: new { usuario.NombreUsuario, usuario.Email }
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ejemplo: Login verificando contraseña
        /// </summary>
        public async Task<Usuario?> LoginEjemploAsync(string nombreUsuario, string password)
        {
            // 1. Buscar usuario
            var usuario = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(nombreUsuario);
            if (usuario == null)
                return null;

            // 2. Verificar contraseña
            bool esValida = _passwordService.VerifyPassword(password, usuario.PasswordHash);
            if (!esValida)
                return null;

            // 3. Actualizar última conexión
            usuario.FechaUltimoLogin = DateTime.UtcNow;
            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            // 4. Retornar usuario con roles
            return await _unitOfWork.Usuarios.GetWithRolesAsync(usuario.UsuarioId);
        }

        /// <summary>
        /// Ejemplo: Obtener todo el árbol de planificación de una entidad
        /// </summary>
        public async Task<PlanEstrategicoInstitucional?> ObtenerPlanificacionCompletaEjemploAsync(int peiId)
        {
            // Obtiene: PEI -> OEI -> Programas -> Indicadores -> Metas
            var pei = await _unitOfWork.Planificacion.GetWithHierarchyAsync(peiId);
            return pei;
        }

        /// <summary>
        /// Ejemplo: Usar transacciones
        /// </summary>
        public async Task<bool> TransaccionEjemploAsync()
        {
            try
            {
                // Iniciar transacción
                if (!await _unitOfWork.BeginTransactionAsync())
                    return false;

                // Operaciones múltiples
                var usuario = new Usuario
                {
                    NombreUsuario = "usuario_test",
                    Email = "test@example.com",
                    PasswordHash = _passwordService.HashPassword("Password123!"),
                    Nombre = "Test",
                    Apellido = "User"
                };

                await _unitOfWork.Usuarios.AddAsync(usuario);

                // Guardar y confirmar
                bool success = await _unitOfWork.CommitAsync();
                return success;
            }
            catch
            {
                // En caso de error, hacer rollback automático
                await _unitOfWork.RollbackAsync();
                return false;
            }
        }
    }
}
