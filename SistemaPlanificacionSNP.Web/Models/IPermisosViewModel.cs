namespace SistemaPlanificacionSNP.Web.Models
{
	public interface IPermisosViewModel
	{
		bool PuedeLeer { get; set; }
		bool PuedeCrear { get; set; }
		bool PuedeEditar { get; set; }
		bool PuedeEliminar { get; set; }
	}
}