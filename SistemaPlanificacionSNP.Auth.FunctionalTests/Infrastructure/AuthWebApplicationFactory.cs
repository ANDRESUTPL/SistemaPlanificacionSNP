using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.TestUtilities.Infrastructure;

namespace SistemaPlanificacionSNP.Auth.FunctionalTests.Infrastructure;

public sealed class AuthWebApplicationFactory : MsSqlWebApplicationFactoryBase<Program, AuthDbContext>
{
	protected override async Task SeedAsync(IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

		// Verificamos si NO existe para crearlo una sola vez
		if (!await db.Usuarios.AnyAsync(u => u.NombreUsuario == "admin.integration"))
		{
			// 1. Crear la Pantalla
			var pantalla = new Pantalla
			{
				Nombre = "Seguridad",
				Ruta = "/seguridad",
				Icono = "fa-shield-alt",
				Orden = 1,
				Activo = true,
				FechaCreacion = DateTime.UtcNow
			};
			db.Pantallas.Add(pantalla);

			// 2. Crear el Rol
			var rol = new Rol
			{
				Nombre = "Administrador",
				Descripcion = "Rol admin integration",
				Activo = true,
				FechaCreacion = DateTime.UtcNow
			};
			db.Rols.Add(rol);

			// Guardamos para generar los IDs de Pantalla y Rol
			await db.SaveChangesAsync();

			// 3. Asignar Permisos del Rol a la Pantalla
			db.RolPermisos.Add(new RolPermiso
			{
				RolId = rol.RolId,
				PantallaId = pantalla.PantallaId,
				Lectura = true,
				Creacion = true,
				Edicion = true,
				Eliminacion = true,
				FechaCreacion = DateTime.UtcNow
			});

			// 4. Crear el Usuario con la contraseña exacta que espera el test ("Password123!")
			var usuario = new Usuario
			{
				NombreUsuario = "admin.integration",
				Email = "admin.integration@snp.gob",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 12),
				Nombre = "Admin",
				Apellido = "Integration",
				Activo = true,
				FechaCreacion = DateTime.UtcNow
			};
			db.Usuarios.Add(usuario);

			// Guardamos para generar el ID del Usuario
			await db.SaveChangesAsync();

			// 5. Vincular el Usuario con el Rol
			db.UsuarioRols.Add(new UsuarioRol
			{
				UsuarioId = usuario.UsuarioId,
				RolId = rol.RolId,
				FechaAsignacion = DateTime.UtcNow
			});

			// Guardado final de las relaciones
			await db.SaveChangesAsync();
		}
	}
}