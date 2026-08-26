using Microsoft.AspNetCore.Http;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Infrastructure.DTOs; // Donde esté tu PermisoDto
using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaPlanificacionSNP.Web.Common
{
	public static class PermisosHelper
	{
		/// <summary>
		/// Aplica los permisos de la sesión a cualquier ViewModel que implemente IPermisosViewModel
		/// </summary>
		public static void CargarPermisos<T>(this HttpContext context, T model, string rutaPantalla) where T : IPermisosViewModel
		{
			if (model == null) return;

			try
			{
				var permisos = context.Session.GetObject<List<PermisoDto>>("PermisosSesion");
				if (permisos != null)
				{
					// Buscamos el permiso correspondiente a la ruta enviada
					var permisoPantalla = permisos.FirstOrDefault(p =>
						!string.IsNullOrEmpty(p.Ruta) &&
						p.Ruta.Contains(rutaPantalla, StringComparison.OrdinalIgnoreCase));

					if (permisoPantalla != null)
					{
						model.PuedeLeer = permisoPantalla.Lectura;
						model.PuedeCrear = permisoPantalla.Creacion;
						model.PuedeEditar = permisoPantalla.Edicion;
						model.PuedeEliminar = permisoPantalla.Eliminacion;
						return; // Salimos exitosamente
					}
				}
			}
			catch(Exception ex)
			{
				// Si falla la deserialización, pasa de largo y deniega permisos por defecto
			}

			// Denegación por defecto (Seguridad: Si no hay permisos, todo es falso)
			model.PuedeLeer = false;
			model.PuedeCrear = false;
			model.PuedeEditar = false;
			model.PuedeEliminar = false;
		}
	}
}