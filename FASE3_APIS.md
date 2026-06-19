# 🚀 Fase 3: APIs REST y API Gateway (JWT + Ocelot)

## 📋 Resumen

En esta fase se implementan:
1. **DTOs** (Data Transfer Objects) para todas las entidades
2. **AutoMapper** para mapeo automático Entity ↔ DTO
3. **JWT** (JSON Web Tokens) para autenticación
4. **Controladores REST** con endpoints seguros
5. **Ocelot** como API Gateway central

---

## 📂 Estructura de Archivos Creados

```
SistemaPlanificacionSNP.Infrastructure/
├── DTOs/
│   ├── UsuarioDto.cs           ✅ (LoginDto, CreateDto, ResponseDto)
│   ├── RolDto.cs               ✅ (Con permisos)
│   ├── PermisoDto.cs           ✅ (Lectura, Creación, Edición, Eliminación)
│   ├── PlanificacionDto.cs     ✅ (Jerarquía completa PEI→OEI→Programas)
│   └── EntidadPublicaDto.cs    ✅ (Entidades públicas)
│
├── JWT/
│   ├── JwtSettings.cs          ✅ Configuración desde appsettings
│   └── JwtTokenGenerator.cs    ✅ Generación y validación de tokens
│
├── Mapping/
│   └── MappingProfile.cs       ✅ AutoMapper configuration
│
└── Common/
    └── ApiResponse.cs          ✅ Respuestas estandarizadas

SistemaPlanificacionSNP.Auth.Api/
├── Controllers/
│   ├── AuthController.cs       ✅ Login, RefreshToken, Logout, CambiarPassword
│   └── UsuariosController.cs   ✅ CRUD usuarios, menú dinámico
│
├── Program.cs                  ✅ Configuración completa
└── appsettings.json            ✅ JWT settings

SistemaPlanificacionSNP.ApiGateway/
├── ocelot.json                 ✅ Rutas y configuración
└── Program.cs                  ✅ Middleware JWT + Ocelot
```

---

## 🔑 DTOs Principales

### UsuarioDto
```csharp
public class UsuarioDto
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; }      // No incluye PasswordHash
    public string Email { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<RolDto> Roles { get; set; }       // Con permisos
}
```

### LoginDto / LoginResponseDto
```csharp
public class LoginDto
{
    public string NombreUsuario { get; set; }
    public string Password { get; set; }
    public bool Recuerdame { get; set; }
}

public class LoginResponseDto
{
    public UsuarioDto Usuario { get; set; }
    public string AccessToken { get; set; }       // JWT
    public string RefreshToken { get; set; }      // Para renovación
    public DateTime AccessTokenExpiration { get; set; }
}
```

### MenuPermisoDto (Menú Dinámico)
```csharp
public class MenuPermisoDto
{
    public int PantallaId { get; set; }
    public string Nombre { get; set; }
    public string Icono { get; set; }
    public string? Ruta { get; set; }
    public int Orden { get; set; }
    public PermisoDto? Permiso { get; set; }      // Lectura, Creación, Edición, Eliminación
    public List<MenuPermisoDto> Subpantallas { get; set; }  // Jerarquía
}
```

---

## 🔐 Autenticación JWT

### Características

- ✅ **Tokens con Claims**: UsuarioId, NombreUsuario, Email, Roles, Permisos
- ✅ **Refresh Tokens**: Validez de 7 días en BD
- ✅ **Access Tokens**: Validez de 60 minutos (configurable)
- ✅ **Permisos en Claims**: `Lectura_1`, `Creacion_2`, etc.
- ✅ **Seguridad**: HMAC SHA256, sin expiración reloj

### Configuración en appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "SistemaPlanificacionSNP",
    "Audience": "SistemaPlanificacionSNP",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SNP_Database;User Id=sa;Password=YourPassword;Encrypt=false;"
  }
}
```

---

## 🎯 Endpoints de Auth.Api

### 1. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "nombreUsuario": "admin",
  "password": "micontraseña123",
  "recuerdame": true
}

RESPUESTA 200:
{
  "success": true,
  "message": "Login exitoso",
  "data": {
    "usuario": {
      "usuarioId": 1,
      "nombreUsuario": "admin",
      "email": "admin@snp.gov",
      "nombre": "Administrador",
      "apellido": "Sistema",
      "activo": true,
      "roles": [
        {
          "rolId": 1,
          "nombre": "Administrador",
          "descripcion": "Acceso total al sistema",
          "permisos": [...]
        }
      ]
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "rF9kL2mN3oP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3i",
    "accessTokenExpiration": "2026-06-17T11:15:00Z",
    "refreshTokenExpiration": "2026-06-24T10:15:00Z"
  },
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### 2. Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "rF9kL2mN3oP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3i"
}

RESPUESTA 200:
{
  "success": true,
  "message": "Token refrescado",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "pN2qO3rP4sQ5tR6uS7vT8wU9xV0yW1zX2aY3bZ4c",
    "accessTokenExpiration": "2026-06-17T12:15:00Z"
  },
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### 3. Logout
```http
POST /api/auth/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

RESPUESTA 200:
{
  "success": true,
  "message": "Logout exitoso",
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### 4. Cambiar Contraseña
```http
POST /api/auth/cambiar-password
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "passwordActual": "micontraseña123",
  "passwordNueva": "nuevacontraseña456",
  "passwordConfirmar": "nuevacontraseña456"
}

RESPUESTA 200:
{
  "success": true,
  "message": "Contraseña actualizada exitosamente",
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### 5. Obtener Usuario por ID
```http
GET /api/usuarios/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

RESPUESTA 200:
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "usuarioId": 1,
    "nombreUsuario": "admin",
    "email": "admin@snp.gov",
    "nombre": "Administrador",
    "apellido": "Sistema",
    "activo": true,
    "fechaCreacion": "2025-01-01T00:00:00Z",
    "roles": [...]
  },
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### 6. Crear Usuario
```http
POST /api/usuarios/crear
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "nombreUsuario": "jperez",
  "email": "jperez@snp.gov",
  "password": "micontraseña123",
  "nombre": "Juan",
  "apellido": "Pérez"
}

RESPUESTA 201:
{
  "success": true,
  "message": "Usuario creado exitosamente",
  "data": {
    "usuarioId": 5,
    "nombreUsuario": "jperez",
    "email": "jperez@snp.gov",
    "nombre": "Juan",
    "apellido": "Pérez",
    "activo": true,
    "fechaCreacion": "2026-06-17T10:15:00Z"
  },
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### 7. Menú Dinámico (Basado en Roles y Permisos)
```http
GET /api/usuarios/menu/actual
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

RESPUESTA 200:
{
  "success": true,
  "message": "Menú obtenido",
  "data": [
    {
      "pantallaId": 1,
      "nombre": "Configuración",
      "icono": "fas fa-cog",
      "ruta": "/configuracion",
      "orden": 1,
      "permiso": {
        "pantalla Id": 1,
        "lectura": true,
        "creacion": true,
        "edicion": true,
        "eliminacion": true
      },
      "subpantallas": [
        {
          "pantallaId": 2,
          "nombre": "Usuarios",
          "icono": "fas fa-users",
          "ruta": "/configuracion/usuarios",
          "permiso": {...},
          "subpantallas": []
        }
      ]
    }
  ],
  "timestamp": "2026-06-17T10:15:00Z"
}
```

---

## 🔗 Configuración Ocelot (API Gateway)

### Rutas Configuradas

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 7001}],
      "UpstreamHttpMethod": ["POST", "GET"]
    },
    {
      "DownstreamPathTemplate": "/api/usuarios/{everything}",
      "UpstreamPathTemplate": "/api/usuarios/{everything}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 7001}]
    },
    {
      "DownstreamPathTemplate": "/api/parametrizacion/{everything}",
      "UpstreamPathTemplate": "/api/parametrizacion/{everything}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 7002}]
    }
  ]
}
```

### Flujo de Solicitud

```
Cliente Frontend
    ↓
API Gateway (localhost:7000)
    ↓ (valida JWT)
Enruta a microservicio correspondiente
    ↓
Auth.Api (7001) / Parametrizacion.Api (7002) / etc
    ↓
Respuesta
```

---

## 🔄 Flujo de Autenticación Completo

```
1. Usuario envía credenciales
   POST /api/auth/login
   ↓
2. Auth.Api verifica usuario y contraseña
   ↓
3. Genera Access Token + Refresh Token
   - Access Token: 60 min
   - Refresh Token: 7 días (guardado en BD)
   ↓
4. Frontend almacena tokens en localStorage
   ↓
5. Cada solicitud incluye header:
   Authorization: Bearer <AccessToken>
   ↓
6. API Gateway valida JWT
   ↓
7. Si Access Token expirado y Refresh disponible:
   POST /api/auth/refresh-token
   ↓
8. Nuevo Access Token generado
   ↓
9. Solicitud se reintenta con nuevo token
```

---

## 💻 Ejemplo de Implementación Frontend (JavaScript/React)

```javascript
// Login
async function login(nombreUsuario, password) {
  const response = await fetch('https://localhost:7000/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario, password, recuerdame: true })
  });
  
  const data = await response.json();
  if (data.success) {
    localStorage.setItem('accessToken', data.data.accessToken);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    localStorage.setItem('usuario', JSON.stringify(data.data.usuario));
    return data.data;
  }
  throw new Error(data.message);
}

// Solicitud con Token (interceptor)
async function fetchWithToken(url, options = {}) {
  const token = localStorage.getItem('accessToken');
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers
  };
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  let response = await fetch(url, { ...options, headers });
  
  // Si token expirado, intentar refrescar
  if (response.status === 401 && localStorage.getItem('refreshToken')) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      headers['Authorization'] = `Bearer ${localStorage.getItem('accessToken')}`;
      response = await fetch(url, { ...options, headers });
    }
  }
  
  return response;
}

// Refrescar Token
async function refreshAccessToken() {
  const response = await fetch('https://localhost:7000/api/auth/refresh-token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      accessToken: localStorage.getItem('accessToken'),
      refreshToken: localStorage.getItem('refreshToken')
    })
  });
  
  const data = await response.json();
  if (data.success) {
    localStorage.setItem('accessToken', data.data.accessToken);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    return true;
  }
  return false;
}

// Obtener menú dinámico
async function obtenerMenuUsuario() {
  const response = await fetchWithToken('https://localhost:7000/api/usuarios/menu/actual');
  const data = await response.json();
  return data.success ? data.data : [];
}

// Logout
async function logout() {
  await fetchWithToken('https://localhost:7000/api/auth/logout', { method: 'POST' });
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('usuario');
}
```

---

## 📊 Diagrama de Autenticación

```
┌─────────────┐
│   Usuario   │
└──────┬──────┘
       │ Login
       ↓
┌──────────────────┐
│  API Gateway     │
│  (Ocelot)        │
└────────┬─────────┘
         │
         ↓
┌──────────────────┐
│  Auth.Api        │
│  ┌────────────┐  │
│  │ Valida BD  │  │
│  │ BCrypt     │  │
│  └────────────┘  │
│  ┌────────────┐  │
│  │ Genera JWT │  │
│  │ + Refresh  │  │
│  └────────────┘  │
└────────┬─────────┘
         │ AccessToken + RefreshToken
         ↓
┌─────────────────────┐
│  Frontend           │
│  (localStorage)     │
└──────────┬──────────┘
           │
           ├─── Bearer Token en cada request
           │
           ↓
┌──────────────────────┐
│  API Gateway         │
│  (Valida JWT)        │
└────────┬─────────────┘
         │
         ↓ Request válido
┌──────────────────────┐
│  Microservicios      │
│  - Auth.Api          │
│  - Parametrizacion   │
│  - Planificación     │
└──────────────────────┘
```

---

## 🚨 Manejo de Errores

### Respuesta de Error Estándar
```json
{
  "success": false,
  "message": "Descripción del error",
  "errors": ["Campo1: Error específico", "Campo2: Error específico"],
  "timestamp": "2026-06-17T10:15:00Z"
}
```

### Códigos HTTP Utilizados

| Código | Significado |
|--------|-------------|
| 200 | Éxito |
| 201 | Recurso creado |
| 400 | Solicitud inválida / validación fallida |
| 401 | No autenticado o token inválido |
| 403 | Autenticado pero sin permisos |
| 404 | Recurso no encontrado |
| 500 | Error interno del servidor |

---

## 📝 Checklist de Implementación Fase 3

- ✅ DTOs para todas las entidades (Usuario, Rol, Permiso, Planificación)
- ✅ AutoMapper profile con configuración completa
- ✅ JWT settings y token generator
- ✅ AuthController con endpoints de autenticación
- ✅ UsuariosController con CRUD y menú dinámico
- ✅ ApiResponse<T> para respuestas estandarizadas
- ✅ Ocelot configuration (ocelot.json)
- ✅ Program.cs para Auth.Api con DI y middleware
- ✅ Program.cs para API Gateway con JWT middleware
- ✅ CORS configurado para frontend

---

## 🚀 Próximos Pasos (Fase 4)

En Fase 4 se implementarán:
- Frontend MVC con vistas dinámicas
- Menú personalizado por roles
- Formularios con validación
- Tableros de control (Dashboard)
- Reportes de planificación

---

## 📞 Puertos por Defecto

| Servicio | Puerto | URL |
|----------|--------|-----|
| API Gateway | 7000 | https://localhost:7000 |
| Auth.Api | 7001 | https://localhost:7001 |
| Parametrizacion.Api | 7002 | https://localhost:7002 |
| MacroPlanificacion.Api | 7003 | https://localhost:7003 |
| PlanificacionInstitucional.Api | 7004 | https://localhost:7004 |
| ControlCalidad.Api | 7005 | https://localhost:7005 |
| Web.MVC | 7010 | https://localhost:7010 |

---

**Fase 3**: ✅ COMPLETADA  
**Próximo**: Fase 4 - Frontend MVC
