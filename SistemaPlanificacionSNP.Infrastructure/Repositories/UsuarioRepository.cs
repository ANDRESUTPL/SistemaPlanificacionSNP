using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    /// <summary>
    /// Interfaz del repositorio específico para Usuario
    /// Incluye operaciones de seguridad y búsqueda especializada
    /// </summary>
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetWithRolesAsync(int usuarioId);

        Task<List<Usuario?>> GetAllUsersWithRolesAsync();

		Task<IEnumerable<Usuario>> GetActivosAsync();
        Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);
        Task<bool> ExisteEmailAsync(string email);
    }

    /// <summary>
    /// Implementación del repositorio específico para Usuario
    /// </summary>
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> GetWithRolesAsync(int usuarioId)
        {
            return await _dbSet
                .Include(u => u.UsuarioRols)
                    .ThenInclude(ur => ur.Rol)
                        .ThenInclude(r => r.RolPermisos)
                            .ThenInclude(rp => rp.Pantalla)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
        }
		public async Task<List<Usuario?>> GetAllUsersWithRolesAsync()
		{
			return await _dbSet
				.Include(u => u.UsuarioRols)
					.ThenInclude(ur => ur.Rol)
						.ThenInclude(r => r.RolPermisos)
                        .ThenInclude(p => p.Pantalla)
				.ToListAsync();
		}
		public async Task<IEnumerable<Usuario>> GetActivosAsync()
        {
            return await _dbSet
                .Where(u => u.Activo)
                .ToListAsync();
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        {
            return await _dbSet.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }
    }
}
