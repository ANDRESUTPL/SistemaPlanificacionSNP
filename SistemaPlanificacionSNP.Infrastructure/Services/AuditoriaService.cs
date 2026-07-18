using System;
using System.Text.Json;
using System.Threading.Tasks;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Infrastructure.Services
{
    /// <summary>
    /// Interfaz para el servicio de auditoría transaccional
    /// </summary>
    public interface IAuditoriaService
    {
        Task RegistrarCambioAsync(int usuarioId, string entidad, string tipoOperacion, 
            int? idRegistro, object? datosAnteriores, object? datosNuevos, string? descripcion);
        
        Task RegistrarCreacionAsync(int usuarioId, string entidad, int idRegistro, object datoNuevo);
        
        Task RegistrarActualizacionAsync(int usuarioId, string entidad, int idRegistro, 
            object datosAnteriores, object datosNuevos);
        
        Task RegistrarEliminacionAsync(int usuarioId, string entidad, int idRegistro, object datosAnteriores);
    }

    /// <summary>
    /// Implementación del servicio de auditoría transaccional
    /// </summary>
    public class AuditoriaService : IAuditoriaService
    {
        private readonly AuthDbContext _context;

        public AuditoriaService(AuthDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task RegistrarCambioAsync(int usuarioId, string entidad, string tipoOperacion,
            int? idRegistro, object? datosAnteriores, object? datosNuevos, string? descripcion)
        {
            var auditoria = new AuditoriaTransaccional
            {
                UsuarioId = usuarioId,
                Entidad = entidad,
                TipoOperacion = tipoOperacion,
                IdRegistro = idRegistro,
                DatosAnteriores = datosAnteriores != null ? JsonSerializer.Serialize(datosAnteriores) : null,
                DatosNuevos = datosNuevos != null ? JsonSerializer.Serialize(datosNuevos) : null,
                FechaOperacion = DateTime.UtcNow,
                Descripcion = descripcion
            };

            _context.AuditoriaTransaccionals.Add(auditoria);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarCreacionAsync(int usuarioId, string entidad, int idRegistro, object datoNuevo)
        {
            await RegistrarCambioAsync(usuarioId, entidad, "CREATE", idRegistro, null, datoNuevo, 
                $"Se creó un nuevo registro en {entidad}");
        }

        public async Task RegistrarActualizacionAsync(int usuarioId, string entidad, int idRegistro,
            object datosAnteriores, object datosNuevos)
        {
            await RegistrarCambioAsync(usuarioId, entidad, "UPDATE", idRegistro, datosAnteriores, 
                datosNuevos, $"Se actualizó un registro en {entidad}");
        }

        public async Task RegistrarEliminacionAsync(int usuarioId, string entidad, int idRegistro, object datosAnteriores)
        {
            await RegistrarCambioAsync(usuarioId, entidad, "DELETE", idRegistro, datosAnteriores, null,
                $"Se eliminó un registro en {entidad}");
        }
    }
}
