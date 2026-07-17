using System;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace SistemaPlanificacionSNP.Infrastructure.Services
{
    /// <summary>
    /// Interfaz para el servicio de hashing de contraseñas
    /// Utiliza BCrypt para máxima seguridad
    /// </summary>
    public interface IPasswordHashService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// Implementación del servicio de hashing con BCrypt
    /// </summary>
    public class PasswordHashService : IPasswordHashService
    {
        private const int WorkFactor = 12; // Mayor = más seguro pero más lento

        /// <summary>
        /// Hashea una contraseña usando BCrypt
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            return BC.HashPassword(password, workFactor: WorkFactor);
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("El hash no puede estar vacío", nameof(hash));

			try
			{			
				string cleanPassword = password.Trim();
				string cleanHash = hash.Trim();

				bool response = BC.Verify(cleanPassword, cleanHash);
				return response;
			}
			catch (Exception ex)
			{				
				Console.WriteLine($"Error real en BCrypt: {ex.Message}");
				throw; // Relanzamos el error temporalmente para que estalle en tu consola y lo leas
			}
		}
    }
}
