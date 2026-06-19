# 🔐 Fase 2: Gestión de Base de Datos (Repositorios y Seguridad)

## 📋 Resumen

En esta fase se implementan:
1. **Patrón Repository** genérico y específico
2. **Patrón Unit of Work** para transacciones
3. **Hashing BCrypt** para contraseñas seguras
4. **Auditoría Automática** para cambios
5. **Inyección de Dependencias** para todos los servicios

---

## 📂 Estructura de Archivos Creados

```
SistemaPlanificacionSNP.Infrastructure/
├── Repositories/
│   ├── IRepository.cs              ✅ Interfaz genérica
│   ├── Repository.cs               ✅ Implementación genérica
│   ├── IUsuarioRepository.cs        ✅ Interfaz especializada
│   ├── UsuarioRepository.cs         ✅ Con búsquedas de seguridad
│   ├── IAuditoriaRepository.cs      ✅ Para historial
│   ├── AuditoriaRepository.cs       ✅ Consultas de auditoría
│   ├── IPlanificacionRepository.cs  ✅ Jerarquía completa
│   └── PlanificacionRepository.cs   ✅ Obtiene toda la estructura
│
├── UnitOfWork/
│   ├── IUnitOfWork.cs              ✅ Interfaz de transacciones
│   └── UnitOfWork.cs               ✅ Gestión centralizada
│
├── Services/
│   ├── IPasswordHashService.cs      ✅ Interfaz de hashing
│   ├── PasswordHashService.cs       ✅ BCrypt implementation
│   ├── IAuditoriaService.cs         ✅ Interfaz de auditoría
│   └── AuditoriaService.cs          ✅ Registro de cambios
│
├── DependencyInjectionExtensions.cs ✅ Registro en DI
└── EjemploUsoRepositorios.cs        ✅ Ejemplos de uso
```

---

## 🔧 Componentes Principales

### 1. Patrón Repository Genérico

**Interfaz IRepository<T>**
```csharp
public interface IRepository<T> where T : class
{
    // Lectura
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync();
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    // Escritura
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task<T> UpdateAsync(T entity);
    Task RemoveAsync(T entity);
    Task RemoveRangeAsync(IEnumerable<T> entities);
    Task SaveChangesAsync();
}
```

### 2. Repositorios Específicos

#### **UsuarioRepository**
```csharp
public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> GetByEmailAsync(string email);
    Task<Usuario?> GetWithRolesAsync(int usuarioId);  // Con permisos
    Task<IEnumerable<Usuario>> GetActivosAsync();
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);
    Task<bool> ExisteEmailAsync(string email);
}
```

#### **AuditoriaRepository**
```csharp
public interface IAuditoriaRepository : IRepository<AuditoriaTransaccional>
{
    Task<IEnumerable<AuditoriaTransaccional>> GetByUsuarioAsync(int usuarioId);
    Task<IEnumerable<AuditoriaTransaccional>> GetByEntidadAsync(string nombreEntidad);
    Task<IEnumerable<AuditoriaTransaccional>> GetByFechaRangoAsync(DateTime desde, DateTime hasta);
    Task<IEnumerable<AuditoriaTransaccional>> GetByOperacionAsync(string tipoOperacion);
}
```

#### **PlanificacionRepository**
```csharp
public interface IPlanificacionRepository : IRepository<PlanEstrategicoInstitucional>
{
    // Obtiene: PEI → OEI → Programas → Indicadores → Metas
    Task<PlanEstrategicoInstitucional?> GetWithHierarchyAsync(int peiId);
    Task<IEnumerable<PlanEstrategicoInstitucional>> GetByEntidadAsync(int entidadPublicaId);
    Task<IEnumerable<PlanEstrategicoInstitucional>> GetByEstadoAsync(string estado);
}
```

### 3. Patrón Unit of Work

```csharp
public interface IUnitOfWork : IDisposable
{
    // Repositorios
    IUsuarioRepository Usuarios { get; }
    IAuditoriaRepository Auditorias { get; }
    IPlanificacionRepository Planificacion { get; }
    IRepository<T> GetRepository<T>() where T : class;

    // Transacciones
    Task<int> SaveChangesAsync();
    Task<bool> BeginTransactionAsync();
    Task<bool> CommitAsync();
    Task<bool> RollbackAsync();
}
```

### 4. Servicios de Seguridad

#### **PasswordHashService (BCrypt)**
```csharp
public interface IPasswordHashService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

**Características:**
- WorkFactor = 12 (muy seguro)
- Automáticamente genera salt único
- Resistente a ataques de fuerza bruta

#### **AuditoriaService**
```csharp
public interface IAuditoriaService
{
    Task RegistrarCambioAsync(int usuarioId, string entidad, string tipoOperacion, ...);
    Task RegistrarCreacionAsync(int usuarioId, string entidad, int idRegistro, object datoNuevo);
    Task RegistrarActualizacionAsync(int usuarioId, string entidad, int idRegistro, ...);
    Task RegistrarEliminacionAsync(int usuarioId, string entidad, int idRegistro, ...);
}
```

### 5. Inyección de Dependencias

En **Program.cs** de cualquier API:
```csharp
// Agregar servicios de infraestructura
services.AddInfrastructureServices(connectionString);
```

Esto registra automáticamente:
- DbContext
- Todos los repositorios
- Unit of Work
- Servicios de seguridad

---

## 💻 Ejemplos de Uso

### Crear Usuario con Contraseña

```csharp
public class UsuarioController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashService _passwordService;

    public UsuarioController(IUnitOfWork unitOfWork, IPasswordHashService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearUsuario(CreateUsuarioRequest request)
    {
        // Verificar que no exista
        if (await _unitOfWork.Usuarios.ExisteNombreUsuarioAsync(request.NombreUsuario))
            return BadRequest("Usuario ya existe");

        // Crear usuario
        var usuario = new Usuario
        {
            NombreUsuario = request.NombreUsuario,
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Nombre = request.Nombre,
            Apellido = request.Apellido
        };

        // Guardar
        await _unitOfWork.Usuarios.AddAsync(usuario);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Usuario creado" });
    }
}
```

### Login Verificando Contraseña

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    // Buscar usuario
    var usuario = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(request.NombreUsuario);
    if (usuario == null)
        return Unauthorized("Usuario no encontrado");

    // Verificar contraseña
    if (!_passwordService.VerifyPassword(request.Password, usuario.PasswordHash))
        return Unauthorized("Contraseña incorrecta");

    // Obtener roles y permisos
    usuario = await _unitOfWork.Usuarios.GetWithRolesAsync(usuario.UsuarioId);

    // Generar JWT (en Fase 3)
    return Ok(new { message = "Login exitoso", usuario });
}
```

### Obtener Jerarquía Completa de Planificación

```csharp
[HttpGet("{peiId}")]
public async Task<IActionResult> GetPlanificacion(int peiId)
{
    var pei = await _unitOfWork.Planificacion.GetWithHierarchyAsync(peiId);
    if (pei == null)
        return NotFound();

    // pei contiene:
    // - EntidadPublica
    // - ObjetivosEstrategicos[]
    //   - ProgramasPresupuestarios[]
    //     - MatricesIndicadores[]
    //       - MetasTerritorial[]
    //     - ProyectosInversion[]

    return Ok(pei);
}
```

### Usar Transacciones

```csharp
[HttpPost("crear-estructura")]
public async Task<IActionResult> CrearEstructura()
{
    try
    {
        // Comenzar transacción
        if (!await _unitOfWork.BeginTransactionAsync())
            return StatusCode(500, "Error en transacción");

        // Crear múltiples entidades
        var pei = new PlanEstrategicoInstitucional { /* ... */ };
        await _unitOfWork.Planificacion.AddAsync(pei);

        var oei = new ObjetivoEstrategico { /* ... */ };
        var repo = _unitOfWork.GetRepository<ObjetivoEstrategico>();
        await repo.AddAsync(oei);

        // Confirmar transacción
        if (!await _unitOfWork.CommitAsync())
            return StatusCode(500, "Error al guardar");

        return Ok(new { message = "Estructura creada" });
    }
    catch
    {
        await _unitOfWork.RollbackAsync();
        return StatusCode(500, "Error interno");
    }
}
```

---

## 🔒 Seguridad Implementada

### BCrypt
- ✅ Hashing unidireccional (no reversible)
- ✅ Salt único por contraseña
- ✅ WorkFactor 12 (alto nivel de seguridad)
- ✅ Resistente a rainbow tables y GPU attacks

### Auditoría
- ✅ Todos los cambios registrados
- ✅ Quién hizo el cambio (UsuarioId)
- ✅ Qué se cambió (datos anteriores/nuevos)
- ✅ Cuándo se cambió (FechaOperacion)
- ✅ Tipo de operación (CREATE, UPDATE, DELETE)

---

## 📊 Diagrama de Flujo (Ejemplo: Login)

```
Usuario envía credenciales
    ↓
UsuarioRepository.GetByNombreUsuarioAsync()
    ↓
PasswordHashService.VerifyPassword()
    ↓
¿Contraseña válida?
    ├─ NO → Retornar 401 Unauthorized
    └─ SÍ → UsuarioRepository.GetWithRolesAsync()
         ↓
      AuditoriaService.RegistrarActualizacionAsync()
         ↓
      Generar JWT (Fase 3)
         ↓
      Retornar token
```

---

## 📝 Checklist de Implementación

- ✅ Patrón Repository genérico
- ✅ Repositorios específicos (Usuario, Auditoría, Planificación)
- ✅ Unit of Work con transacciones
- ✅ PasswordHashService con BCrypt
- ✅ AuditoriaService automático
- ✅ DependencyInjectionExtensions
- ✅ Ejemplos de uso

---

## 🚀 Próximos Pasos (Fase 3)

En Fase 3 se implementarán:
- DTOs y AutoMapper profiles
- Controladores con endpoints
- JWT (generación y validación)
- API Gateway (Ocelot)
- Respuestas estandarizadas

---

## 📞 Referencia Rápida

| Componente | Ubicación | Uso |
|-----------|-----------|-----|
| Repository genérico | `Repositories/Repository.cs` | Base para todo |
| UsuarioRepository | `Repositories/UsuarioRepository.cs` | Usuarios con roles |
| Unit of Work | `UnitOfWork/UnitOfWork.cs` | Transacciones |
| BCrypt | `Services/PasswordHashService.cs` | Hashing seguro |
| Auditoría | `Services/AuditoriaService.cs` | Registro de cambios |
| DI | `DependencyInjectionExtensions.cs` | Registro servicios |

---

**Fase 2**: ✅ COMPLETADA  
**Próximo**: Fase 3 - APIs y API Gateway
