using System.Net;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Common
{
    public static class ApiHttpErrorHelper
    {
        public static string BuildStatusMessage(
            HttpStatusCode statusCode,
            string fallback,
            string? unauthorizedMessage = null,
            string? forbiddenMessage = null)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => unauthorizedMessage ?? "Tu sesion expiro. Inicia sesion nuevamente para continuar.",
                HttpStatusCode.Forbidden => forbiddenMessage ?? "No cuentas con permisos para realizar esta accion.",
                HttpStatusCode.NotFound => "No se encontro la informacion solicitada.",
                HttpStatusCode.InternalServerError => "El servidor presento un error interno. Intenta nuevamente en unos minutos.",
                HttpStatusCode.BadGateway => "El gateway no pudo comunicarse con el servicio solicitado.",
                HttpStatusCode.ServiceUnavailable => "El servicio no esta disponible temporalmente.",
                _ => fallback
            };
        }

        public static string? TryExtractApiMessage(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public static async Task<string?> TryExtractApiMessageAsync(HttpResponseMessage? response)
        {
            if (response?.Content == null)
            {
                return null;
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync();
                return TryExtractApiMessage(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
