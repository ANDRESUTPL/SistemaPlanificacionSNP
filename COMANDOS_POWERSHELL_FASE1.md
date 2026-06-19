# 🎯 COMANDOS PARA EJECUTAR FASE 1 (PowerShell)

## Paso a Paso - Desde PowerShell en Visual Studio

### 1️⃣ Acceder a la Consola del Administrador de Paquetes

```
En Visual Studio:
Herramientas → Administrador de Paquetes NuGet → Consola del Administrador de Paquetes

O presiona: Ctrl + `
```

### 2️⃣ Verificar que el Proyecto Correcto Está Seleccionado

En el dropdown de la PMC (parte superior de la consola):

```
Default project: SistemaPlanificacionSNP.Infrastructure
```

Si no está seleccionado, haz clic en el dropdown y selecciónalo.

### 3️⃣ Ejecutar los Comandos

Copia y pega cada comando en la consola y presiona Enter.

---

## 📋 COMANDOS COMPLETOS (Copiar y Pegar)

### Bloque 1: Instalar Paquetes de Entity Framework Core

```powershell
Install-Package Microsoft.EntityFrameworkCore -Version 8.0.0
```

```powershell
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0
```

```powershell
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.0
```

```powershell
Install-Package Microsoft.EntityFrameworkCore.Design -Version 8.0.0
```

**⏱️ Espera a que finalice cada uno (~30-60 segundos)**

### Bloque 2: Instalar Paquetes de Seguridad

```powershell
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 8.0.0
```

```powershell
Install-Package BCrypt.Net-Next -Version 4.0.3
```

### Bloque 3: Instalar Paquetes de Mapping

```powershell
Install-Package AutoMapper -Version 13.0.1
```

```powershell
Install-Package AutoMapper.Extensions.Microsoft.DependencyInjection -Version 12.0.1
```

### Bloque 4: Instalar Paquetes del Gateway

```powershell
Install-Package Ocelot -Version 21.0.0
```

**⏱️ Espera a que finalice (~60 segundos)**

---

## 🗄️ CREAR LA BASE DE DATOS

### Paso 1: Generar la Migración Inicial

```powershell
Add-Migration InitialCreate -Project SistemaPlanificacionSNP.Infrastructure
```

**Expected Output** (aproximadamente):
```
Build started...
Build succeeded.
To undo this action, use Remove-Migration.
```

Esto crea el archivo: `SistemaPlanificacionSNP.Infrastructure/Migrations/[timestamp]_InitialCreate.cs`

### Paso 2: Aplicar la Migración a la Base de Datos

```powershell
Update-Database -Project SistemaPlanificacionSNP.Infrastructure
```

**Expected Output**:
```
Build started...
Build succeeded.
Applying migration '[timestamp]InitialCreate'
Done.
```

---

## ✅ VERIFICAR QUE FUNCIONÓ

### En SQL Server Object Explorer (Visual Studio)

```
1. Menú: Ver → SQL Server Object Explorer
2. Expande: SQL Server
3. Expande: (LocalDb)\MSSQLLocalDB
4. Expande: Databases
5. Busca: SistemaPlanificacionSNP
6. Expande: Tables
7. Verifica que existan estas 19 tablas:

   □ dbo.Auditorias
   □ dbo.Catalogos
   □ dbo.EntidadesPublicas
   □ dbo.ItemsCatalogo
   □ dbo.MatricesIndicadores
   □ dbo.MetasTerritorial
   □ dbo.ObjetivosDesarrolloSostenible
   □ dbo.ObjetivosEstrategicos
   □ dbo.Pantallas
   □ dbo.PeriodosPlanificacion
   □ dbo.PlanesEstrategicos
   □ dbo.PlanesNacionalesDesarrollo
   □ dbo.ProgramasPresupuestarios
   □ dbo.ProyectosInversion
   □ dbo.Revisiones
   □ dbo.RolPermisos
   □ dbo.Roles
   □ dbo.Usuarios
   □ dbo.UsuarioRoles
```

### Ejecutar Script SQL (Verificación)

En **SQL Server Management Studio** o **SQL Server Object Explorer - New Query**:

```sql
USE SistemaPlanificacionSNP;

-- Contar tablas
SELECT COUNT(*) as NumeroTablas FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo';

-- Listar tablas
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME;

-- Ver estructura de Usuario
EXEC sp_help 'Usuarios';

-- Ver relaciones
SELECT * FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_CATALOG = 'SistemaPlanificacionSNP';
```

---

## 🔧 COMANDOS ADICIONALES ÚTILES

### Crear una Nueva Migración (después de cambios)

```powershell
Add-Migration NombreDeLaMigracion -Project SistemaPlanificacionSNP.Infrastructure
```

Ejemplo:
```powershell
Add-Migration AgregarCampoDescripcion -Project SistemaPlanificacionSNP.Infrastructure
```

### Ver Migraciones Aplicadas

```powershell
Get-Migration -Project SistemaPlanificacionSNP.Infrastructure
```

### Revertir a una Migración Anterior

```powershell
Update-Database -Migration [NombreDeMigracionAnterior] -Project SistemaPlanificacionSNP.Infrastructure
```

Ejemplo:
```powershell
Update-Database -Migration InitialCreate -Project SistemaPlanificacionSNP.Infrastructure
```

### Eliminar la Última Migración (si no está aplicada)

```powershell
Remove-Migration -Project SistemaPlanificacionSNP.Infrastructure
```

### Generar Script SQL (sin aplicar)

```powershell
Script-Migration -From 0 -To InitialCreate -Project SistemaPlanificacionSNP.Infrastructure | Out-File -FilePath "C:\Scripts\Migration.sql"
```

### Reinstalar Todos los Paquetes

```powershell
Update-Package -Reinstall
```

---

## 📋 CHECKLIST DE EJECUCIÓN

Marca cada paso conforme lo completes:

```
[ ] 1. Abierta la Consola del Administrador de Paquetes (Ctrl + `)
[ ] 2. Seleccionado "SistemaPlanificacionSNP.Infrastructure" como proyecto por defecto
[ ] 3. Instalado Microsoft.EntityFrameworkCore 8.0.0
[ ] 4. Instalado Microsoft.EntityFrameworkCore.SqlServer 8.0.0
[ ] 5. Instalado Microsoft.EntityFrameworkCore.Tools 8.0.0
[ ] 6. Instalado Microsoft.EntityFrameworkCore.Design 8.0.0
[ ] 7. Instalado Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
[ ] 8. Instalado BCrypt.Net-Next 4.0.3
[ ] 9. Instalado AutoMapper 13.0.1
[ ] 10. Instalado AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1
[ ] 11. Instalado Ocelot 21.0.0
[ ] 12. Ejecutado: Add-Migration InitialCreate -Project SistemaPlanificacionSNP.Infrastructure
[ ] 13. Ejecutado: Update-Database -Project SistemaPlanificacionSNP.Infrastructure
[ ] 14. Verificadas las 19 tablas en SQL Server
[ ] 15. Leída la documentación completa
```

---

## 🐛 SOLUCIÓN DE PROBLEMAS

### Problema: "Cannot open database..."

```
Causa: La instancia de SQL Server no está corriendo
Solución: 
- Inicia SQL Server (LocalDb)
- O abre SQL Server Object Explorer para conectar
```

### Problema: "The migration was not applied to the database"

```
Causa: Update-Database no se ejecutó o falló
Solución:
1. Revisa la consola para errores
2. Vuelve a ejecutar: Update-Database -Project SistemaPlanificacionSNP.Infrastructure
```

### Problema: "Package [X] is not installed"

```
Causa: El paquete no se instaló completamente
Solución:
1. Cierra Visual Studio
2. Elimina la carpeta: C:\Users\[YourUser]\.nuget\packages
3. Abre Visual Studio nuevamente
4. Vuelve a ejecutar Install-Package
```

### Problema: "DbContext no se ve en IntelliSense"

```
Causa: El proyecto no está compilado
Solución:
1. Reconstruye la solución: Ctrl + Shift + B
2. O haz clic derecho en Solución > Compilar solución
```

### Problema: "Error en Add-Migration"

```
Causa: Las entidades no son válidas
Solución:
1. Verifica que todas las entidades tengan PK
2. Verifica que las relaciones estén bien configuradas
3. Reconstruye la solución
```

---

## 📝 EJEMPLO COMPLETO DE SESIÓN

```
Abres Visual Studio → Abres la solución → Consola de Paquetes

PM> Install-Package Microsoft.EntityFrameworkCore -Version 8.0.0
[Esperas ~30 seg...]
Successfully installed.

PM> Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0
[Esperas ~30 seg...]
Successfully installed.

PM> Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.0
[Esperas ~30 seg...]
Successfully installed.

PM> Install-Package Microsoft.EntityFrameworkCore.Design -Version 8.0.0
[Esperas ~20 seg...]
Successfully installed.

[Repites lo mismo para los otros paquetes...]

PM> Add-Migration InitialCreate -Project SistemaPlanificacionSNP.Infrastructure
Build started...
Build succeeded.
To undo this action, use Remove-Migration.

PM> Update-Database -Project SistemaPlanificacionSNP.Infrastructure
Build started...
Build succeeded.
Applying migration '[timestamp]InitialCreate'
Done.

✅ ¡LISTO! La base de datos está creada con 19 tablas.
```

---

## 🎯 PRÓXIMO PASO

Una vez completes esto, procede a **Fase 2**:

```
📄 Ver: FASE2_REPOSITORIOS.md (próximo documento)

Se implementarán:
- Repositorios genéricos y específicos
- Patrón Unit of Work
- Hashing BCrypt para usuarios
- Auditoría automática
```

---

**¿Listo para ejecutar?** 🚀

1. Abre: `c:\ProyectoAndres\SistemaPlanificacionSNP\SistemaPlanificacionSNP.sln`
2. Abre la Consola de Paquetes: `Ctrl + ` `
3. Selecciona el proyecto: `SistemaPlanificacionSNP.Infrastructure`
4. Copia y pega los comandos de arriba
5. ¡Espera a que termine!

**Tiempo estimado**: 10-15 minutos

---

*Documentación: Junio 17, 2026*
*Versión: 1.0*
