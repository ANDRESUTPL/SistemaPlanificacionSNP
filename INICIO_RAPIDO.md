# 🚀 GUÍA RÁPIDA DE INICIO

## 1. Abrir la Solución en Visual Studio

1. Abre **Visual Studio 2022** (o superior)
2. Selecciona **Archivo > Abrir > Proyecto o Solución**
3. Navega a: `c:\ProyectoAndres\SistemaPlanificacionSNP\`
4. Selecciona **SistemaPlanificacionSNP.sln**
5. Haz clic en **Abrir**

## 2. Restaurar Paquetes NuGet

Visual Studio debería restaurar automáticamente los paquetes. Si no es así:

1. **Menú**: Herramientas > Administrador de Paquetes NuGet > Administrar Paquetes NuGet para la Solución
2. Haz clic en **Restaurar** en la parte superior

O desde la **Consola del Administrador de Paquetes**:
```powershell
Update-Package -Reinstall
```

## 3. Instalar Dependencias Faltantes

Si recives errores de "Metapaquete no encontrado", instala explícitamente:

```powershell
# Asegúrate de que SistemaPlanificacionSNP.Infrastructure esté seleccionado

Install-Package Microsoft.EntityFrameworkCore -Version 8.0.0
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.0
Install-Package Microsoft.EntityFrameworkCore.Design -Version 8.0.0
```

## 4. Crear la Base de Datos (Migración)

En la **Consola del Administrador de Paquetes** (PMC):

```powershell
# 1. Selecciona el proyecto por defecto: SistemaPlanificacionSNP.Infrastructure

# 2. Genera la migración inicial
Add-Migration InitialCreate -Project SistemaPlanificacionSNP.Infrastructure

# 3. Aplica la migración a la base de datos
Update-Database -Project SistemaPlanificacionSNP.Infrastructure
```

## 5. Verificar la Base de Datos

1. Abre **SQL Server Object Explorer** en Visual Studio:
   - Menú: **Ver > SQL Server Object Explorer**
2. Expande **SQL Server > (LocalDb)\MSSQLLocalDB > Databases**
3. Verifica que exista **SistemaPlanificacionSNP**
4. Expande la base de datos para ver las tablas

O usa **SQL Server Management Studio (SSMS)**:
1. Abre SSMS
2. Conéctate a **(LocalDb)\MSSQLLocalDB**
3. Verifica que exista la base de datos y sus tablas

## 6. Estructura de Proyectos

```
SistemaPlanificacionSNP (Solución)
│
├── Domain (Librería) ⬅️ Contiene entidades
│
├── Infrastructure (Librería) ⬅️ Contiene DbContext y Migraciones
│
├── Auth.Api (API REST) ⬅️ Autenticación y usuarios
├── Parametrizacion.Api (API REST)
├── MacroPlanificacion.Api (API REST)
├── PlanificacionInstitucional.Api (API REST)
├── ControlCalidad.Api (API REST)
│
├── ApiGateway (API Gateway) ⬅️ Ocelot - Enrutamiento central
│
└── Web (ASP.NET Core MVC) ⬅️ Frontend
```

## 7. Próximos Pasos

La **Fase 1** está completa con:
- ✅ 19 entidades del dominio
- ✅ ApplicationDbContext con Fluent API
- ✅ Estructura de proyectos
- ✅ appsettings.json configurados

**Fase 2** implementará:
- Patrón Repository
- Patrón Unit of Work
- Hashing BCrypt para usuarios
- Auditoría

## 8. Solución de Problemas

### Error: "The database already exists..."
- Cambia el nombre de la BD en `appsettings.json`
- O elimina la base de datos en SSMS antes de crear la migración

### Error: "Package restore failed"
```powershell
# Limpia la caché de NuGet
Remove-Item -Recurse -Force $env:USERPROFILE\.nuget\packages

# Restaura nuevamente
Update-Package -Reinstall
```

### Error: "DbContext no found"
- Asegúrate de que `SistemaPlanificacionSNP.Infrastructure` esté seleccionado en la PMC
- Verifica que `ApplicationDbContext` esté correctamente implementado

### Error: "Migration pending"
```powershell
# Aplica todas las migraciones pendientes
Update-Database -Project SistemaPlanificacionSNP.Infrastructure
```

## 9. Comandos Útiles

```powershell
# Ver todas las migraciones
Get-Migration -Project SistemaPlanificacionSNP.Infrastructure

# Ver la BD en formato SQL
Script-Migration -From 0 -To InitialCreate -Project SistemaPlanificacionSNP.Infrastructure

# Revertir a una versión anterior
Update-Database -Migration [NombreMigracion] -Project SistemaPlanificacionSNP.Infrastructure

# Eliminar la última migración (no aplicada aún)
Remove-Migration -Project SistemaPlanificacionSNP.Infrastructure
```

## 10. Configuración de la Conexión

Edita el archivo `appsettings.json` en cada proyecto API:

### Para Desarrollo Local (LocalDB)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(LocalDb)\\MSSQLLocalDB;Database=SistemaPlanificacionSNP;Trusted_Connection=true;Encrypt=false;"
}
```

### Para SQL Server Remoto
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=SQL_SERVER_IP_O_NOMBRE;Database=SistemaPlanificacionSNP;User Id=sa;Password=tu_password;Encrypt=false;"
}
```

---

**Estado**: La solución está lista para Fase 2 (Repositorios y Seguridad)

Para más detalles, ver:
- 📄 README.md
- 📄 FASE1_MIGRACIONES.md
- 📄 RESUMEN_FASE1.md
