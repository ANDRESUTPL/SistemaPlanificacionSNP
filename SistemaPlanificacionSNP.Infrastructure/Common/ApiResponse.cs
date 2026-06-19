namespace SistemaPlanificacionSNP.Infrastructure.Common
{
    /// <summary>
    /// Respuesta genérica para todos los endpoints API
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApiResponse() { }

        public ApiResponse(bool success, string message, T? data = default, List<string>? errors = null)
        {
            this.Success = success;
            Message = message;
            Data = data;
            Errors = errors ?? new();
        }

        public static ApiResponse<T> SuccessWith(T data, string message = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Succeeded(string message = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse<T> FailureWith(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new()
            };
        }

        public static ApiResponse<T> ValidationError(Dictionary<string, List<string>> validationErrors)
        {
            var errors = new List<string>();
            foreach (var kvp in validationErrors)
            {
                errors.AddRange(kvp.Value.Select(err => $"{kvp.Key}: {err}"));
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = "Errores de validación",
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Respuesta genérica para listados paginados
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedResponse() { }

        public PaginatedResponse(List<T> data, int totalItems, int pageNumber, int pageSize)
        {
            Data = data;
            TotalItems = totalItems;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        }
    }

    /// <summary>
    /// Respuesta genérica completa con paginación
    /// </summary>
    public class ApiPaginatedResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaginatedResponse<T> Data { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiPaginatedResponse<T> SuccessWith(PaginatedResponse<T> data, string message = "Operación exitosa")
        {
            return new ApiPaginatedResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiPaginatedResponse<T> Failure(string message)
        {
            return new ApiPaginatedResponse<T>
            {
                Success = false,
                Message = message
            };
        }
    }
}
