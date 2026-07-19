using System.Net;
using System.Text;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Tests.Common;

public static class WebTestData
{
    public static HttpResponseMessage JsonResponse(object value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static object ApiResponse<T>(T data, bool success = true, string message = "Operación exitosa")
    {
        return new
        {
            success,
            message,
            data
        };
    }

    public static object ApiPaginatedResponse<T>(IEnumerable<T> data, int totalItems = 1, int pageNumber = 1, int pageSize = 10)
    {
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new
        {
            success = true,
            message = "Operación exitosa",
            data = new
            {
                data,
                totalItems,
                pageNumber,
                pageSize,
                totalPages
            }
        };
    }
}